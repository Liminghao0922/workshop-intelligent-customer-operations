# End-to-End Test Prompts (v3)

## Scenario 1 - FAQ

- Prompt: `What information do I need to check product warranty?`
- Expected intent: `faq_request`
- Expected tool call: `false`

## Scenario 2 - Status Lookup

- Prompt: `Can you check the status of service request SR-1001?`
- Expected intent: `service_request_status`
- Expected tool call: `true`
- Expected tool name: `getServiceRequestStatus`

## Scenario 3 - Missing Information

- Prompt: `Can you check my repair status?`
- Expected intent: `missing_information`
- Expected tool call: `false`

## Scenario 4 - Escalation

- Prompt: `This is the third time the same issue happened and I need escalation.`
- Expected intent: `escalation_request`
- Expected tool call: `false`
