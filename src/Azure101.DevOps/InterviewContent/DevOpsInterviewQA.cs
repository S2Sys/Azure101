using System;
using System.Collections.Generic;

namespace Azure101.DevOps.InterviewContent
{
    public static class DevOpsInterviewQA
    {
        public static List<(string Question, string Answer, string Category)> GetQuestions() => new()
        {
            ("Design a CI/CD pipeline for .NET microservices deployed to AKS",
            "1. Trigger: Code commit to main. 2. Build: dotnet build, dotnet test (fail if tests fail). 3. Quality: SonarQube scan (code quality). 4. Docker: Build image, scan for vulnerabilities. 5. Push: Upload to Container Registry. 6. Deploy Staging: AKS staging cluster, run smoke tests. 7. Manual Approval. 8. Deploy Prod: AKS production, canary rollout (10%→50%→100%). 9. Monitor: App Insights alerts.",
            "CI/CD Pipeline"),

            ("You deployed code and it caused production outage. How do you rollback?",
            "Immediate (< 1 minute): kubectl rollout undo deployment/myapp (Kubernetes auto-rollback). Manual: Redeploy previous version from artifact. Infrastructure: Switch traffic to previous region. Database: Use point-in-time restore if data corrupted. Notify: Page oncall, send incident alert. Post-mortem: What failed? Why not caught in tests?",
            "Incident Response"),

            ("How do you monitor a production .NET application for issues?",
            "1. Application Insights: APM - request rate, response time, exception rate. 2. Log Analytics: Aggregate logs from all services. 3. Azure Monitor: Infrastructure metrics (CPU, memory). 4. Alerts: Notify if error rate > 1% or latency > 1 second. 5. Dashboards: Executive view, developer view. 6. Trends: Daily/weekly patterns.",
            "Monitoring"),

            ("Design a logging strategy for 100 microservices",
            "1. Centralized: All logs → Log Analytics workspace. 2. Structured logging: JSON format with context (service, trace-id, user). 3. Log levels: DEBUG (local), INFO (info), WARN (problems), ERROR (failures). 4. Correlation: Add trace-id to all requests for cross-service tracing. 5. Retention: 30 days hot, archive to blob storage. 6. Cost: ~$5-10/GB ingested.",
            "Logging Strategy"),

            ("How do you manage secrets in CI/CD pipeline?",
            "Never in code. Use: 1. Key Vault for secrets. 2. Managed Identity for pipeline auth. 3. Variable groups in Azure Pipelines (encrypted). 4. SonarQube scan to detect exposed secrets. 5. Rotate secrets every 90 days. 6. Audit: Log all secret access.",
            "Secrets Management"),

            ("Implement health checks for microservices",
            "1. HTTP GET /health endpoint returns 200 if healthy. 2. Checks: Database connection, cache health, external APIs. 3. Liveness: Is service alive? (Kill if not, auto-restart). 4. Readiness: Is service ready for traffic? (Wait before routing). 5. Kubernetes: kubelet calls health checks, auto-restarts failed pods.",
            "Health Checks")
        };
    }
}
