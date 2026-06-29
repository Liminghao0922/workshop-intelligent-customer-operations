# Observability & Evaluation (Lightweight v3)

## Correlation ID

The backend assigns a `correlationId` for each `/api/chat` request and returns it in the response.

## Minimum Logging

- intent classification result
- whether tool was called
- tool name
- correlation ID

## Response Evaluation Checklist

- [ ] Response matches user intent
- [ ] Tool called only when needed
- [ ] Missing parameters trigger clarification
- [ ] Escalation response is clear and safe

## Demo Validation Table

| Scenario | Expected Intent | Tool Called |
|---|---|---|
| Warranty question | `faq_request` | No |
| Service request status + ID | `service_request_status` | Yes |
| Repair status without ID | `missing_information` | No |
| Repeated unresolved issue | `escalation_request` | No |
