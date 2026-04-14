using System;
using System.Collections.Generic;

namespace Azure101.AI_ML.InterviewContent
{
    public static class AI_MLInterviewQA
    {
        public static List<(string Question, string Answer, string Category)> GetQuestions() => new()
        {
            ("When would you use Cognitive Services vs Azure Machine Learning?",
            "Cognitive Services: Pre-trained models, no ML expertise needed, fast (hours). ML: Custom models, your data, slower (weeks) but better accuracy. Use Cognitive for standard tasks (OCR, translation). Use ML for competitive advantage (fraud detection, churn prediction).",
            "Service Selection"),

            ("How would you build a chatbot for customer support?",
            "1. Use Bot Service (orchestration). 2. LUIS for intent recognition (what user wants). 3. QnA Maker for FAQ answers. 4. Azure OpenAI for natural responses. 5. Log conversations for improvement.",
            "Chatbot Development"),

            ("Design a document processing pipeline for 100k invoices/day",
            "1. Upload to Blob Storage. 2. Trigger Function on blob creation. 3. Form Recognizer API extracts fields (invoice #, date, amount). 4. Store structured data in SQL. 5. Process in batch with Stream Analytics for aggregation. Cost: $0.50 per 1000 images.",
            "Document Processing"),

            ("Implement fraud detection for credit card transactions",
            "1. Collect features: amount, location, time, merchant, user history. 2. Train ML model on historical fraud/normal. 3. Deploy as REST API. 4. Real-time: Score new transaction < 100ms. 5. If fraud probability > 0.9: Block and alert. 6. Retrain monthly on new patterns.",
            "Machine Learning"),

            ("How do you monitor ML model performance in production?",
            "1. Track accuracy metric (how many correct). 2. Monitor data drift (input changed?). 3. Alert if accuracy drops > 5%. 4. Retrain if drift detected. 5. A/B test new model vs old. 6. Log all predictions for analysis.",
            "Model Monitoring"),

            ("What's the cost of using Azure OpenAI for a SaaS chatbot?",
            "GPT-3.5: $0.03-0.06 per 1K tokens. GPT-4: $0.15 per 1K tokens. Typical conversation: 500-1000 tokens. Cost per user: $0.015-0.06. 1M users × $0.03 = $30k/month. Mitigation: Cache responses, use cheaper model, implement rate limiting.",
            "Cost Analysis")
        };
    }
}
