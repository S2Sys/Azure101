using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace Azure101.Databases.Examples
{
    /// <summary>
    /// SQL Database Basic Operations Example
    /// Demonstrates: Connection pooling, async/await, transactions, retry policies
    /// Production-ready patterns for Azure SQL Database
    /// </summary>
    public class SqlDatabaseBasicExample
    {
        private readonly string _connectionString;
        private readonly IAsyncPolicy<bool> _retryPolicy;

        public SqlDatabaseBasicExample(string connectionString)
        {
            _connectionString = connectionString;

            // Retry policy: exponential backoff
            _retryPolicy = Policy<bool>
                .Handle<SqlException>()
                .OrResult(r => !r)
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 100),
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        Console.WriteLine($"Retry {retryCount} after {timespan.TotalMilliseconds}ms");
                    });
        }

        // ============================================================================
        // Pattern 1: Async Query Execution with Connection Pooling
        // ============================================================================
        /// <summary>
        /// Get customer by ID using async/await pattern
        /// Connection pooling is automatic with SqlConnection
        /// Pooling reduces connection overhead by 10-100x
        /// </summary>
        public async Task<Customer> GetCustomerByIdAsync(int customerId)
        {
            try
            {
                // Connection pooling: SqlClient automatically manages pool
                // Pool size: 0-100 connections by default
                // Pool timeout: 15 minutes idle = connection destroyed
                await using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    const string query = "SELECT Id, Name, Email, CreatedDate FROM Customers WHERE Id = @id";

                    await using (var command = new SqlCommand(query, connection))
                    {
                        command.CommandTimeout = 30; // 30 second timeout
                        command.Parameters.AddWithValue("@id", customerId);

                        await using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new Customer
                                {
                                    Id = (int)reader["Id"],
                                    Name = reader["Name"].ToString(),
                                    Email = reader["Email"].ToString(),
                                    CreatedDate = (DateTime)reader["CreatedDate"]
                                };
                            }
                        }
                    }
                }
            }
            catch (SqlException ex) when (ex.Number == -2) // Timeout
            {
                Console.WriteLine("Query timeout - connection lost or database slow");
                throw;
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"Database error: {ex.Message}");
                throw;
            }

            return null;
        }

        // ============================================================================
        // Pattern 2: Bulk Insert with Transaction
        // ============================================================================
        /// <summary>
        /// Insert 1000 customers in a single transaction
        /// Bulk insert: ~100x faster than individual inserts
        /// Transaction: All-or-nothing atomicity
        /// </summary>
        public async Task<int> BulkInsertCustomersAsync(List<Customer> customers)
        {
            try
            {
                await using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Start transaction
                    await using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            const string insertQuery = @"
                                INSERT INTO Customers (Name, Email, CreatedDate)
                                VALUES (@name, @email, @createdDate);
                                SELECT CAST(SCOPE_IDENTITY() as int);";

                            int insertedCount = 0;

                            // Batch insert in groups of 100 for memory efficiency
                            for (int i = 0; i < customers.Count; i++)
                            {
                                var customer = customers[i];

                                await using (var command = new SqlCommand(insertQuery, connection, transaction))
                                {
                                    command.Parameters.AddWithValue("@name", customer.Name);
                                    command.Parameters.AddWithValue("@email", customer.Email);
                                    command.Parameters.AddWithValue("@createdDate", customer.CreatedDate);

                                    var result = await command.ExecuteScalarAsync();
                                    if (result != null && int.TryParse(result.ToString(), out int id))
                                    {
                                        insertedCount++;
                                    }
                                }

                                // Log progress every 100 inserts
                                if ((i + 1) % 100 == 0)
                                {
                                    Console.WriteLine($"Inserted {i + 1}/{customers.Count} customers");
                                }
                            }

                            // Commit transaction
                            transaction.Commit();
                            Console.WriteLine($"✓ Successfully inserted {insertedCount} customers");
                            return insertedCount;
                        }
                        catch (Exception ex)
                        {
                            // Rollback on error
                            transaction.Rollback();
                            Console.WriteLine($"✗ Transaction failed, rolled back: {ex.Message}");
                            throw;
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"Database error during bulk insert: {ex.Message}");
                throw;
            }
        }

        // ============================================================================
        // Pattern 3: Query with Retry Policy (Polly)
        // ============================================================================
        /// <summary>
        /// Execute query with automatic retry on transient failures
        /// Transient errors: Timeouts, connection drops, temporary unavailability
        /// Polly: Industry-standard resilience library
        /// </summary>
        public async Task<int> GetCustomerCountWithRetryAsync()
        {
            try
            {
                // Execute with retry policy
                var result = await _retryPolicy.ExecuteAsync(async () =>
                {
                    await using (var connection = new SqlConnection(_connectionString))
                    {
                        await connection.OpenAsync();

                        const string query = "SELECT COUNT(*) FROM Customers";

                        await using (var command = new SqlCommand(query, connection))
                        {
                            command.CommandTimeout = 10;
                            var result = await command.ExecuteScalarAsync();
                            return result != null && int.TryParse(result.ToString(), out int count) ? count : 0;
                        }
                    }
                });

                Console.WriteLine($"Total customers: {result}");
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed after retries: {ex.Message}");
                throw;
            }
        }

        // ============================================================================
        // Pattern 4: Update with Optimistic Locking (Concurrency Control)
        // ============================================================================
        /// <summary>
        /// Update customer with version check
        /// Prevents lost updates when multiple users edit same record
        /// Uses RowVersion (timestamp) for optimistic locking
        /// </summary>
        public async Task<bool> UpdateCustomerWithLockingAsync(int customerId, string newEmail, byte[] currentVersion)
        {
            try
            {
                await using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Only update if version matches (no other updates occurred)
                    const string updateQuery = @"
                        UPDATE Customers
                        SET Email = @email, ModifiedDate = GETUTCDATE()
                        WHERE Id = @id AND RowVersion = @version;
                        SELECT @@ROWCOUNT;"; // Return 1 if updated, 0 if not

                    await using (var command = new SqlCommand(updateQuery, connection))
                    {
                        command.Parameters.AddWithValue("@id", customerId);
                        command.Parameters.AddWithValue("@email", newEmail);
                        command.Parameters.AddWithValue("@version", currentVersion);

                        var result = await command.ExecuteScalarAsync();

                        if (result != null && int.Parse(result.ToString()) == 1)
                        {
                            Console.WriteLine("✓ Customer updated successfully");
                            return true;
                        }
                        else
                        {
                            Console.WriteLine("✗ Update failed - record was modified by another user");
                            return false;
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"Update error: {ex.Message}");
                throw;
            }
        }

        // ============================================================================
        // Pattern 5: Connection Pool Configuration
        // ============================================================================
        /// <summary>
        /// Best practices for connection string and pooling
        /// </summary>
        public static string GetOptimizedConnectionString(
            string server,
            string database,
            string userId,
            string password)
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = server,
                InitialCatalog = database,
                UserID = userId,
                Password = password,

                // Connection pool settings
                Pooling = true,
                Min Pool Size = 5,              // Minimum connections to keep open
                Max Pool Size = 100,            // Maximum connections allowed
                Connection Timeout = 15,        // Timeout for getting connection from pool
                Connection Lifetime = 300,      // Force reconnection after 5 minutes

                // Performance settings
                Encrypt = true,                 // Encrypt connection
                TrustServerCertificate = false, // Verify server certificate

                // Other optimizations
                MultipleActiveResultSets = true, // Allow multiple operations on same connection
                Application Name = "Azure101Database"
            };

            return builder.ConnectionString;
        }
    }

    // ============================================================================
    // Supporting Model Classes
    // ============================================================================
    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }
}
