# Azure AI & Machine Learning Services - Complete Wiki

## Overview

Azure AI provides cognitive services (pre-trained models) and machine learning (build your own models). Choose between pre-trained APIs (Cognitive Services) or custom training (Machine Learning).

---

## Quick Comparison Table

| Service | Type | Complexity | Cost | Best For |
|---------|------|-----------|------|----------|
| **Cognitive Services** | Pre-trained API | Low | Per-request | Out-of-box AI |
| **Azure OpenAI** | Pre-trained LLM | Low | Per-token | ChatGPT-like apps |
| **Machine Learning** | Custom training | High | Per-compute | Custom models, specific data |
| **Computer Vision** | Image analysis | Low | Per-image | OCR, object detection |
| **Language** | NLP | Low | Per-request | Text analysis, translation |
| **Decision** | Recommendation | Low | Per-request | Content recommender |
| **Bot Service** | Conversational | Medium | Per-message | Chatbots, virtual agents |

---

## 1. Azure Cognitive Services

### What is it?
Pre-built AI services (APIs) requiring no ML expertise. Microsoft-trained models ready to use via REST or SDK.

### When to Use
- Need AI without ML expertise
- Quick time-to-market required
- Pre-trained models sufficient
- Scale AI across organization
- Reduce infrastructure costs

### Categories

#### Vision Services
- **Computer Vision**: Object detection, OCR, scene analysis
- **Custom Vision**: Train model on your images
- **Face**: Detect faces, verify identity
- **Form Recognizer**: Extract data from documents
- **Video Indexer**: Analyze video content

#### Language Services
- **Text Analytics**: Sentiment analysis, entity extraction
- **Translator**: Translate 100+ languages
- **Language Understanding (LUIS)**: Intent recognition
- **QnA Maker**: FAQ chatbot
- **Speech**: Text-to-speech, speech-to-text

#### Decision Services
- **Anomaly Detector**: Detect unusual patterns
- **Content Moderator**: Flag inappropriate content
- **Personalizer**: Recommend content to users

### Pros & Cons

**Pros** ✅
- No ML expertise needed
- Pre-trained by Microsoft
- Pay-per-use (no infrastructure)
- Fast integration (API calls)
- High accuracy for common tasks
- Auto-scaling

**Cons** ❌
- Limited customization
- Vendor lock-in
- Per-request cost can add up
- Privacy concerns (data sent to Microsoft)
- Not suitable for proprietary algorithms

### Limits & Constraints

| Service | Limit |
|---------|-------|
| Vision (per second) | 10-30 req/sec depending on SKU |
| Language (per second) | 10-30 req/sec |
| Translator (per second) | 10-30 req/sec |
| Batch processing | 10-100 items per batch |
| Image size (Vision) | Up to 50MB |
| Text size (Language) | Up to 5,120 characters |

### Real-World Use Cases

#### Use Case 1: Document Processing
```
Scanned Invoice
    └─ Form Recognizer API
    ├─ Extract: Invoice number, date, items, amounts
    ├─ Confidence score for each field
    └─ Return structured JSON

Result: Automate invoice processing without training
```

#### Use Case 2: Content Moderation
```
User uploads image to social platform
    └─ Content Moderator API
    ├─ Analyze for inappropriate content
    ├─ Flag if adult/offensive
    └─ Return moderation score

Human review:
    ├─ If score > 0.7, require human review
    └─ Otherwise approve automatically
```

#### Use Case 3: Multi-Language Support
```
User submits feedback in any language
    └─ Translator API
    ├─ Detect language
    ├─ Translate to English
    ├─ Text Analytics: Sentiment analysis
    └─ Store in database

Result: Support 100+ languages with one integration
```

### Performance Tips
- Batch requests where possible (saves API calls)
- Implement caching for similar inputs
- Use appropriate model sizes (smaller = faster)
- Monitor API quotas and rate limits
- Implement retry logic with exponential backoff

### Cost Optimization
- Free tier for testing (limited requests)
- Pay-per-transaction for production
- Batch API calls (cheaper per request)
- Archive old results instead of re-calling
- Use appropriate service tier (S0 vs S1)

---

## 2. Azure OpenAI Service

### What is it?
Access to OpenAI's latest models (GPT-4, GPT-3.5) via REST API with Azure's infrastructure and security.

### When to Use
- ChatGPT-like applications
- Content generation
- Code completion
- Question answering
- Text summarization
- Translation and paraphrasing

### Key Features
- **Models**: GPT-4, GPT-3.5-Turbo, Codex
- **Completions**: Text generation from prompt
- **Chat**: Conversation-based interactions
- **Embeddings**: Convert text to vectors for similarity
- **Rate Limiting**: Control API usage
- **Safety Filters**: Prevent harmful content

### Pros & Cons

**Pros** ✅
- State-of-the-art language model (GPT-4)
- Flexible use cases (chat, code, summarization)
- Azure infrastructure (compliance, SLA)
- Cost-effective ($0.03-$0.06 per 1K tokens)
- Quick integration via API

**Cons** ❌
- Limited availability (capacity)
- Token limits (4K-8K per request)
- Cost per token (adds up with usage)
- May require prompt engineering
- Occasional latency issues

### Limits & Constraints

| Resource | Limit |
|----------|-------|
| Max tokens per request | 4K (GPT-3.5) to 8K (GPT-4) |
| Rate limit | 40K-120K tokens/minute (varies by tier) |
| Concurrent requests | 10-100 (varies by tier) |
| Response timeout | 600 seconds |
| Monthly usage | No hard limit (quota approval) |

### Real-World Use Cases

#### Use Case 1: Customer Support Chatbot
```
Customer question: "How do I reset my password?"
    └─ Send to OpenAI API
    ├─ System prompt: "You are a helpful support agent"
    ├─ Model generates: "Go to settings > account > reset password"
    └─ Return to customer

Cost: ~$0.001 per question (1000 tokens = $0.03)
```

#### Use Case 2: Code Generation Assistant
```
Developer prompt: "Write a function to validate email"
    └─ Send to OpenAI API (GPT Codex)
    ├─ Model generates Python code
    ├─ Developer reviews and integrates
    └─ Saves development time

Cost: ~$0.001 per function generated
```

#### Use Case 3: Content Summarization
```
Long article (5000 words)
    └─ Send to OpenAI API
    ├─ Prompt: "Summarize in 100 words"
    ├─ Model returns summary
    └─ Display to user

Cost: ~$0.15 per article (5000 tokens)
```

### Performance Tips
- Use embeddings for similarity searches (cheaper)
- Fine-tune on common use cases
- Implement caching for repeated prompts
- Batch requests where possible
- Use appropriate model (GPT-3.5 for most cases, GPT-4 for complex)

### Cost Optimization
- Use GPT-3.5-Turbo (cheaper than GPT-4)
- Reduce token usage (shorter prompts)
- Implement caching
- Use embeddings instead of full completions
- Archive responses to avoid re-calling

---

## 3. Azure Machine Learning

### What is it?
End-to-end platform for building, training, and deploying custom ML models. For data scientists and ML engineers.

### When to Use
- Custom ML models needed
- Have proprietary/sensitive data
- Need specialized algorithms
- Control over model accuracy
- Specific business logic requirements

### Key Features
- **Data Preparation**: Clean and preprocess data
- **AutoML**: Automatically try many algorithms
- **Jupyter Notebooks**: Interactive development
- **Pipelines**: Orchestrate training workflows
- **Model Registry**: Version and manage models
- **Endpoints**: Deploy models as web services

### Pros & Cons

**Pros** ✅
- Complete control over model
- Use proprietary algorithms
- Data stays in your infrastructure
- AutoML reduces expertise needed
- MLOps capabilities

**Cons** ❌
- Requires ML expertise
- Longer development time
- Infrastructure costs
- Complex setup and management
- Maintenance burden

### Limits & Constraints

| Resource | Limit |
|----------|-------|
| Training time | Up to 30 days per job |
| Dataset size | Depends on compute (GB to TB) |
| Models per workspace | 1000+ |
| Concurrent training jobs | Varies by compute |

### Real-World Use Cases

#### Use Case 1: Customer Churn Prediction
```
Training Data:
├── Customer demographics
├── Usage patterns
├── Support tickets
└── Churn outcome (yes/no)

Azure ML:
├── Train model on historical data
├── Model learns patterns
└── Achieves 85% accuracy

Deployment:
├── Real-time scoring endpoint
├── Predict churn for new customers
└── Proactive retention actions
```

#### Use Case 2: Fraud Detection
```
Training Data:
├── 10M historical transactions
├── Features: amount, merchant, location, time
└── Fraud label (yes/no)

Azure ML:
├── Train deep learning model
├── Achieve 99% accuracy
└── Deploy to production

Real-time:
├── Incoming transaction scored
├── Fraud probability returned
└── Block if > 0.9 probability
```

---

## 4. Computer Vision API

### What is it?
Analyze images to extract information (objects, faces, text, description).

### When to Use
- Detect objects in images
- OCR (text extraction)
- Face detection/recognition
- Image description generation
- Quality assessment

### Key Features
- **Object Detection**: Identify what's in image
- **OCR**: Extract text from images/documents
- **Face Detection**: Locate and identify faces
- **Image Analysis**: Generate descriptions
- **Brand Detection**: Identify logos
- **Color Analysis**: Analyze image colors

### Real-World Use Cases

#### Use Case 1: Retail Inventory
```
Warehouse image
    └─ Computer Vision API
    ├─ Detect: Shelf layout, missing items
    ├─ Count products visible
    └─ Alert if shelf empty

Cost: ~$1 per 1000 images
```

#### Use Case 2: Document Scanning
```
Scanned receipt
    └─ Computer Vision API (OCR)
    ├─ Extract: Merchant, date, items, amounts
    └─ Return as structured data

Accuracy: 95%+ for clear documents
```

---

## 5. Language Understanding (LUIS)

### What is it?
Natural language understanding API to extract intent and entities from user input.

### When to Use
- Chatbot intent recognition
- Voice command processing
- Text classification
- Entity extraction

### Key Features
- **Intent**: Understand user goal ("book flight" vs "cancel booking")
- **Entities**: Extract relevant data ("Monday" from "book flight for Monday")
- **Prebuilt Models**: Common intents/entities
- **Active Learning**: Improve model over time

### Real-World Use Cases

#### Use Case 1: Chatbot Intent Routing
```
User: "I want to book a flight from Seattle to Boston next Monday"
    └─ LUIS API
    ├─ Intent: BookFlight
    ├─ Entities: 
    │   ├── origin: Seattle
    │   ├── destination: Boston
    │   └── date: next Monday
    └─ Route to flight booking service

Handling:
    ├─ Collect missing info (class, airlines)
    └─ Complete booking
```

---

## Architecture Patterns

### Pattern 1: Pre-trained (Quick) vs Custom (Accurate)
```
Simple Use Case
    └─ Cognitive Services API
    ├─ Computer Vision for OCR
    ├─ Text Analytics for sentiment
    ├─ Cost: $1-10/month
    └─ Time: Hours

Complex Use Case
    └─ Azure Machine Learning
    ├─ Train on proprietary data
    ├─ Achieve 99% accuracy
    ├─ Cost: $100-1000/month
    └─ Time: Weeks
```

### Pattern 2: AI Pipeline
```
Raw Data
    └─ Data Preparation (clean, transform)
    └─ Feature Engineering (extract signals)
    └─ Model Training (train multiple models)
    └─ Model Evaluation (pick best)
    └─ Deployment (serve predictions)
    └─ Monitoring (track accuracy drift)
```

---

## Interview Questions

1. **Choose between Cognitive Services vs Machine Learning**
   - Cognitive Services: Out-of-box AI, no expertise needed
   - Machine Learning: Custom models, proprietary data, control

2. **Design chatbot application**
   - Language Understanding (LUIS) for intent recognition
   - Bot Service for conversation orchestration
   - Azure OpenAI for natural responses
   - SQL Database for context/history

3. **Build fraud detection system**
   - Data preparation: Clean features, handle class imbalance
   - Feature engineering: Behavioral signals
   - Train multiple models (Random Forest, XGBoost, Neural Network)
   - Deploy best model as real-time endpoint
   - Monitor for model drift

4. **Computer Vision for document processing**
   - Form Recognizer for structured documents (invoices, receipts)
   - Computer Vision OCR for unstructured text
   - Extract and validate data
   - Store in database

5. **Cost optimization for AI**
   - Use Cognitive Services for standard tasks (cheaper)
   - Custom ML only for differentiation
   - Batch processing for non-real-time
   - Cache predictions for repeated inputs

---

## Cost Optimization Tips

1. **Cognitive Services**: $0.001-$0.01 per request
2. **Azure OpenAI**: $0.03-$0.06 per 1K tokens
3. **Computer Vision**: $1 per 1000 images
4. **Machine Learning**: $0.30-$3/hour compute
5. **Optimize**: Batch, cache, choose right tier

---

**Last Updated**: April 2026  
**Difficulty**: Intermediate to Advanced  
**Focus**: Pre-trained AI, custom ML, model deployment
