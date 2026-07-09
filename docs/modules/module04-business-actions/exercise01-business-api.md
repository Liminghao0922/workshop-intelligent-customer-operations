# Exercise 1 - Prepare Business API

## Objective

Prepare a backend API that simulates business actions.

## Example Actions

- Check order status
- Check service request status
- Create escalation request
- Register callback request

## Source

See `src/aspire/IntelligentCustomerOperations.Gateway`:

- `Program.cs` (tool endpoints)
- `Services/TicketService.cs`
- `Services/DynamicsCaseClient.cs`

## Tasks

1. Review the API endpoints exposed by the gateway.
2. Confirm each endpoint has a clear input and output shape.
3. Ensure each action returns predictable JSON for workshop testing.
4. Note which actions should trigger escalation.

## Validation

- [ ] API returns predictable JSON
- [ ] Status lookup works with a request ID
- [ ] Escalation action returns a clear confirmation payload
