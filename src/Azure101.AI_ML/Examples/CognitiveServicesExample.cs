using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Azure;
using Azure.AI.TextAnalytics;
using Azure.AI.Vision.ImageAnalysis;
using Azure.Cognition.Language.Conversations;
using Microsoft.Extensions.Logging;

namespace Azure101.AI_ML.Examples
{
    /// <summary>
    /// Azure Cognitive Services - AI Models as a Service
    /// Demonstrates: Vision, Language, Speech, Decision services
    /// Production pattern: Pre-trained models for common AI tasks
    /// Benefit: No ML expertise needed - use Azure's trained models
    /// </summary>
    public class CognitiveServicesExample
    {
        private readonly TextAnalyticsClient _textAnalyticsClient;
        private readonly ImageAnalysisClient _visionClient;
        private readonly ILogger<CognitiveServicesExample> _logger;

        public CognitiveServicesExample(
            TextAnalyticsClient textAnalyticsClient,
            ImageAnalysisClient visionClient,
            ILogger<CognitiveServicesExample> logger)
        {
            _textAnalyticsClient = textAnalyticsClient;
            _visionClient = visionClient;
            _logger = logger;
        }

        // ============================================================================
        // Pattern 1: Sentiment Analysis
        // ============================================================================
        /// <summary>
        /// Analyze text sentiment: Positive, Negative, Neutral, Mixed
        /// Use case: Customer feedback analysis, review classification
        /// Accuracy: ~94% for standard English text
        /// Performance: 100ms for single document
        /// </summary>
        public async Task<SentimentAnalysis> AnalyzeSentimentAsync(string text)
        {
            try
            {
                _logger.LogInformation("→ Analyzing sentiment of text");

                // Analyze sentiment
                var result = await _textAnalyticsClient.AnalyzeSentimentAsync(
                    text,
                    "en"); // English language

                // Extract results
                var analysis = new SentimentAnalysis
                {
                    OverallSentiment = result.Value.Sentiment.ToString(),
                    PositiveScore = result.Value.ConfidenceScores.Positive,
                    NegativeScore = result.Value.ConfidenceScores.Negative,
                    NeutralScore = result.Value.ConfidenceScores.Neutral,
                    Sentences = new List<SentenceSentiment>()
                };

                // Analyze sentence-level sentiment
                foreach (var sentence in result.Value.Sentences)
                {
                    analysis.Sentences.Add(new SentenceSentiment
                    {
                        Text = sentence.Text,
                        Sentiment = sentence.Sentiment.ToString(),
                        Score = sentence.ConfidenceScores.Positive
                    });
                }

                _logger.LogInformation($"✓ Sentiment: {analysis.OverallSentiment} " +
                    $"(Positive: {analysis.PositiveScore:P}, Negative: {analysis.NegativeScore:P})");

                return analysis;
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError($"✗ Sentiment analysis failed: {ex.Message}");
                throw;
            }
        }

        // ============================================================================
        // Pattern 2: Named Entity Recognition (NER)
        // ============================================================================
        /// <summary>
        /// Extract entities from text: Person, Location, Organization, Product, etc.
        /// Use case: Resume parsing, news article categorization, contract analysis
        /// Example: "John Smith works at Microsoft in Seattle"
        /// Extracts: John Smith (Person), Microsoft (Organization), Seattle (Location)
        /// </summary>
        public async Task<List<EntityInfo>> ExtractEntitiesAsync(string text)
        {
            try
            {
                _logger.LogInformation("→ Extracting named entities");

                var result = await _textAnalyticsClient.RecognizeEntitiesAsync(text, "en");

                var entities = new List<EntityInfo>();

                foreach (var entity in result.Value)
                {
                    entities.Add(new EntityInfo
                    {
                        Text = entity.Text,
                        Category = entity.Category.ToString(),
                        SubCategory = entity.SubCategory,
                        ConfidenceScore = entity.ConfidenceScore
                    });

                    _logger.LogInformation($"  Entity: {entity.Text} ({entity.Category})");
                }

                _logger.LogInformation($"✓ Found {entities.Count} entities");
                return entities;
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError($"✗ Entity extraction failed: {ex.Message}");
                throw;
            }
        }

        // ============================================================================
        // Pattern 3: Key Phrase Extraction
        // ============================================================================
        /// <summary>
        /// Extract main topics from text
        /// Use case: Document summarization, search indexing, content tagging
        /// Example: "The quick brown fox jumps over the lazy dog"
        /// Extracts: "quick brown fox", "lazy dog"
        /// </summary>
        public async Task<List<string>> ExtractKeyPhrasesAsync(string text)
        {
            try
            {
                _logger.LogInformation("→ Extracting key phrases");

                var result = await _textAnalyticsClient.ExtractKeyPhrasesAsync(text, "en");

                var phrases = new List<string>();
                foreach (var phrase in result.Value)
                {
                    phrases.Add(phrase);
                    _logger.LogInformation($"  Phrase: {phrase}");
                }

                _logger.LogInformation($"✓ Found {phrases.Count} key phrases");
                return phrases;
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError($"✗ Key phrase extraction failed: {ex.Message}");
                throw;
            }
        }

        // ============================================================================
        // Pattern 4: Image Analysis (Vision)
        // ============================================================================
        /// <summary>
        /// Analyze images: Detect objects, faces, text (OCR), scene description
        /// Use cases: Product recognition in e-commerce, document scanning, accessibility
        /// Supported: JPEG, PNG, GIF, BMP, WEBP (up to 50MB)
        /// Performance: 500ms per image
        /// </summary>
        public async Task<ImageAnalysisResult> AnalyzeImageAsync(string imageUrl)
        {
            try
            {
                _logger.LogInformation($"→ Analyzing image from {imageUrl}");

                // Create image source
                var imageSource = ImageSource.FromUrl(new Uri(imageUrl));

                // Analyze with multiple features
                var analysisOptions = new ImageAnalysisOptions
                {
                    Features =
                    {
                        ImageAnalysisFeature.Objects,      // Detect objects
                        ImageAnalysisFeature.Tags,         // Scene tags
                        ImageAnalysisFeature.Text,         // OCR (text detection)
                        ImageAnalysisFeature.Faces         // Face detection
                    },
                    Language = "en"
                };

                var result = await _visionClient.AnalyzeAsync(imageSource, analysisOptions);

                var analysis = new ImageAnalysisResult
                {
                    Description = result.Value.Caption.Text,
                    Objects = new List<string>(),
                    Tags = new List<string>(),
                    TextLines = new List<string>(),
                    FaceCount = result.Value.Faces.Count
                };

                // Extract detected objects
                foreach (var obj in result.Value.Objects)
                {
                    analysis.Objects.Add($"{obj.Tags[0].Name} (confidence: {obj.Tags[0].Confidence:P})");
                }

                // Extract tags
                foreach (var tag in result.Value.Tags)
                {
                    analysis.Tags.Add($"{tag.Name} ({tag.Confidence:P})");
                }

                // Extract text (OCR)
                foreach (var line in result.Value.Text.Lines)
                {
                    analysis.TextLines.Add(line.Text);
                }

                _logger.LogInformation($"✓ Image analyzed");
                _logger.LogInformation($"  Description: {analysis.Description}");
                _logger.LogInformation($"  Objects: {analysis.Objects.Count}");
                _logger.LogInformation($"  Faces: {analysis.FaceCount}");

                return analysis;
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError($"✗ Image analysis failed: {ex.Message}");
                throw;
            }
        }

        // ============================================================================
        // Pattern 5: Language Detection
        // ============================================================================
        /// <summary>
        /// Detect language of text
        /// Supports: 120+ languages
        /// Use case: Multi-language support, content classification
        /// Accuracy: 99% for text > 3 words
        /// </summary>
        public async Task<LanguageDetection> DetectLanguageAsync(string text)
        {
            try
            {
                _logger.LogInformation("→ Detecting language");

                var result = await _textAnalyticsClient.DetectLanguageAsync(text);

                var detection = new LanguageDetection
                {
                    PrimaryLanguage = result.Value.PrimaryLanguage.Name,
                    LanguageCode = result.Value.PrimaryLanguage.Iso6391Name,
                    ConfidenceScore = result.Value.PrimaryLanguage.ConfidenceScore
                };

                _logger.LogInformation($"✓ Detected language: {detection.PrimaryLanguage} ({detection.LanguageCode})");
                return detection;
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError($"✗ Language detection failed: {ex.Message}");
                throw;
            }
        }

        // ============================================================================
        // Pattern 6: Batch Processing (Multiple Documents)
        // ============================================================================
        /// <summary>
        /// Analyze multiple documents efficiently in one batch
        /// Cost: Same price per document, reduced latency
        /// Limit: Up to 25 documents per batch
        /// Performance: 1 second for 25 documents (vs 25 seconds individual)
        /// </summary>
        public async Task<List<SentimentAnalysis>> AnalyzeBatchSentimentAsync(List<string> documents)
        {
            try
            {
                _logger.LogInformation($"→ Analyzing sentiment for {documents.Count} documents");

                var results = new List<SentimentAnalysis>();
                var batchSize = 25; // Azure limit

                for (int i = 0; i < documents.Count; i += batchSize)
                {
                    var batch = documents.GetRange(i, Math.Min(batchSize, documents.Count - i));

                    // Analyze batch
                    var batchResults = await _textAnalyticsClient
                        .AnalyzeSentimentBatchAsync(batch, "en");

                    foreach (var result in batchResults.Value)
                    {
                        results.Add(new SentimentAnalysis
                        {
                            OverallSentiment = result.Sentiment.ToString(),
                            PositiveScore = result.ConfidenceScores.Positive,
                            NegativeScore = result.ConfidenceScores.Negative,
                            NeutralScore = result.ConfidenceScores.Neutral
                        });
                    }

                    _logger.LogInformation($"  Processed {Math.Min(batchSize, documents.Count - i)} documents");
                }

                _logger.LogInformation($"✓ Batch analysis complete");
                return results;
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError($"✗ Batch analysis failed: {ex.Message}");
                throw;
            }
        }

        // ============================================================================
        // Pattern 7: Pricing and Cost Optimization
        // ============================================================================
        /// <summary>
        /// Cognitive Services pricing model and cost optimization
        /// </summary>
        public static class PricingAndOptimization
        {
            public const string PRICING_MODEL = @"
                Free Tier: 5,000 API calls/month
                  - Text Analytics: 5,000 records/month
                  - Vision: 5,000 calls/month
                  - Good for: Testing, small applications

                Standard Tier: Pay per API call
                  - Text Analytics: $1 per 1,000 records
                  - Image Analysis: $1 per 1,000 calls
                  - Speech-to-Text: $1 per 1 hour of audio
                  - Good for: Production applications";

            public const string COST_OPTIMIZATION = @"
                1. Batch Processing
                   ✓ Process 25 documents together (vs 1 at a time)
                   ✓ Same cost, reduced latency
                   ✓ Reduces 25 API calls to 1 call

                2. Caching Results
                   ✓ Cache sentiment analysis results (1 hour)
                   ✓ Reuse NER results if text same
                   ✓ Avoid re-analyzing same input

                3. Choose Right Service
                   ✓ Simple text classification? Use Text Analytics
                   ✓ Need OCR? Use Vision API
                   ✓ Using both? Use Vision only (has both)

                4. Optimize Image Size
                   ✓ Resize large images before analysis
                   ✓ 10MB image vs 1MB image = same price
                   ✓ Smaller = faster processing";
        }

        // ============================================================================
        // Comparison: Cognitive Services vs Custom ML
        // ============================================================================
        public static class CognitiveServicesVsCustomML
        {
            public const string COGNITIVE_SERVICES = @"
                Pre-trained models ready to use

                Pros:
                  ✓ No ML expertise required
                  ✓ Fast deployment (minutes)
                  ✓ Continuously updated by Microsoft
                  ✓ Works across languages/domains
                  ✓ Simple API (1-2 lines of code)

                Cons:
                  ✗ Less customizable
                  ✗ Generic models (may not fit niche)
                  ✗ Pay per API call (not per training)

                Use when:
                  - Need quick solution
                  - Standard task (sentiment, NER, translation)
                  - No domain-specific requirements";

            public const string CUSTOM_MACHINE_LEARNING = @"
                Build your own ML model with your data

                Pros:
                  ✓ Highly customizable
                  ✓ Optimized for your domain
                  ✓ Can integrate custom features
                  ✓ Cost-effective at scale (train once)

                Cons:
                  ✗ Requires ML expertise
                  ✗ Slow deployment (weeks/months)
                  ✗ Need labeled training data
                  ✗ Requires model management

                Use when:
                  - Have domain-specific data
                  - Cognitive Services doesn't fit
                  - High volume (thousands of requests)";

            public const string RECOMMENDATION = @"
                Start with Cognitive Services
                → Get 80% results with 20% effort
                → Iterate if needed

                Move to custom ML only if:
                - Accuracy insufficient
                - Specific domain requirements
                - Cost analysis shows training benefit";
        }
    }

    // ============================================================================
    // Supporting Models
    // ============================================================================
    public class SentimentAnalysis
    {
        public string OverallSentiment { get; set; }
        public double PositiveScore { get; set; }
        public double NegativeScore { get; set; }
        public double NeutralScore { get; set; }
        public List<SentenceSentiment> Sentences { get; set; }
    }

    public class SentenceSentiment
    {
        public string Text { get; set; }
        public string Sentiment { get; set; }
        public double Score { get; set; }
    }

    public class EntityInfo
    {
        public string Text { get; set; }
        public string Category { get; set; }
        public string SubCategory { get; set; }
        public double ConfidenceScore { get; set; }
    }

    public class ImageAnalysisResult
    {
        public string Description { get; set; }
        public List<string> Objects { get; set; }
        public List<string> Tags { get; set; }
        public List<string> TextLines { get; set; }
        public int FaceCount { get; set; }
    }

    public class LanguageDetection
    {
        public string PrimaryLanguage { get; set; }
        public string LanguageCode { get; set; }
        public double ConfidenceScore { get; set; }
    }
}
