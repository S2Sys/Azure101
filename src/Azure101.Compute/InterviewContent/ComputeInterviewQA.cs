using Azure101.Documentation.Models;

#nullable enable

namespace Azure101.Compute.InterviewContent;

/// <summary>
/// Interview questions and answers for Azure Compute services
/// Covers VMs, App Service, ACI, AKS, Functions, and Batch
/// </summary>
public static class ComputeInterviewQA
{
    public static List<InterviewQuestion> GetVMQuestions() => new()
    {
        new InterviewQuestion
        {
            Question = "When would you use Virtual Machines vs. App Service vs. Functions?",
            Answer = "VMs (IaaS) for custom OS/runtime, legacy apps, or high control needs. App Service (PaaS) for web apps/APIs when you don't need OS-level control. Functions (Serverless) for event-driven, short-lived code with automatic scaling. Choose based on control vs. convenience tradeoff.",
            DetailedExplanation = "VMs: Most control, highest responsibility (patching, updates, security). App Service: Good balance, native support for .NET/Java/Node. Functions: Minimal ops, automatic scaling, pay-per-execution. Architecture question: How much ops burden do you want?",
            Category = "Service Comparison",
            Difficulty = InterviewDifficulty.Beginner,
            RelatedTopics = new[] { "IaaS", "PaaS", "Serverless" },
            Tags = new[] { "fundamental", "architecture", "compute-choices" }
        },

        new InterviewQuestion
        {
            Question = "What are availability options for Azure VMs and what's the difference between availability sets and availability zones?",
            Answer = "Availability Sets ensure VMs are distributed across multiple update/fault domains within a region (protects against planned and unplanned downtime). Availability Zones physically separate datacenters within a region. AZs provide better protection than Availability Sets but cost more.",
            DetailedExplanation = "Single VM: No guarantee. Availability Set: 99.95% SLA with 2+ VMs. Availability Zones: 99.99% SLA with VMs in different zones. Load Balancer required for both. AZs is newer and preferred.",
            Category = "Availability",
            Difficulty = InterviewDifficulty.Intermediate,
            RelatedTopics = new[] { "SLA", "Disaster recovery", "High availability" },
            Tags = new[] { "availability", "zones", "sla" }
        }
    };

    public static List<InterviewQuestion> GetAppServiceQuestions() => new()
    {
        new InterviewQuestion
        {
            Question = "Explain App Service pricing tiers and when to use each",
            Answer = "Free/Shared: Dev/test only, limited resources. Basic: Development, single instance. Standard: Production, auto-scale, SSL. Premium: Enhanced performance, isolated networking. Isolated: Complete isolation, highest security. Choose based on SLA, scale, and isolation needs.",
            DetailedExplanation = "Typical progression: Dev (Free/Basic) → Prod (Standard with auto-scale) → High-security (Premium/Isolated). Each tier includes more features and better SLAs. Cost increases significantly, so benchmark your app.",
            Category = "Pricing",
            Difficulty = InterviewDifficulty.Intermediate,
            RelatedTopics = new[] { "Scaling", "Cost optimization", "SLA" },
            Tags = new[] { "app-service", "pricing", "tiers" }
        },

        new InterviewQuestion
        {
            Question = "How does auto-scaling work in App Service and what metrics trigger it?",
            Answer = "Auto-scale rules based on metrics like CPU %, memory, HTTP queue length, or custom metrics. When metric exceeds threshold, scale-out adds instances. When below threshold, scale-in removes instances. Set min/max bounds to control cost.",
            DetailedExplanation = "Best practice: Scale out quickly (aggressive), scale in slowly (prevent thrashing). Monitor actual app metrics. Test scale rules with load testing. Can take minutes to add instances, so plan accordingly.",
            Category = "Performance",
            Difficulty = InterviewDifficulty.Intermediate,
            RelatedTopics = new[] { "Performance tuning", "Cost management", "Load testing" },
            Tags = new[] { "app-service", "scaling", "performance" }
        }
    };

    public static List<InterviewQuestion> GetFunctionsQuestions() => new()
    {
        new InterviewQuestion
        {
            Question = "What is Azure Functions and what trigger types are supported?",
            Answer = "Azure Functions is serverless computing - execute code in response to events without managing infrastructure. Triggers: HTTP, Timer, Queue, Blob, Cosmos DB, Service Bus, Event Hub, Event Grid, etc. Pay only for code execution time.",
            DetailedExplanation = "Cold start latency (first invocation after idle) can be seconds. Consumption plan is cheapest but unpredictable cost with scaling. Premium plan guarantees performance. Duration limit: 10 min default (configurable to 60 min).",
            Category = "Overview",
            Difficulty = InterviewDifficulty.Beginner,
            RelatedTopics = new[] { "Serverless", "Event-driven", "Triggers" },
            Tags = new[] { "functions", "serverless", "overview" }
        },

        new InterviewQuestion
        {
            Question = "When would you use Azure Functions vs. Logic Apps?",
            Answer = "Functions: Code-first, low-latency, complex logic. Logic Apps: Visual workflow, B2B scenarios, integrations. Functions better for performance-critical code, Logic Apps for enterprise integrations and non-developers.",
            DetailedExplanation = "Functions: Write C#, Python, Node.js, etc. Full dev experience. Logic Apps: Visual designer (low-code), better for workflows without code. Both serverless, both integrate with Azure services.",
            Category = "Architecture",
            Difficulty = InterviewDifficulty.Intermediate,
            RelatedTopics = new[] { "Serverless", "Integration", "Low-code" },
            Tags = new[] { "functions", "logic-apps", "comparison" }
        }
    };

    public static List<InterviewQuestion> GetContainerQuestions() => new()
    {
        new InterviewQuestion
        {
            Question = "What is the difference between Azure Container Instances (ACI) and Azure Kubernetes Service (AKS)?",
            Answer = "ACI: Simplest way to run containers, no orchestration complexity, good for lightweight workloads. AKS: Full Kubernetes orchestration, production-grade, handles scaling, deployment, networking. Choose ACI for simple cases, AKS for complex/production workloads.",
            DetailedExplanation = "ACI: Seconds to start, no cluster management, pay-per-second. AKS: Sophisticated orchestration, service mesh support, auto-scaling across nodes. AKS has higher baseline cost due to control plane.",
            Category = "Service Comparison",
            Difficulty = InterviewDifficulty.Intermediate,
            RelatedTopics = new[] { "Containers", "Orchestration", "Kubernetes" },
            Tags = new[] { "aci", "aks", "containers" }
        }
    };

    public static List<InterviewQuestion> GetCommonComputeQuestions() => new()
    {
        new InterviewQuestion
        {
            Question = "How do you monitor Azure compute resources effectively?",
            Answer = "Use Application Insights for application monitoring, Azure Monitor for infrastructure metrics, Log Analytics for log aggregation. Set up alerts for critical metrics (CPU, memory, exceptions). Enable diagnostics for detailed troubleshooting.",
            DetailedExplanation = "Application Insights: Dependency tracking, failures, performance metrics. Azure Monitor: Infrastructure health, availability. Log Analytics: Aggregate logs across resources, KQL for analysis. Combination gives full visibility.",
            Category = "Operations",
            Difficulty = InterviewDifficulty.Intermediate,
            RelatedTopics = new[] { "Monitoring", "Diagnostics", "Alerting" },
            Tags = new[] { "monitoring", "operations", "troubleshooting" }
        },

        new InterviewQuestion
        {
            Question = "What are best practices for securing Azure compute resources?",
            Answer = "Use Managed Identity instead of keys/secrets. Implement NSGs and Azure Firewall for network security. Enable Azure Defender (threat protection). Use private endpoints to avoid internet exposure. Keep OS/runtime patched. Store secrets in Key Vault.",
            DetailedExplanation = "Security layers: Identity (Managed Identity/RBAC), Network (NSG/Firewall), Application (secure code), Data (encryption), Operations (patching). Defense in depth approach.",
            Category = "Security",
            Difficulty = InterviewDifficulty.Advanced,
            RelatedTopics = new[] { "Identity", "Networking", "Compliance" },
            Tags = new[] { "security", "best-practice", "defense-in-depth" }
        }
    };

    /// <summary>
    /// Gets all compute interview questions
    /// </summary>
    public static List<InterviewQuestion> GetAllQuestions()
    {
        var allQuestions = new List<InterviewQuestion>();
        allQuestions.AddRange(GetVMQuestions());
        allQuestions.AddRange(GetAppServiceQuestions());
        allQuestions.AddRange(GetFunctionsQuestions());
        allQuestions.AddRange(GetContainerQuestions());
        allQuestions.AddRange(GetCommonComputeQuestions());
        return allQuestions;
    }
}
