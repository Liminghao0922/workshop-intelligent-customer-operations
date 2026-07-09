# Module 00 - Workshop Overview

## Goal

Help participants understand the business scenario, the customer request lifecycle, the role of each key component, and what will be built during this workshop.

## Business Scenario

Customer support teams often receive repetitive requests across email, portals, call centers, and service desks. Handling these requests manually is slow, inconsistent, and costly at scale.

This workshop demonstrates how AI agents can help customer operations teams:

- Understand incoming customer requests
- Retrieve relevant enterprise knowledge
- Execute business actions automatically
- Escalate to human agents when needed

The result is a faster, more consistent customer operations experience — without replacing the human judgment required for complex or sensitive situations.

## Request-to-Resolution Journey

The core storyline of this workshop follows a single customer request through the full resolution lifecycle:

```text
Customer Request
  ↓
Customer Operations App      (user-facing request interface)
  ↓
Microsoft Foundry Agent       (reasoning and orchestration layer)
  ↓
Azure AI Search / Knowledge          (knowledge foundation)
  ↓
Business Action Tool         (operational execution layer)
  ↓
Response / Resolution / Escalation
```

Each step maps directly to a workshop module. By Module 06, participants will have a working end-to-end flow.

## Target Architecture

The solution is composed of five major components:

| Component | Role |
|---|---|
| Customer Operations App | Receives customer requests and surfaces responses |
| Microsoft Foundry Agent | Orchestrates reasoning, tool calling, and response generation |
| Azure AI Search | Provides fast, low-latency vector search over indexed enterprise content |
| Business Action Tool | Executes operational tasks such as ticket creation or order lookup |
| Escalation Path | Routes unresolved or sensitive requests to human agents |

## Why Azure AI Search + Microsoft Foundry Agent

| Capability | What it provides |
|---|---|
| Microsoft Foundry Agent | Structured agent reasoning, tool calling, multi-turn conversation |
| Azure AI Search | Enterprise knowledge grounding from indexed documents and data |
| Combined | Agent answers are grounded in real enterprise content, not just model knowledge |

Azure AI Search indexes your organization's existing knowledge — product documentation, support policies, operational data — and serves it to the agent in real time with sub-second latency.

## Workshop Scope

During this workshop, participants will:

- Prepare enterprise knowledge for grounding
- Build or configure an AI agent for customer operations
- Connect the agent to knowledge and business action tools
- Deploy or run a customer operations application
- Validate end-to-end request-to-resolution scenarios

This workshop focuses on a minimum runnable customer operations experience. The goal is a working flow — not a production-hardened deployment.

## Future Expansion

The following capabilities are important for production or advanced scenarios, but are not the primary focus of this workshop:

- Full Fabric IQ / OneLake integration with production data sources
- Microsoft Foundry Agent deployment at scale
- Multi-agent orchestration with supervisor and specialist agents
- Human approval and handoff workflows
- Advanced observability, evaluation, and model quality tuning
- Production-grade security and governance
- Customer-specific industry scenario packs

Future modules or extensions can replace workshop-scope components with fully Azure-integrated services when needed.

## Module Roadmap

| Module | Name | Focus |
|---|---|---|
| 00 | Workshop Overview | Business scenario, architecture, scope |
| 01 | Prerequisites & Environment Setup | Azure, Cloud Shell, Foundry, and Azure AI Search |
| 02 | Knowledge Foundation with Azure AI Search | Enterprise knowledge index and retrieval |
| 03 | Build AI Agent with Microsoft Foundry | Agent creation, instructions, knowledge connection |
| 04 | Business Actions & Tool Calling | Connect agent to backend APIs and operations |
| 05 | Deploy Customer Operations App | Deploy frontend, API, and agent integration |
| 06 | End-to-End Validation | Request-to-resolution scenario validation |
| 07 | Multi-Agent Extension | Supervisor and specialist agent patterns |

## Recommended Read Order

1. This document (Module 00 overview)
2. `docs/architecture/solution-architecture.md`
3. `docs/architecture/sequence-flow.md`
4. Module 01 — `docs/modules/module01-environment-setup/index.md`

## Expected Output

By the end of Module 00, participants should have a shared understanding of:

- The business problem this workshop addresses
- The target customer request lifecycle
- The role of each major component
- The workshop module flow
- What will be implemented during the workshop
- What can be expanded later

## Exit Criteria

- [ ] Participants understand the customer request lifecycle
- [ ] Participants understand the target architecture and key components
- [ ] Participants understand the workshop scope and expected outcome
- [ ] Participants understand which capabilities are future expansion items
- [ ] Participants are ready to proceed to environment setup in Module 01

## Module Checklist

- [ ] Read this overview
- [ ] Review architecture links above
- [ ] Confirm shared understanding of scope and components with team
- [ ] Proceed to Module 01
