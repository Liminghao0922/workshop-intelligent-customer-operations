# Exercise 1 - Design Agent Roles

## Objective

Design specialized agents for customer operations.

## Suggested Agents

| Agent | Responsibility |
|---|---|
| Supervisor Agent | Route request and coordinate specialists |
| Knowledge Agent | Answer policy and product questions |
| Action Agent | Execute business actions |
| Escalation Agent | Handle human handoff and review |

## Guidance

- Keep each role narrow and explainable
- Avoid overlapping responsibility where possible
- Use the supervisor to decide routing, not to do every task itself

## Validation

- [ ] Each agent has a clear responsibility
- [ ] Boundaries between agents are easy to explain
