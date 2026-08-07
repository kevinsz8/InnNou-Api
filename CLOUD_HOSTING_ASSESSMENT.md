# InnNou — AWS vs Azure Infrastructure & Cost Assessment

**Status: research only, 2026-08-07 — no infrastructure provisioned, no migration started.** Recorded here as a durable reference; the same content was also delivered as a formatted report to the user. See `MEMORY.md`'s `project_cloud_hosting_decision_2026_08_07` entry for how to pick this back up in a future session.

## Recommendation: Azure

Azure wins here mainly because of what InnNou already is, not because AWS is worse in general. The whole backend is Microsoft's own stack: ASP.NET Core, and — more importantly — a database layer that leans hard on SQL-Server-specific T-SQL (`sp_getapplock`, `STRING_SPLIT`, filtered indexes, table-valued parameters, dozens of stored procedures per module). That's not a portability problem on either cloud — both AWS RDS and Azure SQL Database run real SQL Server — but Azure's SQL Database serverless/DTU tiers are meaningfully cheaper than AWS's only equivalent (RDS for SQL Server, which has no auto-pause and bakes in SQL Server licensing at a much higher fixed cost). Add that App Service is a turnkey fit for a single .NET process that needs WebSockets (SignalR) and an always-on background timer (the idempotency-key cleanup service) running side by side — versus AWS needing App Runner/Fargate plus a load balancer wired up separately to get the same thing — and Azure comes out both cheaper and lower-ops for a small team.

AWS is not a bad choice — it's just built for a different shape of team (one that will run many services, wants maximum primitive-level control, or is already living in AWS for other reasons). Neither applies to InnNou today.

## What's actually being hosted

Pulled directly from the codebase and config, not assumed:

| | |
|---|---|
| **API runtime** | .NET 10 / ASP.NET Core, Carter endpoints, MediatR, Dapper — one deployable process |
| **Database** | SQL Server, dev DB ≈ 850 MB today — every write path goes through a stored procedure |
| **Real-time** | SignalR notifications hub — needs WebSocket support, currently single-instance |
| **Background job** | `IdempotencyKeyCleanupService` — an always-running hosted service, not a cron/serverless trigger |
| **File storage** | Local disk today (logos, order PDFs, invoice attachments) — already built behind an `IStorage` seam, explicitly staged to move to S3 or Blob |
| **Frontend** | React + Vite SPA, production build ≈ 1.9 MB — pure static output, no server-side rendering |
| **Email** | Brevo SMTP relay — third-party, identical cost on either cloud |
| **Auth** | Self-issued JWT — stateless, no managed identity service required |

## How each piece maps onto each cloud

### Azure (recommended)

| Role | Service | Why |
|---|---|---|
| API | **App Service** (Linux, B1→S1) | Built-in WebSockets toggle for SignalR; "Always On" keeps the cleanup service alive; one resource, no load balancer to wire up |
| Database | **Azure SQL Database** — Standard DTU or serverless vCore | Same SQL Server engine the app already uses; sp_getapplock, STRING_SPLIT, filtered indexes, TVPs all supported as-is |
| File storage | **Blob Storage** | Drop-in for the existing local-disk storage seam — one of the two options the codebase already names |
| Landing + SPA | **Static Web Apps** | Free tier: 100 GB bandwidth, free SSL, custom domain, GitHub Actions deploy built in |
| Real-time (scale-out) | **Azure SignalR Service** *(later)* | Skip for V1 — App Service handles WebSockets directly for a single instance. Add only when scaling to 2+ API instances |
| Secrets | **Key Vault** | JWT signing key, SMTP creds, connection strings — out of appsettings.json |
| Monitoring | **Application Insights** | Included with App Service, minimal setup |

### AWS (alternative)

| Role | Service | Why |
|---|---|---|
| API | **App Runner** or ECS Fargate | App Runner is the closer analog to App Service; Fargate is more powerful but needs an Application Load Balancer wired up separately for WebSockets/ingress |
| Database | **RDS for SQL Server** (Web Edition, License Included) | Real SQL Server engine — same T-SQL compatibility as Azure. No serverless/auto-pause tier exists for SQL Server on RDS, unlike Aurora |
| File storage | **S3** | The other option the codebase already names for the storage seam |
| Landing + SPA | **S3 + CloudFront** | No single managed "static site" product — two services wired together, generous free tier at this scale |
| Real-time (scale-out) | No managed SignalR equivalent | Terminate WebSockets directly on the compute layer; scaling past one instance needs a Redis backplane you manage yourself |
| Secrets | **Secrets Manager** | Same role as Key Vault, small per-secret monthly fee |
| Monitoring | **CloudWatch** | More configuration than App Insights out of the box |

## Rough monthly cost — two stages

Estimates, not quotes. US-region on-demand list pricing, no reserved-instance or savings-plan discounts applied (those would lower both columns further, roughly proportionally).

### Stage 1 — early access
*A handful of hotel organizations onboarded, light daily traffic, DB well under 5 GB. This is the "we just went live" number.*

| Component | Azure | $/mo | AWS | $/mo |
|---|---|---:|---|---:|
| API compute | App Service B1 (1 vCPU / 1.75 GB) | 13 | App Runner (~1 vCPU / 2 GB, low request volume) | 15–30 |
| Database | SQL DB Standard S0–S1 | 15–30 | RDS SQL Server Web Ed., db.t3.small — no auto-pause, runs 24/7 | 35–40 |
| File storage | Blob Storage, a few GB | 1–2 | S3, a few GB | 1–2 |
| Landing page + SPA | Static Web Apps (Free tier) | 0 | S3 + CloudFront (free-tier eligible) | 1–5 |
| Real-time (SignalR) | In-process on App Service | 0 | In-process on App Runner | 0 |
| DNS + misc | Azure DNS | 1 | Route 53 hosted zone | 1 |
| **Estimated total** | | **≈ $30–46** | | **≈ $53–78** |

### Stage 2 — growth
*Dozens of organizations, hundreds of active users, real production load, DB in the 10–20 GB range, HA/backups expected.*

| Component | Azure | $/mo | AWS | $/mo |
|---|---|---:|---|---:|
| API compute | App Service S1–P1v3 (2 vCPU+) | 70–150 | Fargate (2 vCPU/4GB) + ALB — ALB adds a fixed ~$20/mo App Service doesn't need | 90–140 |
| Database | SQL DB Standard S2–S3 / General Purpose 2 vCore | 60–300 | RDS SQL Server Standard Ed., Multi-AZ, db.t3/m5.large — Standard Ed. licensing + Multi-AZ roughly doubles the Stage-1 number | 300–600 |
| File storage | Blob Storage, tens of GB | 5 | S3, tens of GB | 5 |
| Landing page + SPA | Static Web Apps Standard | 9 | S3 + CloudFront | 10–20 |
| Real-time (SignalR, scaled out) | SignalR Service Standard | 50 | Self-managed Redis backplane on the compute layer | 15–30 |
| Monitoring + secrets | App Insights + Key Vault | 10–15 | CloudWatch + Secrets Manager | 10–20 |
| **Estimated total** | | **≈ $204–529** | | **≈ $430–815** |

## Why the gap is mostly the database, not the app server

1. **SQL Server licensing is the single biggest cost lever, and Azure owns it.** Azure SQL Database's compute pricing already has SQL Server licensing folded in at PaaS rates. RDS for SQL Server bills the same license, but as a "License Included" surcharge on top of full VM pricing — and there's no serverless/auto-pause tier for it (that only exists for AWS's own Aurora engines, not real SQL Server). Standard Edition + Multi-AZ, which Stage 2 needs for real uptime guarantees, is where the AWS number roughly doubles.
2. **App Service is one resource; the AWS equivalent is at least two.** SignalR needs WebSockets and the idempotency cleanup job needs an always-on process — App Service does both out of the box with a checkbox. App Runner also works standalone, but Fargate (the more flexible AWS option once you outgrow App Runner) needs an Application Load Balancer wired up separately, which is a fixed cost App Service simply doesn't have.
3. **The landing page ask is nearly free either way — but Azure's is one resource.** Static Web Apps is a single, purpose-built resource for "static marketing site + SPA, custom domain, free SSL, deploy from GitHub" — exactly the ask. AWS gets you there too, but by wiring S3 (storage) and CloudFront (CDN/SSL) together yourself.
4. **Compatibility is a wash — both run real SQL Server.** Deliberately not listed as an Azure advantage. RDS for SQL Server is genuine boxed SQL Server, not a compatibility-layer engine — `sp_getapplock`, filtered indexes, STRING_SPLIT, and TVPs all work identically on either cloud. The decision here is cost and operational shape, not "will our stored procedures run."

## Does call volume drive the price? (follow-up, 2026-08-07)

Short answer: **not for the architecture recommended above.** App Service (Azure) and Fargate (AWS) are flat, reserved-capacity pricing — you pay a fixed monthly amount for a slice of vCPU/RAM, and the bill doesn't move whether InnNou makes 100 or 100,000 backend calls a day, as long as that capacity isn't saturated. Same for Azure SQL Database's Standard (DTU) tier. This is a different billing model from the "pay per request" serverless products (AWS Lambda + API Gateway, Azure Functions Consumption plan) that used to define early-cloud pricing conversations — none of those were recommended here.

Two real exceptions worth flagging, given InnNou's actual call pattern (debounced search-as-you-type on every `SearchableSelect`, multiple parallel `GetPaged` calls per page, ~6 stored procedures on dashboard load — a chatty, always-a-little-busy SPA, not a quiet one):

- **Azure SQL Database serverless (vCore) mode** bills per-second while active and only stops billing compute after a full hour of *zero* activity. Given InnNou has real users online through most of business hours, it likely rarely gets that quiet window to actually auto-pause — so serverless probably won't save as much as the theory suggests. **Recommendation: default to the flat Standard (DTU) tier, not serverless, for predictability** — this was already the cheaper end of the Stage 1/2 ranges above, just calling it out as the safer pick specifically for this app's traffic shape.
- **AWS App Runner** bills per vCPU-hour while actively processing requests — with InnNou's chatty pattern, the API is probably "active" for most of the business day, which pushes the realistic AWS number toward the high end of the Stage 1/2 ranges above (or slightly past it), not the low end.

Bandwidth/egress (landing page, SPA, API JSON payloads) stays a non-issue regardless of call count — a REST API returning small JSON responses is nowhere near the free-tier bandwidth ceilings on either cloud; that's a real cost driver for video/large-media sites, not for this app.

## Before treating these numbers as a budget

- These are **list, on-demand, US-region** prices with no committed-use discount. A 1-year Azure Reservation or AWS Savings Plan typically knocks 20–40% off the compute and database lines on both sides once traffic is predictable enough to commit.
- SignalR/WebSocket cost on AWS in particular deserves a real spike, not just a pricing-page read — how AWS App Runner bills for an always-running background timer with near-zero HTTP traffic isn't fully documented and is worth testing before relying on the low end of the Stage-1 AWS number.
- Neither table includes a domain name, egress spikes from a bulk-import/export burst, or the cost of standing up a proper CI/CD pipeline (both clouds have a free/cheap path via GitHub Actions either way).
- Backups, point-in-time restore retention, and disaster-recovery region replication are excluded — both clouds price these similarly per-GB, so they don't move the recommendation either way, but they're real line items once InnNou has paying customers depending on uptime.
- Run both sides through the official calculators ([Azure Pricing Calculator](https://azure.microsoft.com/en-us/pricing/calculator/), [AWS Pricing Calculator](https://calculator.aws/)) with the actual target region before this becomes a real budget line — this document is a directional estimate to decide *which cloud*, not a quote.

---
*Prepared as an infrastructure/cost assessment for InnNou, based on the InnNou-Api/InnNou-Web codebases and August 2026 public pricing pages. Not a vendor quote.*
