# Exercise 1 - Create the Call Analysis Agent

## Objective

Create the workshop's second and final Foundry agent with a strict structured-output contract.

## 1. Create the Agent

In **New Foundry**, open the same project used in Part 1:

1. Navigate to **Build** → **Agents**.
2. Select **New agent** → **Build an agent**.
3. Set the name to `call-analysis-agent-$postfix`.
4. Select the `gpt-5` deployment.

Do not attach Azure AI Search. This agent analyzes the supplied transcript; it does not answer live customer questions.

## 2. Configure the Contract

Copy `prompts/agent-instructions/call-analysis-agent.md` into the instructions field and save the agent.

The required output includes:

- `callId` for correlation and idempotency
- `summary`, category, sentiment, and resolution status
- `followUpRequired` and `followUpReason`
- constrained priority and confidence
- customer commitments and action items

## 3. Test with a Masked Transcript

```text
Call ID: test-unresolved-001
Language: en
Customer: I was charged twice for my subscription.
Assistant: I cannot confirm a refund. A billing specialist needs to review the transactions.
Customer: Please have someone contact me.
```

Confirm the response is one JSON object, preserves `test-unresolved-001`, and sets `followUpRequired` to `true`.

Test a resolved call:

```text
Call ID: test-resolved-001
Language: en
Customer: What information do I need for a warranty check?
Assistant: You need the product identifier and proof of purchase.
Customer: That answers my question. Thank you.
```

The result should set `followUpRequired` to `false` and `resolutionStatus` to `resolved`.

## 4. Record the Agent ID

```env
FOUNDRY_ANALYTICS_AGENT_ID=<call-analysis-agent-reference>
```

## Validation

- [ ] Call Analysis Agent exists and uses `gpt-5`
- [ ] No Search tool is attached
- [ ] Output is valid JSON with schema version `1.0`
- [ ] `callId` is preserved
- [ ] Unresolved and resolved examples produce different follow-up decisions
- [ ] Agent does not claim to create a ticket
