# Module 03 - Build AI Agent with Microsoft Foundry

## Goal

Create the customer operations agent that understands requests, uses knowledge, and follows workshop guardrails.

## Topics

- Create the agent
- Define system instructions and behavior rules
- Connect the search index as knowledge
- Test conversation behavior
- Check response quality and escalation handling

## Implementation Note

This module uses `FoundryClient` in:

```text
src/aspire/IntelligentCustomerOperations.Gateway/Services/FoundryClient.cs
```

It supports:

- local fallback mode (`APP_MODE=mock`)
- Microsoft Foundry SDK mode (`source=foundry-agent-sdk`)

Validation intents used later in the flow:

- `faq_request`
- `service_request_status`
- `missing_information`
- `escalation_request`

## Expected Output

By the end of this module, participants should have:

- A working Microsoft Foundry agent
- Clear instructions and guardrails
- A connected knowledge source
- A basic conversation test set

## Exit Criteria

- [ ] Agent created in Microsoft Foundry
- [ ] Instructions written for customer operations behavior
- [ ] Search knowledge connected successfully
- [ ] Sample conversations return sensible responses
- [ ] Ready to proceed to Module 04

## Module Checklist

- [ ] Read module overview
- [ ] Complete Exercise 1 - Create Customer Operations Agent
- [ ] Complete Exercise 2 - Configure Agent Instructions
- [ ] Complete Exercise 3 - Connect Knowledge
- [ ] Validate at least one FAQ and one escalation scenario
- [ ] Proceed to Module 04
