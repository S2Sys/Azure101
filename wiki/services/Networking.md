# Azure Networking Services - Complete Wiki

## Overview

Azure Networking provides connectivity, security, and routing for your cloud infrastructure. Services range from virtual networks (basic connectivity) to advanced traffic management (load balancing, CDN, traffic management).

---

## Quick Comparison Table

| Service | Purpose | Layer | Throughput | Latency | Cost |
|---------|---------|-------|-----------|---------|------|
| **VNet** | Basic connectivity | L3/L2 | Unlimited | Microseconds | Free |
| **NSG** | Network security | L4 | Unlimited | Minimal | Free |
| **Load Balancer** | Layer 4 distribution | L4 | 4.6 Mbps | Very Low | Medium |
| **App Gateway** | Layer 7 routing | L7 | 2.5 Gbps | Low | Medium-High |
| **Front Door** | Global load balancing | L7 | Unlimited | Low | High |
| **VPN Gateway** | Hybrid connectivity | L3 | Up to 10Gbps | Medium | Medium |
| **ExpressRoute** | Private connectivity | L3 | Up to 100Gbps | Very Low | Very High |
| **CDN** | Content distribution | L7 | 60+Gbps | Low | Variable |
| **Firewall** | Centralized security | L4-L7 | 30Gbps | Low | High |

---

## 1. Virtual Networks (VNet)

### What is it?
A logically isolated network in Azure where you can launch resources with custom IP addressing and subnetting.

### When to Use
- All Azure deployments need a VNet
- Multi-tier applications requiring subnet separation
- Hybrid on-premises connectivity
- Custom DNS and routing

### Key Features
- **Address Space**: Define IP ranges (e.g., 10.0.0.0/16)
- **Subnets**: Divide network into smaller segments
- **Network Interfaces**: Attach to VMs and services
- **Route Tables**: Custom routing logic
- **Service Endpoints**: Direct access to Azure services
- **Private Endpoints**: Private connectivity to services

### Architecture & Core Concepts

#### VNet Design Pattern
```
VNet (10.0.0.0/16)
├── Frontend Subnet (10.0.1.0/24)
│   └── Public IPs + NSG (allow port 443)
├── Application Subnet (10.0.2.0/24)
│   └── Internal IPs + NSG (allow from frontend)
├── Database Subnet (10.0.3.0/24)
│   └── Private IPs + NSG (allow from app subnet)
└── Management Subnet (10.0.4.0/24)
    └── Bastion/Jumpbox access
```

#### Connectivity Options
- **VNet-to-VNet**: Peering (same region), Gateway (cross-region)
- **VNet-to-OnPrem**: VPN Gateway, ExpressRoute
- **VNet-to-Internet**: Public IP + NAT Gateway
- **Service Access**: Service Endpoints (public), Private Endpoints (private)

### Pros & Cons

**Pros** ✅
- Fundamental for all Azure deployments
- Complete network control
- Flexible IP addressing and subneting
- Integration with on-premises networks
- No additional cost (only traffic egress)
- Security group controls per subnet

**Cons** ❌
- Requires proper planning (CIDR design)
- Can become complex at scale
- No automatic disaster recovery
- Manual management of routes
- Difficult to extend CIDR post-deployment

### Limits & Constraints

| Resource | Limit |
|----------|-------|
| VNets per region | 500 (can increase) |
| Subnets per VNet | 3000 |
| VMs per subnet | No limit (IP addresses) |
| Network Interfaces per VNet | 65000 |
| Route tables per subscription | 200 |
| Routes per route table | 400 |
| VNet peerings per VNet | 500 |
| VPN connections per gateway | 128 |

### Real-World Use Cases

#### Use Case 1: Multi-Region HA Application
```
Region 1 VNet (10.0.0.0/16)
    ├── Frontend (10.0.1.0/24)
    ├── App (10.0.2.0/24)
    └── Database (10.0.3.0/24)
        ↓ VNet Peering (or Global Load Balancer)
Region 2 VNet (10.1.0.0/16)
    ├── Frontend (10.1.1.0/24)
    ├── App (10.1.2.0/24)
    └── Database (10.1.3.0/24)
    
Failover: Route traffic to healthy region
```

#### Use Case 2: Hybrid Cloud Architecture
```
On-Premises Network (192.168.0.0/16)
    ↓ VPN Gateway / ExpressRoute
Azure VNet (10.0.0.0/16)
    ├── Services talking to on-prem via site-to-site VPN
    └── Seamless hybrid workload distribution
```

#### Use Case 3: Microservices Isolation
```
VNet (10.0.0.0/16)
├── Frontend Apps (10.0.1.0/24) → NSG: allow 443 from internet
├── Order Service (10.0.2.0/24) → NSG: allow from frontend, database
├── Payment Service (10.0.3.0/24) → NSG: allow from order service
├── Database (10.0.4.0/24) → NSG: allow only from services
└── Management (10.0.5.0/24) → NSG: allow SSH/RDP from bastion
```

### Performance Tips
- Use multiple subnets to distribute IPs and reduce congestion
- Leverage service endpoints for Azure services (no internet hop)
- Use private endpoints for sensitive data services
- Implement proper routing to avoid unnecessary hops
- Monitor VNet flow logs for traffic analysis

### Cost Optimization
- VNets are free
- Egress traffic is charged (~$0.02/GB typically)
- Data transfers within same VNet are free
- Use service endpoints instead of public endpoints
- NAT Gateway is cheaper than multiple public IPs

---

## 2. Network Security Group (NSG)

### What is it?
Virtual firewall that filters traffic at the network interface level.

### When to Use
- Control inbound/outbound traffic
- Isolate subnets and services
- Implement security boundaries
- Replace hardware firewalls for cloud workloads

### Key Features
- **Security Rules**: Stateful filtering
- **Inbound/Outbound**: Separate rulesets
- **Priority**: 0-4096 (lower = higher priority)
- **Application Groups**: Group resources for easier management
- **Logging**: NSG flow logs for traffic analysis

### Pros & Cons

**Pros** ✅
- Granular traffic control
- Stateful (return traffic auto-allowed)
- Easy to modify and test
- Application Security Groups for flexibility
- Flow logs for troubleshooting

**Cons** ❌
- No deep packet inspection
- Cannot filter by port numbers above 65535
- Rules can become complex at scale
- No URL filtering or DPI
- Management overhead for many rules

### Limits & Constraints

| Resource | Limit |
|----------|-------|
| NSGs per subscription | 5000 |
| Rules per NSG | 1000 |
| Security rules per NSG | 500 (inbound) + 500 (outbound) |
| Application Security Groups per NSG | Unlimited |
| NSGs per subnet | 1 |
| NSGs per NIC | 1 |

### Real-World Use Cases

#### Use Case 1: Web Server Security
```
NSG Rules:
✓ Inbound port 80 from Internet
✓ Inbound port 443 from Internet
✓ Inbound port 3389 from Management subnet (RDP)
✓ Outbound port 443 to Database subnet
✓ Outbound port 443 to Internet (updates, APIs)
✗ Deny all other
```

#### Use Case 2: Database Server Protection
```
NSG Rules:
✓ Inbound port 1433 (SQL) from Application subnet only
✓ Inbound port 3389 from Bastion/Management only
✗ All internet access blocked
✗ Outbound to internet blocked (except for patches via service endpoint)
```

### Cost Optimization
- NSGs are free
- Only bandwidth charges apply
- Use service endpoints instead of firewalling

---

## 3. Azure Load Balancer

### What is it?
Layer 4 (transport) load balancer distributing traffic across backend pools. Works with TCP and UDP at low latency.

### When to Use
- Distribute traffic across VMs
- Non-HTTP protocols (UDP, SQL, gaming)
- Ultra-low latency requirements
- Internal load balancing between tiers

### Key Features
- **Public/Internal**: External or internal traffic routing
- **Backend Pool**: Group of VMs receiving traffic
- **Health Probes**: Monitor backend health
- **Load Balancing Rules**: Port mapping
- **Session Persistence**: Sticky sessions

### Pros & Cons

**Pros** ✅
- Ultra-low latency (< 1ms)
- Supports all protocols (TCP, UDP)
- Simple, reliable, proven technology
- Excellent for non-HTTP workloads
- Both public and internal variants

**Cons** ❌
- Only Layer 4 (no URL routing)
- No application-aware logic
- Limited to port-based rules
- Need App Gateway for HTTP routing
- More expensive than basic routing

### Limits & Constraints

| Resource | Limit |
|----------|-------|
| Public Load Balancers per subscription | 1000 |
| Backend pools per LB | 250 |
| VMs per backend pool | 1000 |
| Frontend IP configs | 250 |
| Rules per LB | 250 |
| Throughput | 4.6 Mbps (always-on) |

### Real-World Use Cases

#### Use Case 1: Database Traffic Distribution
```
Client Apps
    ↓ Load Balancer (port 5432)
    ├─ PostgreSQL Replica 1
    ├─ PostgreSQL Replica 2
    └─ PostgreSQL Replica 3
```

#### Use Case 2: Gaming Server Load Distribution
```
Players → Load Balancer (UDP port 7777)
    ├─ Game Server 1
    ├─ Game Server 2
    └─ Game Server 3
(Health check: custom UDP probe)
```

---

## 4. Application Gateway

### What is it?
Layer 7 (application) load balancer with advanced routing, SSL termination, and WAF capabilities.

### When to Use
- HTTP/HTTPS web applications
- URL path-based routing
- Host header routing
- Web Application Firewall (WAF)
- SSL/TLS termination

### Key Features
- **URL Routing**: Route /api/* to different backend
- **Host Routing**: api.example.com vs app.example.com
- **Multi-Site Hosting**: Multiple sites on one gateway
- **WAF**: Built-in Web Application Firewall
- **SSL/TLS**: Certificate management

### Pros & Cons

**Pros** ✅
- Application-aware routing
- URL path and hostname routing
- Built-in WAF for attacks
- SSL termination
- Cookie-based affinity

**Cons** ❌
- Higher latency than Load Balancer
- More expensive than Load Balancer
- Complex configuration
- Slower startup time
- Not suitable for non-HTTP protocols

### Limits & Constraints

| Resource | Limit |
|----------|-------|
| App Gateways per subscription | 1000 |
| Listeners per gateway | 100 |
| Routing rules per gateway | 100 |
| Backend pools per gateway | 100 |
| Backend targets per pool | 1200 |
| Throughput | 2.5 Gbps |
| Concurrent connections | Varies by size |

### Real-World Use Cases

#### Use Case 1: Microservices Routing
```
Client
    ↓ Application Gateway
    ├── /orders/* → Order Service (backend pool)
    ├── /payments/* → Payment Service (backend pool)
    ├── /inventory/* → Inventory Service (backend pool)
    └── /api/* → API Service (backend pool)
```

#### Use Case 2: Multi-Tenant SaaS
```
Client
    ↓ Application Gateway (host-based routing)
    ├── tenant1.saas.com → Tenant 1 Backend
    ├── tenant2.saas.com → Tenant 2 Backend
    └── admin.saas.com → Admin Panel Backend
```

---

## 5. Azure Front Door

### What is it?
Global load balancer with automatic failover, caching, and WAF. Operates at the edge globally.

### When to Use
- Global audience across regions
- Multi-region failover
- DDoS protection
- Edge caching
- Web Application Firewall

### Key Features
- **Global Routing**: Direct users to nearest region
- **Multi-Region Failover**: Automatic failover to healthy region
- **Edge Caching**: Cache at 200+ edge locations
- **WAF**: Built-in Web Application Firewall
- **URL Rewriting**: Modify URLs before routing

### Pros & Cons

**Pros** ✅
- True global load balancing
- Automatic multi-region failover
- Edge caching (200+ locations)
- Integrated DDoS protection
- Ultra-high availability

**Cons** ❌
- Higher cost than regional LB
- Not suitable for internal-only services
- More complex configuration
- Higher latency added to some regions
- Overkill for single-region apps

### Limits & Constraints

| Resource | Limit |
|----------|-------|
| Front Door instances per subscription | 100 |
| Frontend hosts per instance | 500 |
| Routing rules per instance | 500 |
| Backend pools per instance | 50 |
| Cache TTL | Up to 366 days |

### Real-World Use Cases

#### Use Case 1: Global E-Commerce
```
Users Worldwide
    ↓ Azure Front Door (edge in multiple regions)
    ├── EU users → European Region
    ├── US users → US Region
    ├── APAC users → Asia-Pacific Region
    └── Auto-failover if region down
```

---

## 6. VPN Gateway

### What is it?
Creates encrypted site-to-site or point-to-site VPN connections between on-premises networks and Azure VNets.

### When to Use
- Hybrid connectivity (on-prem to Azure)
- Secure branch office connectivity
- Developer VPN access
- Cost-effective compared to ExpressRoute

### Key Features
- **Site-to-Site**: On-premises gateway to VPN gateway
- **Point-to-Site**: Individual computers to Azure
- **Forced Tunneling**: Route all internet traffic through on-prem
- **High Availability**: Active-active configurations

### Pros & Cons

**Pros** ✅
- Cost-effective hybrid connectivity
- Works over public internet
- Flexible (site-to-site and point-to-site)
- Quick to set up
- No long-term contracts

**Cons** ❌
- Network overhead (encryption/decryption)
- Dependent on internet quality
- Slower than ExpressRoute
- Throughput limited to ~1.25 Gbps typical
- Can have jitter and latency issues

### Limits & Constraints

| Resource | Limit |
|----------|-------|
| VPN Gateways per subscription | 10 |
| Connections per gateway | 128 |
| Throughput | Up to 10 Gbps (depends on SKU) |
| IKE/IPSec handshake timeout | 10 seconds |

### Real-World Use Cases

#### Use Case 1: Hybrid Development Environment
```
Developer Laptop
    ↓ Point-to-Site VPN
Azure VNet
    ↓ Can access:
    - Development SQL databases
    - Development VMs
    - Development AppServices
    (Secure, encrypted tunnel)
```

#### Use Case 2: Multi-Location Enterprise
```
HQ On-Premises Network
    ↓ Site-to-Site VPN
Azure VNet
    ↓ VNet Peering
Regional Azure VNets
    ↓ Connected via:
    - Branch Office 1 (VPN)
    - Branch Office 2 (VPN)
    - Branch Office 3 (VPN)
```

---

## 7. ExpressRoute

### What is it?
Private, dedicated network connection to Azure without traversing the public internet. Provides 1-100 Gbps bandwidth.

### When to Use
- Mission-critical hybrid workloads
- Large data transfers
- Consistent low-latency requirements
- Regulatory/compliance requirements

### Key Features
- **Dedicated Bandwidth**: 1Mbps to 100 Gbps
- **Private Connection**: Never touches public internet
- **Dynamic Routing**: BGP routing
- **Global Reach**: Connect to Azure globally
- **SLA**: 99.95% availability guaranteed

### Pros & Cons

**Pros** ✅
- Private, guaranteed bandwidth
- Consistent low latency
- High throughput (up to 100Gbps)
- Better security (no internet exposure)
- Suitable for large file transfers

**Cons** ❌
- Significantly higher cost (thousands/month)
- Long lead time (weeks to months)
- Requires networking partner
- Overkill for small deployments
- No quick failover to internet

### Limits & Constraints

| Resource | Limit |
|----------|-------|
| ExpressRoute circuits per subscription | 10 |
| Virtual Networks per circuit | 10 (standard), 20 (premium) |
| BGP communities | 200 |
| BGP route limit | 200 (standard), 10000 (premium) |

### Real-World Use Cases

#### Use Case 1: Financial Institution
```
Data Center (Financial Market Data)
    ↓ ExpressRoute (10 Gbps, dedicated)
Azure Cloud
    ↓ Services:
    - Real-time analysis
    - Low-latency trading
    - 99.95% SLA guaranteed
    (Cannot tolerate internet latency/outages)
```

---

## 8. Azure CDN

### What is it?
Content Delivery Network distributing content from 200+ edge locations globally for low-latency access.

### When to Use
- Static content distribution (images, CSS, JS)
- Video/media streaming
- Global user base
- Reduce origin server load

### Key Features
- **Edge Locations**: 200+ globally
- **Dynamic Content**: Newer CDN supports dynamic acceleration
- **Caching**: Configurable TTL
- **Compression**: GZIP compression of content
- **HTTPS**: Full HTTPS support

### Pros & Cons

**Pros** ✅
- Dramatically faster for global users
- Reduces origin server load
- Scales automatically
- Very affordable
- Flexible caching options

**Cons** ❌
- Cache invalidation complexity
- Not suitable for real-time data
- Edge locations outside your control
- Regional cache misses still hit origin
- Versioning required for updates

### Limits & Constraints

| Resource | Limit |
|----------|-------|
| CDN profiles per subscription | 25 |
| Endpoints per profile | 25 |
| Custom domains per endpoint | 10 |
| Cache TTL | Customizable, max varies |

### Real-World Use Cases

#### Use Case 1: Website Content Distribution
```
User in Singapore
    ↓ Requests /image.jpg
    ↓ Served from Singapore edge (cached)
    ↓ Cache hit: 50ms response

User in London
    ↓ Requests /image.jpg
    ↓ Served from London edge (cached)
    ↓ Cache hit: 20ms response

Origin Server (Azure Storage)
    ↓ Only needs to serve cache misses
```

---

## 9. Azure Firewall

### What is it?
Managed, cloud-based network security service with stateful inspection and threat protection across VNets and regions.

### When to Use
- Centralized network security
- Multi-VNet management
- DDoS protection + WAF needed
- Complex security policies

### Key Features
- **Stateful Firewall**: Monitor connections
- **FQDN Filtering**: Block domains
- **Network Rules**: Layer 3-4 filtering
- **Application Rules**: Layer 7 filtering
- **Threat Intelligence**: Block known malicious IPs

### Pros & Cons

**Pros** ✅
- Centralized security management
- Multi-VNet support
- Built-in DDoS protection
- Threat intelligence feeds
- FQDN and URL filtering

**Cons** ❌
- Higher cost than NSGs
- Added latency for all traffic
- Complex rule management
- Overkill for simple topologies
- Requires hub-and-spoke VNet design

### Limits & Constraints

| Resource | Limit |
|----------|-------|
| Firewalls per region | 10 |
| Network rules per firewall | 10000 |
| Application rules per firewall | 10000 |
| Throughput | 30 Gbps |

### Real-World Use Cases

#### Use Case 1: Hub-and-Spoke Network Security
```
Hub VNet (Firewall)
├── NSG rules
├── FQDN filtering
└── Application rules
    ↓
Spokes (App, DB, Worker)
    └── All traffic through hub firewall
    (Centralized security policy)
```

---

## Architecture Patterns

### Pattern 1: Secure Multi-Tier Application
```
Internet
    ↓
Azure Front Door (WAF)
    ↓
Application Gateway (SSL termination, URL routing)
    ↓
NSG (Allow HTTP from App Gateway only)
    ↓
Web Tier (Public)
    ↓
NSG (Allow from Web tier only)
    ↓
Application Tier (Private)
    ↓
NSG (Allow from App tier only)
    ↓
Database (Private, no internet access)
```

### Pattern 2: Global Disaster Recovery
```
Region 1: Primary
├── VNet + Services
└── Replicated to Region 2

Region 2: Secondary
├── VNet + Services (standby)
└── Available for failover

Azure Front Door
├── Route healthy traffic to Region 1
└── Failover to Region 2 if Region 1 down
(Users experience seamless transition)
```

### Pattern 3: Hybrid Cloud
```
On-Premises
├── VPN Gateway
└── ExpressRoute (primary)
    ↓
Azure VNet (Hub)
├── VPN Gateway (backup)
└── ExpressRoute connection
    ↓
Hybrid services access:
    ├── On-prem databases
    ├── Azure databases
    ├── Branch offices
```

---

## Performance Comparison

| Metric | Load Balancer | App Gateway | Front Door | VPN | ExpressRoute |
|--------|---------------|-------------|-----------|-----|--------------|
| Latency | < 1ms | 5-20ms | 30-200ms | 50-200ms | < 1ms |
| Throughput | 4.6 Mbps | 2.5 Gbps | Unlimited | ~1 Gbps | Up to 100 Gbps |
| Cost | Low | Medium | High | Low-Medium | Very High |
| Setup Time | Minutes | Minutes | Minutes | Hours | Weeks |
| Global | No | No | Yes | No | Optional |

---

## Interview Questions

1. **Describe VNet design for a 3-tier application**
   - Frontend subnet (public), Application subnet (private), Database subnet (private)
   - NSGs isolate traffic between tiers
   - Only frontend accepts internet traffic
   - Application talks to both frontend and database
   - Database only accepts from application

2. **How would you architect hybrid connectivity?**
   - Primary: ExpressRoute (dedicated, guaranteed SLA)
   - Backup: VPN Gateway (failover if ExpressRoute down)
   - On-premises gateway coordinates both connections
   - BGP routing automatically switches between paths

3. **Design global application with failover**
   - Azure Front Door routes users to nearest region
   - Health probes detect regional failures
   - Automatic failover to secondary region
   - Database replication for consistency

4. **NSG vs Azure Firewall - when to use each?**
   - NSG: Single subnet/NIC, simple rules, no cost
   - Firewall: Multi-VNet, centralized policy, threat intelligence, needs hub-and-spoke

5. **Load Balancer vs App Gateway vs Front Door - differences?**
   - Load Balancer: Layer 4, low latency, all protocols
   - App Gateway: Layer 7, URL routing, WAF, HTTPS
   - Front Door: Global, multi-region, edge caching

6. **Optimize network costs**
   - Use service endpoints (cheaper than public endpoint access)
   - Implement data transfer optimization
   - Cache static content at CDN edge
   - Use peering instead of gateways where possible

7. **Troubleshoot intermittent connectivity**
   - Check health probes (Load Balancer/App Gateway)
   - Verify NSG rules and service endpoints
   - Monitor packet loss and latency
   - Check backend pool status

8. **What's the scale limit for peering?**
   - 500 peerings per VNet
   - Transitive peering requires manual routing (no automatic propagation)
   - Hub-and-spoke design for many VNets

---

## Cost Optimization Tips

1. **Choose right tool for job**: Load Balancer << App Gateway < Front Door
2. **Use service endpoints**: Cheaper than public endpoints
3. **Implement caching**: Reduce load on origin servers
4. **Clean up unused public IPs**: Each has hourly cost when not assigned
5. **Optimize data transfers**: Azure to Azure is free; to Internet is charged
6. **Use Azure CDN for static content**: Dramatically reduces origin load
7. **Consolidate services**: Fewer gateways = lower costs

---

**Last Updated**: April 2026  
**Difficulty**: Intermediate to Advanced  
**Focus**: Networking architecture, connectivity, security
