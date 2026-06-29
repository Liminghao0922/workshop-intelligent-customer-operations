# Quiz: Design a Smart Call Center

## Scenario

A company wants to modernize its call center. Customers should be able to call a phone number and talk naturally with an AI voice agent. If the AI cannot resolve the issue, the system should create context for a human agent. After each call, the company wants analytics for quality improvement, product feedback, and operational reporting.

## Student task

Design a cloud architecture diagram and explain why each component is needed. The diagram must be implementable, not only conceptual. Choose appropriate cloud services yourself based on the requirements.

## Functional requirements

1. Accept customer calls through a phone number or existing contact-center telephony system.
2. Support a real-time voice agent that can listen, reason, and respond with low latency.
3. Use enterprise knowledge, such as FAQs, policies, or product manuals, to answer customer questions.
4. Escalate to a human agent or create a CRM ticket when the AI cannot resolve the issue.
5. Store call audio, transcript, and structured call metadata.
6. Run a post-call analytics pipeline that extracts summary, intent, sentiment, entities, resolution status, and action items.
7. Redact or protect personal data before sending transcripts to generative AI analytics.
8. Provide dashboards for business users and supervisors.
9. Capture telemetry, failures, model usage, and call quality metrics.
10. Support English, Japanese, and Chinese customer experiences.

## Non-functional requirements

- Security: use enterprise identity, service-to-service authentication, least privilege access control, encrypted storage, and private networking where required.
- Reliability: design for retry, idempotent batch processing, durable storage, and failure isolation between real-time call handling and offline analytics.
- Performance: optimize the real-time path for low latency; keep heavy analytics out of the live call path.
- Cost: use event-driven compute for post-call processing and store audio in appropriate hot, cool, or archive tiers.
- Observability: include technical telemetry, application traces, model usage metrics, call quality metrics, and business-level metrics.
- Compliance: protect PII and define retention for audio and transcripts.

## Required deliverables

Students submit:

1. One architecture diagram.
2. A short explanation of the real-time call flow.
3. A short explanation of the post-call analytics flow.
4. A list of selected cloud services or platform capabilities and why they were chosen.
5. Security, reliability, cost, and observability considerations.
6. A short note on how English, Japanese, and Chinese are supported.

## Important design hint

Do not put every task in the live voice path. The real-time path should focus on conversation, retrieval, and immediate handoff. Batch analytics should run after the call so that summaries, sentiment, trend analysis, and reporting do not increase call latency.
