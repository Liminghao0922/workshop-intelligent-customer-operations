# Module 06 - End-to-End Validation

## Goal

Validate the full customer request automation journey from first question to final resolution or escalation.

## Topics

- FAQ request scenario
- Order or case lookup scenario
- Escalation scenario
- Validation checklist
- Demo narrative

## Expected Call Metadata (Aspire)

Each `GET /api/calls/{id}` response should include:

- `id`
- `status`
- `language`
- `analyticsStatus`
- `ticket` (if escalated)
- `postCallResult` (after analyze step)

## Expected Output

By the end of this module, participants should have:

- A tested request-to-resolution flow
- At least one grounded FAQ scenario
- At least one business action scenario
- At least one escalation scenario

## Exit Criteria

- [ ] FAQ scenario returns grounded answers
- [ ] Status lookup scenario triggers a tool call correctly
- [ ] Escalation scenario produces a clear handoff outcome
- [ ] Call metadata contains expected fields
- [ ] Ready to proceed to Module 07

## Module Checklist

- [ ] Read module overview
- [ ] Complete Scenario 1 - FAQ Request
- [ ] Complete Scenario 2 - Order or Service Request Status
- [ ] Complete Scenario 3 - Escalation
- [ ] Verify call metadata in the response
- [ ] Proceed to Module 07
