# FAQ

## Why use Azure AI Search for the knowledge layer?

This workshop uses Azure AI Search as the enterprise knowledge foundation because it provides sub-second vector and hybrid search responses with no cold-start latency. This makes it suitable for real-time agent calls in both workshop and production scenarios. Microsoft Foundry Agent handles reasoning, orchestration, and actions on top of the search results.

## Why call this Customer Operations instead of Call Center?

Customer Operations covers broader workflows such as service requests, support policies, order status, escalation, and internal operations. It is not limited to voice call center scenarios.

## Can this be extended to multi-agent architecture?

No supervisor agent is required for the core workshop. The solution intentionally uses two agents with separate responsibilities: live knowledge answering and post-call analysis.
