# Exercise 2 - Configure Agent Instructions

## Objective

Configure the instruction set for the customer operations agent.

## Starter Instruction

Use `prompts/agent-instructions/customer-operations-agent.md` as the baseline.

## Key Behavior

- Be helpful and concise
- Use knowledge for factual answers
- Do not invent policy details
- Use tools for business actions
- Escalate high-risk or ambiguous requests

## Suggested Instruction Sections

- Role and goal
- What to do for FAQ requests
- What to do when information is missing
- When to use tools
- When to escalate

## Validation

- [ ] Agent follows scope and escalation rules
- [ ] Responses are consistent across multiple test prompts
- [ ] Agent does not invent unsupported policy details
