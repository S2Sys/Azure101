using System;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Network.Models;

namespace Azure101.Networking.Examples
{
    /// <summary>
    /// Demonstrates VNet, NSG, Load Balancer, and App Gateway patterns
    /// </summary>
    public class NetworkingPatternsExample
    {
        // ============================================================================
        // Pattern 1: Virtual Network (VNet) with Subnets and NSGs
        // ============================================================================
        public async Task Pattern1_CreateSecureVNetAsync()
        {
            var credential = new DefaultAzureCredential();
            var subscriptionId = "your-subscription-id";
            var resourceGroupName = "rg-networking";
            var location = "eastus";

            var armClient = new ArmClient(credential, subscriptionId);
            var resourceGroupResource = armClient.GetResourceGroupResource(
                ResourceGroupResource.CreateResourceIdentifier(subscriptionId, resourceGroupName));

            // Create VNet
            var vnetData = new VirtualNetworkData
            {
                Location = location,
                AddressPrefixes = { "10.0.0.0/16" },
                Subnets =
                {
                    new SubnetData { Name = "frontend", AddressPrefix = "10.0.1.0/24" },
                    new SubnetData { Name = "backend", AddressPrefix = "10.0.2.0/24" },
                    new SubnetData { Name = "database", AddressPrefix = "10.0.3.0/24" }
                }
            };

            var vnet = await resourceGroupResource.GetVirtualNetworks()
                .CreateOrUpdateAsync(WaitUntil.Completed, "vnet-prod", vnetData);

            Console.WriteLine($"Created VNet: {vnet.Value.Id}");

            // Create NSG for Frontend (allow HTTP/HTTPS)
            var frontendNsgData = new NetworkSecurityGroupData { Location = location };
            frontendNsgData.SecurityRules.Add(new SecurityRuleData
            {
                Name = "allow-http",
                Protocol = SecurityRuleProtocol.Tcp,
                SourcePortRange = "*",
                DestinationPortRange = "80",
                SourceAddressPrefix = "Internet",
                DestinationAddressPrefix = "*",
                Access = SecurityRuleAccess.Allow,
                Priority = 100,
                Direction = SecurityRuleDirection.Inbound
            });

            var frontendNsg = await resourceGroupResource.GetNetworkSecurityGroups()
                .CreateOrUpdateAsync(WaitUntil.Completed, "nsg-frontend", frontendNsgData);

            Console.WriteLine($"Created NSG: {frontendNsg.Value.Id}");
        }

        // ============================================================================
        // Pattern 2: Network Security Group (NSG) Rules for Database Protection
        // ============================================================================
        public async Task Pattern2_SecureDatabaseAccessAsync()
        {
            var credential = new DefaultAzureCredential();
            var subscriptionId = "your-subscription-id";
            var resourceGroupName = "rg-networking";
            var location = "eastus";

            var armClient = new ArmClient(credential, subscriptionId);
            var resourceGroupResource = armClient.GetResourceGroupResource(
                ResourceGroupResource.CreateResourceIdentifier(subscriptionId, resourceGroupName));

            // Create NSG for Database tier
            var dbNsgData = new NetworkSecurityGroupData { Location = location };

            // Allow SQL traffic only from application subnet
            dbNsgData.SecurityRules.Add(new SecurityRuleData
            {
                Name = "allow-sql-from-app",
                Protocol = SecurityRuleProtocol.Tcp,
                SourcePortRange = "*",
                DestinationPortRange = "1433",
                SourceAddressPrefix = "10.0.2.0/24", // Application subnet
                DestinationAddressPrefix = "*",
                Access = SecurityRuleAccess.Allow,
                Priority = 100,
                Direction = SecurityRuleDirection.Inbound
            });

            // Allow RDP only from bastion
            dbNsgData.SecurityRules.Add(new SecurityRuleData
            {
                Name = "allow-rdp-from-bastion",
                Protocol = SecurityRuleProtocol.Tcp,
                SourcePortRange = "*",
                DestinationPortRange = "3389",
                SourceAddressPrefix = "10.0.4.0/24", // Bastion subnet
                DestinationAddressPrefix = "*",
                Access = SecurityRuleAccess.Allow,
                Priority = 110,
                Direction = SecurityRuleDirection.Inbound
            });

            // Deny all other inbound
            dbNsgData.SecurityRules.Add(new SecurityRuleData
            {
                Name = "deny-all-inbound",
                Protocol = SecurityRuleProtocol.Asterisk,
                SourcePortRange = "*",
                DestinationPortRange = "*",
                SourceAddressPrefix = "*",
                DestinationAddressPrefix = "*",
                Access = SecurityRuleAccess.Deny,
                Priority = 1000,
                Direction = SecurityRuleDirection.Inbound
            });

            var dbNsg = await resourceGroupResource.GetNetworkSecurityGroups()
                .CreateOrUpdateAsync(WaitUntil.Completed, "nsg-database", dbNsgData);

            Console.WriteLine($"✓ Created secure database NSG with 3 rules");
        }

        // ============================================================================
        // Pattern 3: Load Balancer for Internal Service Distribution
        // ============================================================================
        public async Task Pattern3_InternalLoadBalancerAsync()
        {
            // Internal Load Balancer for backend service distribution
            // Layer 4 (TCP/UDP) load balancing for high performance
            // No public IP (internal only)

            Console.WriteLine(@"
Load Balancer Configuration:
├── Type: Internal (no public IP)
├── Layer: 4 (TCP/UDP)
├── Backend Pool: 3x application VMs
├── Health Probe: TCP port 8080, every 15 seconds
├── Load Balancing Rule:
│   ├── Frontend: 10.0.4.1:8080
│   ├── Backend: Port 8080
│   ├── Protocol: TCP
│   ├── Session Persistence: Disabled
│   └── Timeout: 4 minutes
├── Throughput: 4.6 Mbps
└── Cost: ~$40/month

Use Case: Distribute traffic across multiple backend services
with ultra-low latency (< 1ms) and high reliability (99.99% SLA)
            ");
        }

        // ============================================================================
        // Pattern 4: Application Gateway for URL Routing
        // ============================================================================
        public async Task Pattern4_ApplicationGatewayRoutingAsync()
        {
            // Application Gateway for Layer 7 routing
            Console.WriteLine(@"
Application Gateway Configuration:
├── Type: Layer 7 (Application layer)
├── Frontend: HTTPS (port 443)
├── Routing Rules:
│   ├── /api/* → API Backend Pool
│   ├── /orders/* → Order Service Backend Pool
│   ├── /inventory/* → Inventory Service Backend Pool
│   ├── /payments/* → Payment Service Backend Pool
│   └── /* → Default Backend Pool
├── WAF: Enabled (OWASP Top 10 protection)
├── SSL/TLS: Certificate termination
├── Health Probes: HTTP /health endpoint, 30 second interval
├── Throughput: 2.5 Gbps
└── Cost: ~$250-400/month

Benefits:
✓ URL path-based routing
✓ Host header routing (multi-tenant)
✓ SSL/TLS termination
✓ Web Application Firewall (OWASP rules)
✓ Cookie-based affinity for sticky sessions
✓ Request rewriting (modify headers)

Real-World Pattern:
Frontend clients send HTTPS requests to gateway
    ↓
App Gateway terminates SSL/TLS
    ↓
Analyzes request (URL, headers, cookies)
    ↓
Routes to appropriate backend pool
    ↓
Health probe checks backend health
    ↓
If backend unhealthy, routes to healthy instance
            ");
        }

        // ============================================================================
        // Pattern 5: VNet Peering for Multi-VNet Communication
        // ============================================================================
        public async Task Pattern5_VNetPeeringAsync()
        {
            Console.WriteLine(@"
VNet Peering Pattern (Hub-and-Spoke):

Hub VNet (10.0.0.0/16)
├── Firewall
├── API Gateway
└── Shared services

Spoke 1 VNet (10.1.0.0/16) - Production Apps
├── Peered to Hub
├── Can reach Hub services
└── Isolated from Spoke 2

Spoke 2 VNet (10.2.0.0/16) - Development
├── Peered to Hub
├── Can reach Hub services
└── Isolated from Spoke 1

Benefits:
✓ Low-latency connectivity (private network)
✓ Bandwidth optimization (no internet hops)
✓ Security isolation (each spoke independent)
✓ Simplified management (hub has shared services)
✓ Cost-effective (peering is free)

Communication Flow:
App in Spoke 1 → Peering → Hub Firewall → Routes to destination
                                         ↓
                                 Spoke 2 (if allowed)

Network Requirements:
- No overlapping CIDR ranges
- Peering must be approved both ways
- Transitive peering NOT automatic (requires UDR)
            ");
        }

        // ============================================================================
        // Pattern 6: VPN Gateway for Hybrid Connectivity
        // ============================================================================
        public async Task Pattern6_VPNGatewayAsync()
        {
            Console.WriteLine(@"
VPN Gateway Pattern (Hybrid Cloud):

On-Premises Network
├── VPN Gateway (IPSec termination)
└── Static routes to Azure prefixes

    ↓ Encrypted VPN Tunnel (IPSec)

Azure VNet
├── VPN Gateway (accepts IPSec connections)
├── Tunnel Configuration:
│   ├── Pre-shared key: abc123xyz
│   ├── IKE version: IKEv2
│   ├── IPSec encryption: AES256
│   └── Tunnel timeout: 30 seconds
└── Routes traffic based on routing table

Benefits:
✓ Secure encrypted connection
✓ Works over public internet
✓ Cost-effective (vs ExpressRoute)
✓ Quick to setup (hours vs weeks)
✓ Flexible (both site-to-site and point-to-site)

Limitations:
✗ Slower than ExpressRoute (encryption overhead)
✗ Dependent on internet connection quality
✗ Higher latency (50-200ms typical)
✗ Throughput: ~1.25 Gbps per tunnel

Use Case: Branch offices connecting to Azure
├── Branch Office 1 → VPN → Azure
├── Branch Office 2 → VPN → Azure
└── Branch Office 3 → VPN → Azure

All can access shared Azure services (databases, APIs)
            ");
        }

        // ============================================================================
        // Pattern 7: ExpressRoute for Dedicated Connectivity
        // ============================================================================
        public async Task Pattern7_ExpressRouteAsync()
        {
            Console.WriteLine(@"
ExpressRoute Pattern (Dedicated Connection):

Data Center (Financial Firm)
    ├── Requires guaranteed bandwidth
    ├── Cannot tolerate internet latency
    └── Compliance requirement (private connection)

    ↓ Dedicated Private Circuit (1-100 Gbps)
    (Carrier provides dedicated line)

Azure Cloud
    ├── ExpressRoute Gateway
    ├── BGP routing (dynamic)
    └── Private peering

Connection Details:
├── Bandwidth: 10 Gbps (dedicated)
├── Latency: < 1ms (consistent)
├── Reliability: 99.95% SLA
├── Setup time: 2-4 weeks
├── Cost: $2000-5000+/month

Why ExpressRoute:
✓ Guaranteed bandwidth (vs shared internet)
✓ Ultra-low latency (< 1ms consistent)
✓ Private connection (no internet exposure)
✓ Multiple redundant paths
✓ Better for large data transfers
✓ Supports hybrid cloud at scale

Real-World Use Case:
├── Financial trading: Real-time, low-latency critical
├── Healthcare: HIPAA compliance requires private connection
├── Large enterprises: 100GB+ data transfers daily
└── Disaster recovery: Multi-region failover

Comparison: VPN vs ExpressRoute
VPN: $20-50/month, high latency, shared internet
ExpressRoute: $2000+/month, low latency, dedicated
            ");
        }

        // ============================================================================
        // Pattern 8: Azure Front Door for Global Load Balancing
        // ============================================================================
        public async Task Pattern8_FrontDoorGlobalAsync()
        {
            Console.WriteLine(@"
Azure Front Door (Global Load Balancer):

Scenario: Global e-commerce platform

Users in Asia-Pacific
    ├── Route to nearest region (Asia)
    ├── Latency: ~20-50ms
    └── Served from Asia edge location

    ↓ 200+ Global Edge Locations

Users in Europe
    ├── Route to nearest region (Europe)
    ├── Latency: ~10-30ms
    └── Served from Europe edge location

    ↓

Users in North America
    ├── Route to nearest region (US East)
    ├── Latency: ~5-20ms
    └── Served from US edge location

Front Door Features:
├── Global routing (Anycast)
├── Multi-region failover (automatic)
├── Edge caching (200+ locations)
├── WAF (OWASP protection)
├── DDoS protection
├── URL rewriting
└── Path-based routing

Health Probes:
├── HTTP HEAD every 30 seconds
├── Marks region as 'Down' after 3 failures
└── Auto-routes traffic to healthy region

Real-World Example:
Primary Region (US East): Processing 90% of traffic
Secondary Region (Europe): Failover if primary down

If primary region fails:
✓ Front Door detects failure (< 2 minutes)
✓ Automatically routes 100% traffic to secondary
✓ Users in Asia still experience high latency
✓ But system remains online (availability > performance)

Cost: ~$0.60/million requests + data transfer
            ");
        }

        // ============================================================================
        // Pattern 9: Network Monitoring and Diagnostics
        // ============================================================================
        public async Task Pattern9_NetworkMonitoringAsync()
        {
            Console.WriteLine(@"
Network Monitoring Pattern:

What to Monitor:
├── Network Performance Monitor (NPM)
│   ├── Latency between subnets
│   ├── Packet loss percentage
│   ├── Jitter (latency variation)
│   └── Bandwidth utilization
├── NSG Flow Logs
│   ├── All traffic through NSGs
│   ├── Allow/Deny rules matched
│   ├── Timestamp and 5-tuple (SrcIP, DstIP, Port, Protocol)
│   └── Send to Log Analytics for analysis
├── Connection Monitor
│   ├── Latency from A to B
│   ├── Packet loss percentage
│   └── Test from multiple locations
└── Packet Capture
    ├── Raw packet analysis
    ├── For detailed troubleshooting
    └── Limited duration (5 minutes)

Troubleshooting Example:
Problem: Users in US West report slow access to database

Investigation Steps:
1. Check NSG Flow Logs
   └── Are packets reaching database subnet?
   └── If Deny rule matched: NSG is blocking
   └── If no logs: Network path issue

2. Check Network Performance Monitor
   └── Latency: 500ms (should be < 50ms)
   └── Packet loss: 5% (should be 0%)
   └── Indicates network congestion or routing issue

3. Check Route Tables
   └── Is traffic taking optimal path?
   └── Manual UDR (User Defined Route) might be redirecting

4. Check Application Gateway/Load Balancer
   └── Health probes failing?
   └── Backend pool unhealthy?

Root Cause: Routing table incorrectly routes US West traffic
through expensive gateway instead of direct peering

Fix: Update routing table to prioritize direct peering
Result: Latency drops from 500ms to 30ms ✓
            ");
        }
    }
}
