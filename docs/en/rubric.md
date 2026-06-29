# Scoring Rubric

Total: 100 points.

| Area | Points | What to look for |
| --- | ---: | --- |
| Real-time voice design | 20 | Telephony provider, voice gateway, Voice Live API, low-latency design, human handoff |
| Post-call analytics | 20 | Storage trigger, transcription, PII redaction, summarization, sentiment, intent, dashboards |
| Implementability | 15 | Uses real Azure services with clear data flow and feasible integrations |
| Security and compliance | 15 | Managed identity, RBAC, encryption, PII handling, retention, network controls |
| Reliability and operations | 10 | Retries, idempotency, failure isolation, monitoring, alerting |
| Cost and performance | 10 | Separates live path from batch path, event-driven compute, storage tiers |
| Multilingual design | 10 | English, Japanese, and Chinese support across voice, prompts, knowledge, analytics |

## Common mistakes

- Mixing heavy analytics into the real-time call path.
- Sending raw PII directly to generative AI analytics.
- Drawing a diagram that names "AI" but omits telephony, storage, triggers, monitoring, or CRM handoff.
- Forgetting dashboards and operational telemetry.
- Treating multilingual support as UI translation only.
