# Workshop - Intelligent Customer Operations

Build an intelligent customer request automation solution using **Azure AI Foundry Agents** and **Microsoft Fabric IQ**.

This repository is designed as both:

1. A learner portal published with GitHub Pages / MkDocs.
2. A hands-on workshop source repository containing sample application code, data, prompts, architecture assets, instructor materials, and deployment placeholders.

## Workshop Story

Participants will build an end-to-end customer request automation experience:

```text
Customer Request
    ↓
Customer Operations Web App
    ↓
Azure AI Foundry Agent
    ↓
Microsoft Fabric IQ / Enterprise Knowledge
    ↓
Business APIs / Actions
    ↓
Response / Resolution / Escalation
```

## Recommended Repository Name

```text
workshop-intelligent-customer-operations
```

## High-Level Modules

| Module | Name | Outcome |
|---|---|---|
| 00 | Workshop Overview | Understand business scenario and target architecture |
| 01 | Prerequisites & Environment Setup | Prepare Azure, Fabric, Foundry, GitHub, and local tools |
| 02 | Knowledge Foundation with Fabric IQ | Prepare enterprise data and knowledge grounding |
| 03 | Build AI Agent with Azure AI Foundry | Create and test a customer operations agent |
| 04 | Business Actions & Tool Calling | Connect the agent to backend APIs / actions |
| 05 | Deploy Customer Operations App | Deploy frontend, API, and agent integration |
| 06 | End-to-End Validation | Validate request-to-resolution scenarios |
| 07 | Multi-Agent Extension | Extend into supervisor / specialist agent workflow |

## Local Preview

```bash
pip install -r requirements.txt
mkdocs serve
```

Then open the local MkDocs URL shown in your terminal.

## Publish to GitHub Pages

This repo includes a GitHub Actions workflow under `.github/workflows/publish-docs.yml`.

Recommended GitHub Pages setting:

```text
Source: GitHub Actions
```

## Repo Structure

```text
workshop-intelligent-customer-operations-v2/
├── docs/                 # Learner portal content
├── src/                  # Sample app and backend code placeholders
├── data/                 # Sample customer operations data
├── infra/                # Infrastructure-as-code placeholders
├── prompts/              # Agent instructions and prompt assets
├── assets/               # Images, diagrams, icons
├── slides/               # Deck placeholders
├── instructor/           # Delivery guide, timing, speaker notes
├── scripts/              # Setup and utility scripts
├── .github/workflows/    # GitHub Pages publishing workflow
├── mkdocs.yml
└── requirements.txt
```
