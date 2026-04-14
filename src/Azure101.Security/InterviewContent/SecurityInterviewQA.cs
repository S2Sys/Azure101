using System;
using System.Collections.Generic;

namespace Azure101.Security.InterviewContent
{
    public static class SecurityInterviewQA
    {
        public static List<(string Question, string Answer, string Category)> GetQuestions() => new()
        {
            ("How do you securely store database connection strings in Azure?",
            "Use Key Vault (never hardcode). App uses Managed Identity to retrieve secret at runtime. Audit trail logged.",
            "Secrets Management"),

            ("Design authentication for a .NET API accessed by web and mobile clients",
            "1. Web: Use Azure AD OAuth2 (redirect flow). 2. Mobile: Use Azure AD with refresh tokens. 3. API: Validate JWT tokens in middleware. 4. All: Token expires in 1 hour, refresh extends session.",
            "Authentication"),

            ("Implement least privilege access for developers accessing production databases",
            "Use RBAC with roles like 'db_datareader' (read-only). Developers get temporary access via Just-In-Time (JIT) - auto-revokes after 2 hours. All access logged in Log Analytics.",
            "Authorization"),

            ("How do you handle secrets rotation for API keys?",
            "1. Store in Key Vault. 2. Set rotation schedule (90 days). 3. New key generated before expiry. 4. App refreshes on next cache miss. 5. Old key disabled after grace period.",
            "Secrets Management"),

            ("Design zero-trust network security for microservices",
            "1. Verify identity (Azure AD MFA). 2. Verify network (VPN/private endpoint). 3. Verify device (compliant only). 4. Encrypt (TLS everywhere). 5. Log everything (audit trail). 6. Assume compromise (detect lateral movement).",
            "Network Security"),

            ("Implement Web Application Firewall (WAF) rules to prevent OWASP Top 10 attacks",
            "1. SQL Injection: Detect SQL keywords in params. 2. XSS: Filter script tags. 3. DDoS: Rate limit per IP. 4. Bot: Challenge with CAPTCHA. 5. Malware: Scan uploads. 6. Log all blocks to Log Analytics.",
            "WAF Protection")
        };
    }
}
