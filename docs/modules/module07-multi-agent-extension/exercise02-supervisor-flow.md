# Exercise 2 - Supervisor Flow

## Objective

Create a conceptual supervisor flow for routing customer requests.

## Flow

```text
Customer Request
  ↓
Supervisor Agent
  ├─ Knowledge Agent
  ├─ Action Agent
  └─ Escalation Agent
```

## Guidance

- Route FAQ questions to knowledge first
- Route requests with identifiers to action tools
- Route risky or ambiguous requests to escalation

## Validation

- [ ] Routing logic is easy to explain
- [ ] Knowledge, action, and escalation paths are distinct
- [ ] Supervisor flow improves maintainability
