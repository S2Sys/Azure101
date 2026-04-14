using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;

namespace Azure101.AI_ML.Examples
{
    /// <summary>
    /// Azure OpenAI Service - Access to GPT-4, GPT-3.5-turbo, Embeddings
    /// Demonstrates: Chat completions, function calling, embeddings, prompt optimization
    /// Production pattern: LLM-powered applications, intelligent assistants, content generation
    /// Note: Requires Azure OpenAI access (applied separately from standard Azure)
    /// </summary>
    public class AzureOpenAIExample
    {
        private readonly OpenAIClient _openAIClient;
        private readonly ILogger<AzureOpenAIExample> _logger;

        public AzureOpenAIExample(OpenAIClient openAIClient, ILogger<AzureOpenAIExample> logger)
        {
            _openAIClient = openAIClient;
            _logger = logger;
        }

        // ============================================================================
        // Pattern 1: Simple Chat Completion
        // ============================================================================
        /// <summary>
        /// Ask GPT model a question and get answer
        /// Model: gpt-35-turbo (fast, cheap) or gpt-4 (smarter, slower)
        /// Cost: ~$0.002 per 1K input tokens, ~$0.004 per 1K output tokens (gpt-3.5)
        /// Performance: 1-2 seconds response time
        /// Use case: Q&A, chat interface, content generation
        /// </summary>
        public async Task<string> SimpleCompletionAsync(string userMessage)
        {
            try
            {
                _logger.LogInformation($"→ Getting AI response for: {userMessage}");

                // Create chat messages
                var messages = new List<ChatCompletionMessage>
                {
                    new ChatCompletionMessage(
                        ChatRole.User,
                        userMessage)
                };

                // Call OpenAI
                var completionOptions = new ChatCompletionOptions
                {
                    Temperature = 0.7f,        // 0=deterministic, 1=creative
                    MaxTokens = 500,           // Max response length (1 token ≈ 4 chars)
                    FrequencyPenalty = 0f,     // Reduce repetition
                    PresencePenalty = 0f       // Encourage new topics
                };

                var response = await _openAIClient.GetChatCompletionsAsync(
                    deploymentOrModelName: "gpt-35-turbo", // Your deployment name
                    completionOptions);

                var assistantMessage = response.Value.Choices[0].Message.Content;

                _logger.LogInformation($"✓ Response: {assistantMessage}");
                _logger.LogInformation($"  Tokens used: {response.Value.Usage.TotalTokens}");

                return assistantMessage;
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError($"✗ OpenAI request failed: {ex.Message}");
                throw;
            }
        }

        // ============================================================================
        // Pattern 2: Multi-Turn Conversation (Chat History)
        // ============================================================================
        /// <summary>
        /// Maintain conversation context across multiple messages
        /// GPT remembers previous messages in thread
        /// Use case: Chatbot, customer support, interactive assistant
        /// Cost: Charged for entire conversation history (be mindful of length)
        /// </summary>
        public async Task<string> ConversationAsync(
            List<ConversationMessage> history,
            string userMessage)
        {
            try
            {
                _logger.LogInformation($"→ Processing message (history: {history.Count} messages)");

                // Build messages list with history
                var messages = new List<ChatCompletionMessage>();

                // Add system context (optional)
                messages.Add(new ChatCompletionMessage(
                    ChatRole.System,
                    "You are a helpful customer service assistant."));

                // Add conversation history
                foreach (var msg in history)
                {
                    if (msg.IsUserMessage)
                        messages.Add(new ChatCompletionMessage(ChatRole.User, msg.Content));
                    else
                        messages.Add(new ChatCompletionMessage(ChatRole.Assistant, msg.Content));
                }

                // Add current user message
                messages.Add(new ChatCompletionMessage(ChatRole.User, userMessage));

                var response = await _openAIClient.GetChatCompletionsAsync(
                    deploymentOrModelName: "gpt-35-turbo",
                    new ChatCompletionOptions
                    {
                        Temperature = 0.7f,
                        MaxTokens = 500
                    });

                var reply = response.Value.Choices[0].Message.Content;

                _logger.LogInformation($"✓ Reply: {reply}");

                return reply;
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError($"✗ Conversation failed: {ex.Message}");
                throw;
            }
        }

        // ============================================================================
        // Pattern 3: Function Calling (Tool Use)
        // ============================================================================
        /// <summary>
        /// AI calls functions you provide (structured output)
        /// AI determines when to use each function and extracts parameters
        /// Use case: AI agents, automation, structured data extraction
        /// Example: "Check inventory for product X" → AI calls CheckInventory() function
        /// </summary>
        public async Task<FunctionCallResult> FunctionCallingAsync(string userRequest)
        {
            try
            {
                _logger.LogInformation($"→ Processing function call request: {userRequest}");

                // Define available functions
                var functions = new List<ChatCompletionFunctionDefinition>
                {
                    new ChatCompletionFunctionDefinition
                    {
                        Name = "get_product_inventory",
                        Description = "Get inventory count for a product",
                        Parameters = BinaryData.FromObjectAsJson(new
                        {
                            type = "object",
                            properties = new
                            {
                                product_name = new { type = "string", description = "Name of product" }
                            },
                            required = new[] { "product_name" }
                        })
                    },
                    new ChatCompletionFunctionDefinition
                    {
                        Name = "check_order_status",
                        Description = "Check status of existing order",
                        Parameters = BinaryData.FromObjectAsJson(new
                        {
                            type = "object",
                            properties = new
                            {
                                order_id = new { type = "string", description = "Order ID" }
                            },
                            required = new[] { "order_id" }
                        })
                    }
                };

                var messages = new List<ChatCompletionMessage>
                {
                    new ChatCompletionMessage(ChatRole.User, userRequest)
                };

                var options = new ChatCompletionOptions
                {
                    Temperature = 0.5f,
                    MaxTokens = 1000,
                    Functions = functions
                };

                var response = await _openAIClient.GetChatCompletionsAsync(
                    deploymentOrModelName: "gpt-35-turbo",
                    options);

                var choice = response.Value.Choices[0];

                // Check if function was called
                if (choice.FinishReason == CompletionFinishReason.FunctionCall)
                {
                    var functionCall = choice.Message.FunctionCall;
                    _logger.LogInformation($"✓ Function called: {functionCall.Name}");
                    _logger.LogInformation($"  Parameters: {functionCall.Arguments}");

                    // Execute the appropriate function
                    var result = await ExecuteFunctionAsync(functionCall.Name, functionCall.Arguments);

                    return new FunctionCallResult
                    {
                        FunctionName = functionCall.Name,
                        Parameters = functionCall.Arguments,
                        Result = result
                    };
                }

                return new FunctionCallResult { Result = "No function called" };
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError($"✗ Function calling failed: {ex.Message}");
                throw;
            }
        }

        // ============================================================================
        // Pattern 4: Embeddings (Vector Representation)
        // ============================================================================
        /// <summary>
        /// Convert text to vector embeddings for similarity search
        /// Embeddings: 1536-dimensional vector (for text-embedding-3-small)
        /// Use case: Semantic search, recommendation engine, clustering
        /// Cost: ~$0.02 per 1M input tokens (very cheap!)
        /// Application: "Find similar documents" without using full GPT
        /// </summary>
        public async Task<List<double>> GetEmbeddingAsync(string text)
        {
            try
            {
                _logger.LogInformation($"→ Generating embedding for text");

                var response = await _openAIClient.GetEmbeddingsAsync(
                    deploymentOrModelName: "text-embedding-3-small",
                    new EmbeddingsOptions { Input = { text } });

                var embedding = response.Value.Data[0].Embedding.ToList();

                _logger.LogInformation($"✓ Embedding generated ({embedding.Count} dimensions)");
                return embedding;
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError($"✗ Embedding generation failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Find similar documents using embeddings
        /// Algorithm: Cosine similarity between vectors
        /// Performance: Near-instant (vector comparison)
        /// Use case: Semantic search, FAQ matching, document similarity
        /// </summary>
        public async Task<List<SimilarDocument>> FindSimilarDocumentsAsync(
            string query,
            List<string> documents,
            float similarityThreshold = 0.7f)
        {
            try
            {
                _logger.LogInformation($"→ Finding similar documents for: {query}");

                // Get embedding for query
                var queryEmbedding = await GetEmbeddingAsync(query);

                // Get embeddings for documents
                var docEmbeddings = new List<List<double>>();
                foreach (var doc in documents)
                {
                    var embedding = await GetEmbeddingAsync(doc);
                    docEmbeddings.Add(embedding);
                }

                // Calculate similarity scores
                var results = new List<SimilarDocument>();
                for (int i = 0; i < documents.Count; i++)
                {
                    var similarity = CosineSimilarity(queryEmbedding, docEmbeddings[i]);

                    if (similarity >= similarityThreshold)
                    {
                        results.Add(new SimilarDocument
                        {
                            Document = documents[i],
                            SimilarityScore = similarity
                        });
                    }
                }

                // Sort by similarity
                results.Sort((a, b) => b.SimilarityScore.CompareTo(a.SimilarityScore));

                _logger.LogInformation($"✓ Found {results.Count} similar documents");
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError($"✗ Similarity search failed: {ex.Message}");
                throw;
            }
        }

        // ============================================================================
        // Pattern 5: Prompt Engineering Best Practices
        // ============================================================================
        /// <summary>
        /// Techniques to improve AI quality
        /// </summary>
        public static class PromptEngineering
        {
            public const string TECHNIQUES = @"
                1. Be Specific
                   BAD: 'Explain databases'
                   GOOD: 'Explain how SQL index seeks work with 100-word limit'

                2. Provide Context
                   BAD: 'What should I use?'
                   GOOD: 'I need to process 1M records daily. What database?'

                3. Give Examples
                   BAD: 'Format as JSON'
                   GOOD: 'Format as JSON like: {\"name\": \"value\"}'

                4. Role Definition
                   'You are a senior Azure architect...'
                   'You are debugging a slow database query...'

                5. Step-by-Step Reasoning
                   'Think through this step by step before answering'
                   → Better quality responses

                6. Few-Shot Examples
                   Show 2-3 examples of desired output format
                   → AI follows pattern

                7. Temperature Selection
                   - Temperature 0.0 = Deterministic (Q&A, facts)
                   - Temperature 0.7 = Creative (writing, ideas)
                   - Temperature 1.0 = Random";

            public const string PROMPT_EXAMPLES = @"
                ✓ GOOD PROMPT (Specific, Context, Format):
                'As a C# architect, write a SQL query that:
                1. Gets orders from last 90 days
                2. Groups by customer
                3. Returns customer name, order count, total amount
                4. Sorted by total amount descending
                5. Only customers with 3+ orders'

                ✓ CHAIN OF THOUGHT (Complex problems):
                'Explain how to optimize a slow query. Work through:
                1. Identify the problem
                2. List possible causes
                3. Suggest solutions for each cause
                4. Pick best solution with reasoning'

                ✓ SYSTEM PROMPT (Consistency):
                System: 'You are a DevOps expert specializing in Azure Kubernetes Service'
                User: 'How do I enable auto-scaling?'
                → All responses assume AKS context";
        }

        // ============================================================================
        // Pattern 6: Token Counting and Cost Optimization
        // ============================================================================
        /// <summary>
        /// Understand token usage and control costs
        /// 1 token ≈ 4 characters ≈ 0.75 words
        /// Charged for input + output tokens
        /// </summary>
        public static class TokenManagement
        {
            public const string TOKEN_FACTS = @"
                Pricing (gpt-3.5-turbo):
                - Input: $0.50 per 1M tokens
                - Output: $1.50 per 1M tokens

                Token Estimation:
                - 'hello' = 1 token
                - Full sentence = 15-20 tokens
                - 1KB text = 250 tokens
                - 1M API calls = ~100M tokens

                Cost Example:
                - 1000 API calls
                - 500 tokens input + 200 tokens output each
                - 500K input + 200K output = 700K tokens
                - Cost: $0.50 * 0.5 + $1.50 * 0.2 = $0.55 per 1000 calls";

            public const string COST_OPTIMIZATION = @"
                1. Reduce Input Tokens
                   ✓ Don't include full conversation history
                   ✓ Summarize old messages
                   ✓ Remove irrelevant context

                2. Limit Output Tokens
                   ✓ Set MaxTokens = 500 (not 4096)
                   ✓ Ask for concise responses
                   ✓ Use gpt-35-turbo instead of gpt-4

                3. Cache Results
                   ✓ Same question? Reuse response
                   ✓ Common prompts? Cache outputs
                   ✓ Reduce API calls by 90%

                4. Batch Processing
                   ✓ Process multiple requests together
                   ✓ Not supported by Azure OpenAI yet
                   ✓ Use loop with caching";

            public const string WHEN_USE_GPT4 = @"
                Use gpt-35-turbo (cheaper, faster):
                - Simple Q&A
                - Customer support
                - Content generation
                - Code generation (simple)
                - Classification tasks

                Use gpt-4 (smarter, expensive):
                - Complex reasoning
                - Code review
                - Architecture decisions
                - Multi-step planning
                - Unusual edge cases";
        }

        // ============================================================================
        // Helper Methods
        // ============================================================================
        private async Task<string> ExecuteFunctionAsync(string functionName, string parameters)
        {
            // In real scenario, execute actual function based on name
            return functionName switch
            {
                "get_product_inventory" => "Product 'Laptop' has 45 units in stock",
                "check_order_status" => "Order #12345 is In Transit (expected delivery: Jan 15)",
                _ => "Unknown function"
            };
        }

        private float CosineSimilarity(List<double> vec1, List<double> vec2)
        {
            double dotProduct = 0;
            double magnitude1 = 0;
            double magnitude2 = 0;

            for (int i = 0; i < vec1.Count; i++)
            {
                dotProduct += vec1[i] * vec2[i];
                magnitude1 += vec1[i] * vec1[i];
                magnitude2 += vec2[i] * vec2[i];
            }

            magnitude1 = Math.Sqrt(magnitude1);
            magnitude2 = Math.Sqrt(magnitude2);

            return (float)(dotProduct / (magnitude1 * magnitude2));
        }
    }

    // ============================================================================
    // Supporting Models
    // ============================================================================
    public class ConversationMessage
    {
        public string Content { get; set; }
        public bool IsUserMessage { get; set; }
    }

    public class FunctionCallResult
    {
        public string FunctionName { get; set; }
        public string Parameters { get; set; }
        public string Result { get; set; }
    }

    public class SimilarDocument
    {
        public string Document { get; set; }
        public float SimilarityScore { get; set; }
    }
}
