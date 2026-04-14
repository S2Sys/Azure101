using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.AI.ML;
using Azure.AI.ML.Models;
using Azure.Identity;
using Microsoft.Extensions.Logging;

namespace Azure101.AI_ML.Examples
{
    /// <summary>
    /// Azure Machine Learning - Build, Train, Deploy ML Models
    /// Demonstrates: Model training, deployment, monitoring, data preparation
    /// Production pattern: Full ML lifecycle management
    /// Use case: Custom ML models when pre-built services don't fit
    /// </summary>
    public class MachineLearningExample
    {
        private readonly MLClient _mlClient;
        private readonly ILogger<MachineLearningExample> _logger;
        private readonly string _resourceGroupName;
        private readonly string _workspaceName;

        public MachineLearningExample(
            string subscriptionId,
            string resourceGroupName,
            string workspaceName,
            ILogger<MachineLearningExample> logger)
        {
            _logger = logger;
            _resourceGroupName = resourceGroupName;
            _workspaceName = workspaceName;

            // Create ML client
            var credential = new DefaultAzureCredential();
            _mlClient = new MLClient(credential, subscriptionId, resourceGroupName, workspaceName);
        }

        // ============================================================================
        // Pattern 1: Create Training Job (Model Training)
        // ============================================================================
        /// <summary>
        /// Train ML model using AutoML (automated machine learning)
        /// AutoML: Automatically tries multiple algorithms and hyperparameters
        /// Time: 30 minutes to 2 hours depending on data size
        /// Cost: Compute charges while training (e.g., $0.5/hour)
        /// Benefit: No ML expertise needed - AutoML handles algorithm selection
        /// </summary>
        public async Task<string> TrainModelWithAutoMLAsync(
            string trainingDataPath,
            string targetColumn,
            string taskType) // "classification", "regression"
        {
            try
            {
                _logger.LogInformation($"→ Starting AutoML training for {taskType}");

                // Define training job
                var automLJob = new AutoMLJob
                {
                    Name = $"automl-{taskType}-{DateTime.UtcNow.Ticks}",
                    Description = $"AutoML {taskType} job",
                    ExperimentName = $"exp-{taskType}",

                    // Training configuration
                    Task = taskType switch
                    {
                        "classification" => MLTaskType.Classification,
                        "regression" => MLTaskType.Regression,
                        _ => throw new ArgumentException("Invalid task type")
                    },

                    PrimaryMetric = taskType == "classification"
                        ? ClassificationPrimaryMetrics.Accuracy
                        : RegressionPrimaryMetrics.NormalizedMeanAbsoluteError as object,

                    // Training dataset
                    TrainingData = new MLTableJobInput(
                        new Uri(trainingDataPath)),

                    // Target column to predict
                    TargetColumnName = targetColumn,

                    // Compute resource
                    ComputeId = "/subscriptions/{subscriptionId}/resourceGroups/{rg}/providers/Microsoft.MachineLearningServices/workspaces/{ws}/computes/cpu-cluster",

                    // Time limit
                    LimitSettings = new AutoMLLimitSettings
                    {
                        MaxTrials = 5,              // Try 5 algorithms max
                        MaxConcurrentTrials = 2,    // Run 2 in parallel
                        Timeout = new TimeSpan(0, 30, 0)  // 30 minute limit
                    }
                };

                // Submit job
                var jobOperation = await _mlClient.Jobs.CreateOrUpdateAsync(automLJob);
                var jobId = jobOperation.Value.Id;

                _logger.LogInformation($"✓ Training job submitted: {jobId}");
                _logger.LogInformation($"  Monitor at: Azure Portal → ML Workspace → Jobs");

                // Poll for completion (in real scenario, would use background job)
                var completedJob = await PollJobAsync(jobId);

                _logger.LogInformation($"✓ Training complete. Best model accuracy: {completedJob}");
                return jobId;
            }
            catch (Exception ex)
            {
                _logger.LogError($"✗ Training failed: {ex.Message}");
                throw;
            }
        }

        // ============================================================================
        // Pattern 2: Register Trained Model
        // ============================================================================
        /// <summary>
        /// Register trained model to model registry (versioning + tracking)
        /// Benefits: Model versioning, rollback, promotion to prod
        /// Use case: Keep history of all trained models, revert if new one fails
        /// </summary>
        public async Task<Model> RegisterModelAsync(
            string modelName,
            string modelPath,
            string modelVersion)
        {
            try
            {
                _logger.LogInformation($"→ Registering model {modelName} version {modelVersion}");

                var model = new Model
                {
                    Name = modelName,
                    Version = modelVersion,
                    Path = modelPath,
                    Type = ModelType.CustomModel,
                    Description = $"Production model for {modelName}"
                };

                var registeredModel = await _mlClient.Models.CreateOrUpdateAsync(model);

                _logger.LogInformation($"✓ Model registered");
                _logger.LogInformation($"  Name: {registeredModel.Value.Name}");
                _logger.LogInformation($"  Version: {registeredModel.Value.Version}");
                _logger.LogInformation($"  Path: {registeredModel.Value.Path}");

                return registeredModel.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError($"✗ Model registration failed: {ex.Message}");
                throw;
            }
        }

        // ============================================================================
        // Pattern 3: Create Online Endpoint for Real-Time Predictions
        // ============================================================================
        /// <summary>
        /// Deploy model as REST API endpoint
        /// Inference speed: 100-500ms per request
        /// Cost: Compute per hour (e.g., $0.4/hour for small endpoint)
        /// Use case: Real-time predictions in web/mobile apps
        /// Scaling: Auto-scale based on traffic
        /// </summary>
        public async Task<string> DeployModelAsync(
            string endpointName,
            string deploymentName,
            string modelId,
            string modelVersion,
            int instanceCount = 1)
        {
            try
            {
                _logger.LogInformation($"→ Deploying model as endpoint: {endpointName}");

                // Create online endpoint
                var endpoint = new OnlineEndpoint
                {
                    Name = endpointName,
                    Description = $"Endpoint for {modelId}",
                    AuthMode = EndpointAuthMode.Key
                };

                await _mlClient.OnlineEndpoints.CreateOrUpdateAsync(endpoint);
                _logger.LogInformation($"✓ Endpoint created: {endpointName}");

                // Create deployment (blue-green deployment support)
                var deployment = new OnlineDeployment
                {
                    Name = deploymentName,
                    EndpointName = endpointName,
                    Model = new AssetReferenceBase()
                    {
                        AssetId = $"azureml://registries/default/models/{modelId}/versions/{modelVersion}"
                    },
                    InstanceType = "Standard_DS2_v2",  // VM size
                    InstanceCount = instanceCount,
                    CodeConfiguration = new CodeConfiguration
                    {
                        ScoringUri = "file://./score.py"
                    }
                };

                var createdDeployment = await _mlClient.OnlineDeployments.CreateOrUpdateAsync(
                    endpointName, deployment);

                _logger.LogInformation($"✓ Deployment created: {deploymentName}");
                _logger.LogInformation($"  Endpoint URI: https://{endpointName}.eastus.inference.ml.azure.com/score");

                return $"{endpointName}/{deploymentName}";
            }
            catch (Exception ex)
            {
                _logger.LogError($"✗ Deployment failed: {ex.Message}");
                throw;
            }
        }

        // ============================================================================
        // Pattern 4: Make Predictions (Inference)
        // ============================================================================
        /// <summary>
        /// Send data to deployed endpoint for predictions
        /// Protocol: HTTP POST with JSON
        /// Response: Model prediction for input data
        /// Use case: Every prediction in production application
        /// </summary>
        public async Task<PredictionResult> MakePredictionAsync(
            string endpointName,
            List<Dictionary<string, object>> inputData)
        {
            try
            {
                _logger.LogInformation($"→ Making prediction on {endpointName}");

                // Format input as JSON
                var json = System.Text.Json.JsonSerializer.Serialize(inputData);

                // Call endpoint (in real scenario)
                // var httpClient = new HttpClient();
                // var response = await httpClient.PostAsync(
                //     $"https://{endpointName}.eastus.inference.ml.azure.com/score",
                //     new StringContent(json));

                // Mock response for example
                var prediction = new PredictionResult
                {
                    PredictedClass = "Class A",
                    Probability = 0.95f,
                    Confidence = 0.92f,
                    ModelVersion = "1.2"
                };

                _logger.LogInformation($"✓ Prediction: {prediction.PredictedClass} (confidence: {prediction.Confidence:P})");

                return prediction;
            }
            catch (Exception ex)
            {
                _logger.LogError($"✗ Prediction failed: {ex.Message}");
                throw;
            }
        }

        // ============================================================================
        // Pattern 5: Batch Prediction (High Volume)
        // ============================================================================
        /// <summary>
        /// Score large dataset (thousands of records) efficiently
        /// Time: Process in parallel, takes few minutes
        /// Cost: Lower per-prediction cost vs real-time API
        /// Use case: Nightly scoring, batch processing
        /// Example: Score all customers for churn prediction
        /// </summary>
        public async Task<string> BatchPredictionAsync(
            string modelVersion,
            string inputDataPath)
        {
            try
            {
                _logger.LogInformation($"→ Starting batch prediction job");

                // Define batch job
                var batchJob = new BatchDeployment
                {
                    Name = $"batch-job-{DateTime.UtcNow.Ticks}",
                    Description = "Batch scoring job",
                    Model = new AssetReferenceBase()
                    {
                        AssetId = $"azureml://registries/default/models/my-model/versions/{modelVersion}"
                    },
                    OutputAction = BatchOutputAction.AppendRow,
                    MiniBatchSize = 10,  // Process 10 records at a time
                    InstanceCount = 5,   // Use 5 compute instances in parallel
                    Timeout = new TimeSpan(1, 0, 0)  // 1 hour timeout
                };

                _logger.LogInformation($"✓ Batch job submitted");
                _logger.LogInformation($"  Input: {inputDataPath}");
                _logger.LogInformation($"  Processing: 5 instances × 10 records/batch");
                _logger.LogInformation($"  Output: Will be saved to output path");

                return "batch-job-123";
            }
            catch (Exception ex)
            {
                _logger.LogError($"✗ Batch prediction failed: {ex.Message}");
                throw;
            }
        }

        // ============================================================================
        // Pattern 6: Model Monitoring and Performance
        // ============================================================================
        /// <summary>
        /// Monitor deployed model performance in production
        /// Detect: Data drift, prediction drift, model degradation
        /// Action: Alert, retrain, or rollback model
        /// </summary>
        public async Task<ModelPerformance> MonitorModelAsync(
            string endpointName,
            string deploymentName)
        {
            try
            {
                _logger.LogInformation($"→ Analyzing model performance: {deploymentName}");

                // Get endpoint details
                var endpoint = await _mlClient.OnlineEndpoints.GetAsync(endpointName);
                var deployment = await _mlClient.OnlineDeployments.GetAsync(endpointName, deploymentName);

                var performance = new ModelPerformance
                {
                    EndpointName = endpointName,
                    DeploymentName = deploymentName,
                    AvailableInstances = deployment.Value.InstanceCount ?? 0,
                    ModelVersion = deployment.Value.Model?.AssetId ?? "unknown",
                    Status = "Active"
                };

                _logger.LogInformation($"✓ Performance metrics:");
                _logger.LogInformation($"  Deployment Status: {deployment.Value.ProvisioningState}");
                _logger.LogInformation($"  Instances: {performance.AvailableInstances}");
                _logger.LogInformation($"  Model Version: {performance.ModelVersion}");

                // In real scenario, would query Application Insights for:
                // - Request latency (p50, p95, p99)
                // - Error rate
                // - Prediction distribution (drift detection)
                // - Data characteristics vs training data

                return performance;
            }
            catch (Exception ex)
            {
                _logger.LogError($"✗ Monitoring failed: {ex.Message}");
                throw;
            }
        }

        // ============================================================================
        // Pattern 7: ML Workflow - Complete Lifecycle
        // ============================================================================
        /// <summary>
        /// End-to-end ML workflow: Prepare → Train → Validate → Deploy → Monitor
        /// This is the typical pattern for production ML systems
        /// </summary>
        public static class MLWorkflow
        {
            public const string PHASES = @"
                PHASE 1: PREPARE DATA
                ├─ Collect raw data
                ├─ Clean and validate
                ├─ Feature engineering
                └─ Split: 70% train, 15% validation, 15% test

                PHASE 2: TRAIN MODELS
                ├─ AutoML tries multiple algorithms
                ├─ Hyperparameter tuning
                ├─ Cross-validation
                └─ Select best model

                PHASE 3: EVALUATE
                ├─ Test on test set (never seen before)
                ├─ Check metrics (accuracy, precision, recall, F1)
                ├─ Confusion matrix analysis
                └─ Compare vs baseline/previous model

                PHASE 4: REGISTER
                ├─ Version model
                ├─ Document features and performance
                ├─ Store in model registry
                └─ Enable rollback if needed

                PHASE 5: DEPLOY
                ├─ Create endpoint
                ├─ Blue-green deployment
                ├─ Test in staging
                └─ Gradually shift traffic (canary)

                PHASE 6: MONITOR
                ├─ Track performance metrics
                ├─ Detect data drift
                ├─ Detect prediction drift
                ├─ Alert on degradation
                └─ Schedule retraining";

            public const string TIMELINE = @"
                Data Preparation: 2-4 weeks (often 80% of time!)
                Training: 1-2 hours
                Evaluation: 1-2 days
                Deployment: 1-2 days
                Monitoring: Ongoing

                Total: 1-2 months for first production model";

            public const string CHALLENGES = @"
                1. Data Quality
                   Problem: Missing values, outliers, inconsistencies
                   Solution: Data validation, cleaning, imputation

                2. Class Imbalance
                   Problem: 95% class A, 5% class B
                   Solution: Oversampling, undersampling, cost-weighted training

                3. Overfitting
                   Problem: Model memorizes training data, fails on new data
                   Solution: Regularization, early stopping, cross-validation

                4. Data Drift
                   Problem: Production data different from training data
                   Solution: Monitor distribution, retrain periodically

                5. Model Drift
                   Problem: Model accuracy degrades over time
                   Solution: Automatic retraining, performance monitoring";
        }

        // ============================================================================
        // Comparison: Cognitive Services vs Machine Learning
        // ============================================================================
        public static class CognitiveServicesVsML
        {
            public const string COGNITIVE_SERVICES = @"
                Pre-built AI models

                Use when:
                ✓ Standard task (sentiment, OCR, translation)
                ✓ Quick deployment needed
                ✓ Limited ML expertise
                ✓ Don't need customization

                Pros:
                ✓ Ready in minutes
                ✓ Microsoft maintains models
                ✓ Works across domains
                ✗ Less control

                Cost:
                $0.002-$2 per API call (varies by service)";

            public const string AZURE_ML = @"
                Build custom ML models

                Use when:
                ✓ Cognitive Services doesn't fit
                ✓ Domain-specific needs
                ✓ Have training data
                ✓ High volume (lower per-request cost)

                Pros:
                ✓ Highly customizable
                ✓ Optimized for your domain
                ✓ Cost-effective at scale
                ✗ Takes longer (weeks)

                Cost:
                Training: $0.5-2/hour (compute)
                Inference: $0.4-1/hour (endpoint)
                + storage, compute";

            public const string DECISION_FRAMEWORK = @"
                Question 1: Is it a standard task?
                YES → Use Cognitive Services
                NO → Use Azure ML

                Question 2: Do you have training data?
                NO → Use Cognitive Services
                YES → Could use either

                Question 3: High volume (1M+ predictions/month)?
                YES → Azure ML (cheaper at scale)
                NO → Cognitive Services (easier)

                Question 4: Need 99.5% accuracy?
                YES → Azure ML (custom tuning)
                NO → Cognitive Services (good enough)";
        }

        // ============================================================================
        // Helper Methods
        // ============================================================================
        private async Task<string> PollJobAsync(string jobId, int maxWaitSeconds = 3600)
        {
            // In real scenario, would poll job status until complete
            await Task.Delay(500);
            return "0.94"; // Mock accuracy
        }
    }

    // ============================================================================
    // Supporting Models
    // ============================================================================
    public class PredictionResult
    {
        public string PredictedClass { get; set; }
        public float Probability { get; set; }
        public float Confidence { get; set; }
        public string ModelVersion { get; set; }
    }

    public class ModelPerformance
    {
        public string EndpointName { get; set; }
        public string DeploymentName { get; set; }
        public int AvailableInstances { get; set; }
        public string ModelVersion { get; set; }
        public string Status { get; set; }
        public double LatencyMs { get; set; }
        public double ErrorRate { get; set; }
    }
}
