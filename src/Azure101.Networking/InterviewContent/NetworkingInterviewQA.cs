using System;
using System.Collections.Generic;

namespace Azure101.Networking.InterviewContent
{
    /// <summary>
    /// Interview Q&A for Azure Networking Services
    /// Topics: VNets, NSG, Load Balancers, Application Gateways, CDN, Firewalls, etc.
    /// </summary>
    public static class NetworkingInterviewQA
    {
        public static List<InterviewQuestion> GetQuestions() => new()
        {
            // ============================================================================
            // VNet & Subnet Design Questions
            // ============================================================================
            new InterviewQuestion
            {
                Question = "Design a VNet for a 3-tier web application with frontend, application, and database layers",
                Answer = @"
VNet Design: 10.0.0.0/16 with 3 subnets:

1. Frontend Subnet: 10.0.1.0/24 (256 IPs)
   - NSG: Allow HTTP (80), HTTPS (443) from Internet
   - Allow RDP (3389) from bastion only
   - Deny database access

2. Application Subnet: 10.0.2.0/24 (256 IPs)
   - NSG: Allow traffic only from frontend subnet
   - Allow outbound to database subnet (port 1433)
   - Deny direct internet access (use outbound NAT if needed)

3. Database Subnet: 10.0.3.0/24 (256 IPs)
   - NSG: Allow SQL (1433) ONLY from application subnet
   - Allow RDP (3389) from bastion subnet
   - Deny internet access completely

Benefits:
✓ Security isolation per tier
✓ Principle of least privilege (only necessary traffic)
✓ Easy to audit and troubleshoot
✓ Scales horizontally (add more VMs per subnet)

Subnet Sizing:
- Use /24 for medium deployments (254 usable IPs)
- Use /25 or /26 if smaller (128 or 64 IPs)
- Reserve space for growth (don't use /32)

Note: Cannot change CIDR after VNet creation - plan carefully!
                ",
                Difficulty = InterviewDifficulty.Intermediate,
                Category = "VNet Design",
                Tags = new[] { "networking", "security", "architecture", "design" }
            },

            new InterviewQuestion
            {
                Question = "Explain NSG rule priority and order of evaluation. What happens when multiple rules match?",
                Answer = @"
NSG Rule Evaluation:

1. Priority Range: 0-4096 (lower = higher priority)
2. Evaluation Order: Lowest priority number first
3. First Match Wins: Processing stops at first matching rule

Example Rules:
Rule 1: Priority 100 - Allow port 443 from Internet
Rule 2: Priority 200 - Deny all traffic
Rule 3: Priority 300 - Allow port 8080 from subnet

Traffic: Internet → port 443
├── Rule 1 matches (priority 100)
├── Action: ALLOW
└── No further evaluation needed

Traffic: Internet → port 22 (SSH)
├── Rule 1: No match (not port 443)
├── Rule 2: Matches (priority 200)
├── Action: DENY
└── No further evaluation needed

Best Practices:
✓ Use gaps (100, 200, 300) for easy insertion
✓ Lower numbers for common rules (less evaluation)
✓ Group related rules together (10x, 20x, etc)
✓ Document why each rule exists
✓ Use Application Security Groups (ASGs) for flexibility
✓ Review rules quarterly for cleanup

Common Mistake:
Rule 1 (100): Deny all
Rule 2 (200): Allow port 443
Result: Port 443 is BLOCKED (rule 1 matches first)

Correct approach:
Rule 1 (100): Allow port 443
Rule 2 (200): Deny all
Result: Port 443 is ALLOWED, everything else DENIED
                ",
                Difficulty = InterviewDifficulty.Intermediate,
                Category = "NSG Rules",
                Tags = new[] { "networking", "security", "nsg", "rules" }
            },

            new InterviewQuestion
            {
                Question = "When would you use a Load Balancer vs Application Gateway vs Front Door?",
                Answer = @"
Three Load Balancing Solutions:

1. LOAD BALANCER (Layer 4 - TCP/UDP)
   Use when:
   ├── Need ultra-low latency (< 1ms)
   ├── Non-HTTP protocols (UDP, SQL, gaming)
   ├── Millions of requests/second
   ├── Internal service distribution
   └── Cost is critical (cheapest option)

   Example: Database query distribution
   10.0.4.1 (Load Balancer) → Distributes SQL queries
   ├── Backend 1: Query processor (port 1433)
   ├── Backend 2: Query processor
   └── Backend 3: Query processor

   Cost: ~$40/month


2. APPLICATION GATEWAY (Layer 7 - HTTP/HTTPS)
   Use when:
   ├── Web applications (HTTP/HTTPS only)
   ├── Need URL routing (/api → different backend)
   ├── Host header routing (tenant1.com → different backend)
   ├── SSL/TLS termination needed
   ├── WAF (Web Application Firewall) required
   └── Regional deployment (not global)

   Example: Microservices routing
   /orders/* → Orders Service Backend
   /payments/* → Payments Service Backend
   /api/* → API Service Backend
   /admin/* → Admin Service Backend

   Cost: ~$250-400/month


3. FRONT DOOR (Layer 7 - Global)
   Use when:
   ├── Global users (200+ edge locations)
   ├── Multi-region failover needed
   ├── Users should go to nearest region
   ├── Automatic failover to healthy region
   ├── DDoS protection required
   └── Edge caching needed

   Example: E-commerce global presence
   Asia Users → Route to Asia region (20ms latency)
   Europe Users → Route to Europe region (15ms latency)
   US Users → Route to US region (10ms latency)

   Cost: ~$0.60/million requests

Selection Decision Tree:
Start → Non-HTTP protocol?
        ├─ YES → Load Balancer
        └─ NO → Single region?
                ├─ YES → Need WAF?
                │       ├─ YES → App Gateway
                │       └─ NO → Load Balancer
                └─ NO → Front Door (multi-region)
                ",
                Difficulty = InterviewDifficulty.Intermediate,
                Category = "Load Balancing",
                Tags = new[] { "networking", "load-balancer", "comparison", "architecture" }
            },

            new InterviewQuestion
            {
                Question = "You have an on-premises data center and Azure. How do you choose between VPN Gateway and ExpressRoute?",
                Answer = @"
VPN Gateway vs ExpressRoute Decision Matrix:

                        VPN Gateway          ExpressRoute
Speed                   Slow (encryption)    Ultra-fast (dedicated)
Cost                    $20-50/month         $2000-5000+/month
Setup Time              Hours                2-4 weeks
Bandwidth               ~1.25 Gbps           1-100 Gbps
Latency                 50-200ms             < 1ms
Network Quality         Internet dependent   Dedicated, guaranteed
Throughput              Variable (shared)    Guaranteed (dedicated)
Setup Complexity        Easy (few hours)     Complex (weeks)
Use Over Internet       Yes                  No (private circuit)

Decision Guide:

Choose VPN Gateway if:
✓ Budget is primary concern
✓ Don't need guaranteed bandwidth
✓ Traffic is sporadic/bursty
✓ Latency < 100ms acceptable
✓ Small data transfers
✓ Dev/test environments

Example: Small startup connecting remote office
├── Cost: $30/month
├── Setup: 2 hours
├── Branch office → VPN → Azure
└── Acceptable for email, file sharing

---

Choose ExpressRoute if:
✓ Guaranteed bandwidth critical
✓ Consistent low latency required
✓ Large data transfers (100GB+)
✓ Compliance requires private connection (HIPAA, PCI)
✓ High-volume replication traffic
✓ Mission-critical systems

Example: Financial firm trading platform
├── Cost: $3000/month (10 Gbps)
├── Setup: 3 weeks
├── Data Center → ExpressRoute → Azure
├── Real-time trading (latency critical)
└── Cannot tolerate internet latency/jitter

---

Hybrid Approach (Recommended for Large Orgs):
Primary: ExpressRoute (high-volume, low-latency)
Backup: VPN Gateway (failover if ExpressRoute down)

Benefits:
✓ Normal operations use dedicated circuit
✓ If circuit fails, automatic failover to VPN
✓ Business continuity ensured
✓ Cost: ExpressRoute + VPN backup

Real Scenario:
Day 1: Circuit working, all traffic via ExpressRoute (1ms latency)
Day 2: Circuit failure detected
        ↓
        Automatic failover to VPN (150ms latency)
        ↓
        Issue escalated, engineer working on restoration
        ↓
Day 3: Circuit restored, failback to ExpressRoute
                ",
                Difficulty = InterviewDifficulty.Advanced,
                Category = "Hybrid Connectivity",
                Tags = new[] { "networking", "vpn", "expressroute", "hybrid", "connectivity" }
            },

            new InterviewQuestion
            {
                Question = "Your users are experiencing 500ms latency when accessing your global app. How do you diagnose and fix?",
                Answer = @"
Troubleshooting High Latency (500ms):

Step 1: Identify Geographic Distribution
├── Where are users? (US, EU, Asia, etc)
├── Where is data? (which region)
├── What's the expected latency?
└── Is it regional or global?

Step 2: Check Network Path
Using Azure Network Watcher → Connection Monitor:
├── Measure latency from user location to service
├── Expected: 20-100ms depending on distance
├── If measured: 500ms → likely wrong routing or hop

Step 3: Check Load Balancer/Gateway Health
├── Load Balancer: Check backend pool health
│   └── If unhealthy: Auto-routes to healthy (may add latency)
├── App Gateway: Check backend HTTP 200 response
│   └── If slow backend: Requests timeout
└── Front Door: Check regional health probes

Step 4: Check DNS Resolution
dig api.example.com
├── DNS should resolve < 5ms
├── If slow: Update DNS TTL or use Azure DNS

Step 5: Check Backend Performance
Using Application Insights:
├── Query response time
├── Is backend slow (yes/no)?
├── If backend slow: Investigate database, not network

Step 6: Verify Optimal Routing
Using Azure Pipelines or custom script:
├── Tracert to destination
├── Are packets taking optimal path?
├── Manual UDR routing incorrectly?

Diagnosis Example:
User in US West: 500ms latency to database

Check 1: Connection Monitor
├── User → Azure: 150ms (normal)
├── Azure → Database: 350ms (high!)

Check 2: Check if database healthy
├── Running queries: CPU 5%, healthy
├── Queries responding < 10ms

Check 3: Network path
├── Tracert shows: User → Load Balancer → Wrong Region!
├── Load balancer routing traffic to US East (3000 miles)
├── Instead of local US West database

Root Cause: Load balancer backend pool missing US West region

Fix: Add US West database to backend pool
Result: Latency drops to 30ms (local region) ✓

Common Causes of High Latency:
1. Wrong region (50% of cases)
   └── Route to nearest region, not fixed region
2. Unhealthy backend
   └── Requests timeout, fail over to distant backup
3. Slow database queries
   └── Application slow, not network
4. Network congestion
   └── Check Application Insights, Stream Analytics
5. DNS resolution slow
   └── Update DNS or use Azure DNS
                ",
                Difficulty = InterviewDifficulty.Advanced,
                Category = "Troubleshooting",
                Tags = new[] { "networking", "troubleshooting", "latency", "diagnostics" }
            },

            new InterviewQuestion
            {
                Question = "What are the limits and quotas of Azure Networking services?",
                Answer = @"
Key Networking Limits:

Virtual Networks:
├── VNets per region: 500
├── Subnets per VNet: 3000
├── Address space (CIDR): /8 to /29 (largest to smallest)
├── Cannot change CIDR after creation
└── Cannot delete VNet with resources

Network Interfaces:
├── NICs per VNet: 65,000
├── IP configs per NIC: 256
├── Public IPs per subscription: 20 (soft limit)
└── Private IPs per NIC: Limited by subnet size

Network Security Groups:
├── NSGs per subscription: 5000
├── Rules per NSG: 1000 total
├── Inbound rules: 500
├── Outbound rules: 500
└── Application Security Groups: Unlimited

Load Balancer:
├── Backend pools: 250
├── Targets per pool: 1000
├── Throughput: 4.6 Mbps
├── Concurrent connections: Millions
└── Rules per LB: 250

Application Gateway:
├── Listeners: 100
├── Routing rules: 100
├── Backend pools: 100
├── HTTP settings: 100
├── Throughput: 2.5 Gbps
└── Concurrent connections: 10000+

Azure Firewall:
├── Firewalls per region: 10
├── Rules: 1000+ (soft limit)
├── Throughput: 30 Gbps
└── Concurrent connections: 10000+

VPN Gateway:
├── Gateways per subscription: 10
├── Connections per gateway: 128
├── Throughput: 1.25 Gbps typical, up to 10 Gbps
├── IKE handshake timeout: 10 seconds
└── Point-to-site users: 128 (standard), 250 (high performance)

Scaling Implications:
- Small startup: 1-2 VNets, 1 Load Balancer → OK
- Mid-size: 5-10 VNets, 3-5 Load Balancers → OK
- Enterprise: 50+ VNets, 20+ Load Balancers → Approaching limits

If approaching limits:
├── Request quota increase (Support ticket)
├── Use hub-and-spoke design (reduces VNet count)
├── Consolidate Load Balancers (use Front Door)
└── Archive old rules/configs
                ",
                Difficulty = InterviewDifficulty.Intermediate,
                Category = "Limits & Quotas",
                Tags = new[] { "networking", "limits", "quotas", "scaling" }
            },

            new InterviewQuestion
            {
                Question = "Design a high-availability networking architecture for a critical business application",
                Answer = @"
High-Availability Network Architecture:

Goal: 99.99% availability (52 minutes downtime/year)

Layer 1: Global Load Balancing
├── Azure Front Door
├── Multiple regions (primary + backup)
├── Automatic multi-region failover
└── Health probes detect failures

Layer 2: Regional Load Balancing
├── Application Gateway per region
├── URL routing to microservices
├── WAF protection
└── SSL/TLS termination

Layer 3: Service Distribution
├── Load Balancer (internal) per tier
├── 3+ backend instances (never 2)
├── Health probes every 15 seconds
└── Auto-failover within seconds

Layer 4: Network Security
├── VNet + NSG (defense in depth)
├── Network Watcher monitoring
├── Azure DDoS Protection
└── Azure Firewall (optional)

Layer 5: Hybrid Connectivity
├── ExpressRoute (primary)
├── VPN Gateway (backup)
├── Automatic failover between circuits
└── On-premises redundancy

Example Architecture:
Region 1 (US East)
├── Frontend LB (3+ instances)
├── Application LB (3+ instances)
├── Database (primary, replication to Region 2)

    ↓ Front Door (global)

Region 2 (US West)
├── Frontend LB (3+ instances) - Standby
├── Application LB (3+ instances) - Standby
├── Database (secondary, read-only)

    ↓ On-Premises (ExpressRoute + VPN backup)

Data Center
├── Critical data replicated from cloud
└── Hybrid failover capability

Failure Scenarios & Recovery:

Scenario 1: Single VM fails
├── Health probe detects (< 30 seconds)
├── LB removes from rotation
├── Requests route to healthy VMs
├── No user impact ✓

Scenario 2: Entire Region fails
├── Front Door detects all backends unhealthy
├── Automatic failover to Region 2
├── Users redirected in < 2 minutes
├── SLA: 99.99% maintained ✓

Scenario 3: Internet connectivity lost
├── ExpressRoute continues (dedicated circuit)
├── No impact on hybrid cloud communication
├── VPN failover if ExpressRoute also fails
├── Redundancy: 2 circuit failures tolerated

Scenario 4: Database corruption in Region 1
├── Promote Region 2 database to primary
├── Update connection strings in applications
├── Point applications to Region 2
├── Data loss: 0 (real-time replication)
└── Recovery time: < 5 minutes

Cost Impact:
├── Front Door: $0.60/million requests
├── 2x Regions: 2x infrastructure cost
├── ExpressRoute + VPN: $2000-3000/month
├── Total: 2.5-3x single region cost
└── Value: 99.99% availability (vs 99.95% single region)

ROI Calculation:
Downtime at 99.95%: 22 hours/year
Downtime at 99.99%: 52 minutes/year
Difference: 21 hours saved/year

If $10k per hour downtime:
Benefit: $210,000/year
Cost: ~$3000/month × 12 = $36,000/year
Net benefit: $174,000/year
Payback: < 1 month ✓
                ",
                Difficulty = InterviewDifficulty.Advanced,
                Category = "High Availability",
                Tags = new[] { "networking", "architecture", "ha", "design", "redundancy" }
            },

            new InterviewQuestion
            {
                Question = "Explain how to implement network-level DDoS protection and rate limiting",
                Answer = @"
DDoS Protection Layers:

Layer 1: Azure DDoS Protection (Automatic)
├── Standard: Automatic, free
│   ├── Volumetric attacks (layer 3)
│   ├── Protocol attacks (layer 4)
│   ├── Application attacks (layer 7) - Limited
│   └── Mitigation: Automatic
├── Premium: $3000/month
│   ├── All above PLUS:
│   ├── Adaptive real-time tuning
│   ├── DDoS Protection alerts
│   ├── Attack analytics
│   ├── Support from DDoS engineers
│   └── Cost protection (refund during attack)

Layer 2: Azure Firewall
├── Stateful inspection
├── IP address/port filtering
├── Intrusion detection and prevention
└── Throughput: 30 Gbps

Layer 3: Application Gateway WAF
├── Layer 7 (application) protection
├── OWASP Top 10 rules
├── Rate limiting: Custom rules
├── Geo-blocking: Block by country
└── IP reputation filtering

Layer 4: NSG Rules
├── Explicit allow-list approach
├── Deny all others
└── Prevents unexpected traffic

Rate Limiting Implementation:

Option 1: NSG Rate Limiting
Problem: NSG doesn't support per-IP rate limiting
Solution: Use custom rules

Option 2: Application Gateway
WAF Rule:
├── Condition: Requests from same IP > 1000/minute
├── Action: Block for 5 minutes
├── Rate limit: 1000 req/min per IP
└── Cooldown: 5 minutes

Example Configuration:
If requests_from_ip > 1000 in 1 minute:
    Block_IP for 5 minutes
    Send 429 (Too Many Requests) response
    Log to Application Insights

Option 3: Azure Front Door (Recommended)
├── Built-in rate limiting
├── Distributed across 200+ edge locations
├── Rule: 100 requests/10 seconds per IP
├── Action: Return 429 response
└── Cost: Included in Front Door pricing

DDoS Attack Example:

Attacker: Bot network sending 10 Gbps traffic

Defense Strategy:
1. Azure DDoS Protection (automatic)
   ├── Detects 10 Gbps flood
   ├── Identifies: UDP port 53 (DNS amplification)
   └── Action: Drop packets, rate limit source

2. Application Gateway WAF
   ├── Rules: Block known DDoS patterns
   ├── Geo-blocking: If attack from unlikely country
   └── Challenge: Require CAPTCHA for suspicious traffic

3. Front Door
   ├── Throttle requests from attacker IP
   ├── Route legitimate traffic to cached responses
   └── Attackers waste bandwidth, real users unaffected

4. NSG
   ├── Manual: Add attacker IP range to deny rule
   └── Result: Traffic blocked at network edge

Result:
├── Legitimate users: See normal performance
├── Attacker: Requests return 429/blocked
├── Service: Continues normal operation
└── Uptime: 99.99%+ maintained

Cost of DDoS Protection:
No Premium DDoS Protection:
├── Attack: 10 Gbps flood for 1 hour
├── Bandwidth: 10 Gbps × 1 hour = $5,000 outbound
├── Total: Unexpected bill + potential downtime

With Premium DDoS Protection ($3000/month):
├── Attack: 10 Gbps flood for 1 hour
├── Bandwidth: Free (covered by DDoS protection)
├── Cost: $3000 flat (no extra charges)
└── Benefit: Peace of mind, expert support
                ",
                Difficulty = InterviewDifficulty.Advanced,
                Category = "Security",
                Tags = new[] { "networking", "ddos", "security", "waf", "rate-limiting" }
            },

            new InterviewQuestion
            {
                Question = "What's the difference between service endpoints and private endpoints?",
                Answer = @"
Service Endpoints vs Private Endpoints:

                        Service Endpoints    Private Endpoints
Network Path            Public internet      Private VNet only
CIDR Exposure           Wide (Azure service) Specific (resource)
Setup Complexity        Simple (1 click)     Complex (DNS, NIC)
Cost                    Free                 ~$7/endpoint/month
Latency                 10-50ms              < 1ms
Supported Services      Limited (~20)        Wide (100+)
DNS Management          Public DNS           Private DNS zone
Network Impact          Mild (not isolated)  Strong (isolated)
Compliance              Good                 Excellent (PCI, HIPAA)

Service Endpoints:
├── How it works:
│   ├── VNet routes traffic to Azure service directly
│   ├── Bypasses public internet
│   └── But traffic visible to Azure infrastructure
├── Use case:
│   ├── Cost-sensitive (free)
│   ├── Good enough isolation
│   └── Non-sensitive data
├── Example:
│   Storage account → Service Endpoint
│   ├── App in VNet can access blob storage
│   ├── Traffic doesn't traverse internet
│   └── But still Microsoft's backbone

Private Endpoints:
├── How it works:
│   ├── Service appears inside VNet (IP in subnet)
│   ├── Connection via private link
│   ├── Completely private (no public exposure)
│   └── Hostname resolves to private IP
├── Use case:
│   ├── Sensitive data (PII, financial)
│   ├── Compliance required (HIPAA, PCI-DSS)
│   ├── Data must never touch public internet
│   └── Multi-tenant isolation critical
├── Example:
│   SQL Database → Private Endpoint
│   ├── Database appears as 10.0.3.5 in VNet
│   ├── App connects to 10.0.3.5:1433
│   ├── Zero public IP exposure
│   └── Firewall: Only allow VNet traffic

Implementation Comparison:

Service Endpoint (Simple):
1. Go to Storage Account → Firewalls
2. Click 'Add virtual network'
3. Select VNet and subnet
4. Done! (2 minutes)

Private Endpoint (Complex):
1. Create Private Endpoint resource
2. Select target resource (SQL, Storage, etc)
3. Create Network Interface (NIC) in subnet
4. Create Private DNS Zone
5. Add DNS record (CNAME)
6. Test connectivity (may need to troubleshoot)
7. Done! (15-20 minutes + troubleshooting)

Cost Comparison for 10 Resources:

Service Endpoints:
├── 10 resources × $0 = $0/month
└── Total: Free

Private Endpoints:
├── 10 resources × $7 = $70/month
└── Total: $70/month

Real-World Scenario:

Patient Health Data Application:
├── Requirement: HIPAA compliant
├── Data sensitivity: Critical (PII + medical)
├── Compliance: Cannot use public endpoints

Solution: Private Endpoints
├── SQL Database: Private endpoint only
│   ├── Public endpoint: DISABLED
│   └── Connection only via private link
├── Storage Account: Private endpoint only
│   ├── Public endpoint: DISABLED
│   └── Blobs accessed via private IP (10.0.3.5)
├── App Service: In VNet (App Service Environment)
│   └── Connects to private endpoints only
└── Result: Compliant, secure, HIPAA eligible

Hybrid Scenario (Recommended for many):

Public APIs:
├── Use Service Endpoints
├── Cost: Free
└── Latency: Acceptable

Sensitive Data:
├── Use Private Endpoints
├── Cost: ~$7/endpoint
└── Latency: Excellent, isolation: Perfect

Cost: Mix of free + paid
Security: High (sensitive data protected)
Compliance: Eligible for HIPAA, PCI-DSS
                ",
                Difficulty = InterviewDifficulty.Intermediate,
                Category = "Connectivity",
                Tags = new[] { "networking", "endpoints", "security", "compliance" }
            }
        };

        public static List<InterviewQuestion> GetAdvancedQuestions() => new()
        {
            new InterviewQuestion
            {
                Question = "Design a multi-region network architecture for disaster recovery with sub-second failover",
                Answer = @"
Multi-Region DR Architecture (Sub-Second Failover):

Requirement: RTO < 1 minute, RPO < 1 second

Primary Region (US East) - Active
├── Virtual Network: 10.0.0.0/16
├── Application Tier: 5 instances (load balanced)
├── Database: SQL with continuous replication
└── Traffic Manager: Routes 100% to primary

Secondary Region (US West) - Standby
├── Virtual Network: 10.1.0.0/16
├── Application Tier: 5 instances (warm standby)
├── Database: Continuous geo-replication
└── Traffic Manager: Routes 0% (until failover)

Connectivity:
├── Global Load Balancer: Front Door or Traffic Manager
├── ExpressRoute: Primary circuit
├── VPN Gateway: Backup circuit
└── Inter-region peering: VNet-to-VNet

Failover Mechanism:

Normal Operation (Primary Active):
User request → Front Door → US East region → App → DB
Latency: ~10ms, Success: 100%

Primary Region Fails:
├── Front Door health probe fails (after 2-3 probes)
├── Automatic failover triggered (< 2 minutes)
└── Traffic routes to US West

After Failover:
User request → Front Door → US West region → App → DB
Latency: ~40ms (farther), Success: Restored

Data Consistency:
├── Database replication: Continuous (< 1 second lag)
├── On failover: Secondary database becomes primary
├── Data loss: 0-1 second (RPO met)
└── Consistency: Strong (ACID guaranteed)

Cost:
├── Primary region: $10,000/month
├── Secondary region: $10,000/month (warm standby)
├── Front Door: ~$0.60/million requests
├── ExpressRoute + VPN: $3000/month
├── Total: ~$23,000/month

Alternative (Lower Cost): Cold Standby
├── Secondary region: Minimal resources
├── Scale up on failover (10-15 minute RTO)
├── Cost: 50% reduction
└── Tradeoff: Longer recovery time
                ",
                Difficulty = InterviewDifficulty.Advanced,
                Category = "Disaster Recovery",
                Tags = new[] { "networking", "dr", "failover", "architecture" }
            }
        };
    }

    public class InterviewQuestion
    {
        public string Question { get; set; }
        public string Answer { get; set; }
        public InterviewDifficulty Difficulty { get; set; }
        public string Category { get; set; }
        public string[] Tags { get; set; }
    }

    public enum InterviewDifficulty
    {
        Beginner,
        Intermediate,
        Advanced
    }
}
