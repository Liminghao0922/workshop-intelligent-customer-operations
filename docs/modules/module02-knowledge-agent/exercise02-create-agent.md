# Exercise 2 - Create the Knowledge Agent

## Objective

Create a Foundry agent, configure its grounding rules, and connect it to the workshop Search index.

## 1. Open the Foundry Project

1. Open the Foundry resource from the [Azure portal](https://portal.azure.com).
2. Open the project created during Shared Setup.
3. Select **Go to Foundry portal**.
4. Confirm that you are using **New Foundry**.

## 2. Add the Azure AI Search Connection

1. Navigate to **Operate** → **Admin**.
2. Select the workshop project.
3. Select **Connected resources** → **Add connection**.
4. Select **Azure AI Search**.
5. Select `srch-customer-ops-$postfix`.
6. Select **API Key** authentication for this workshop.
7. Select **Connect**.

## 3. Create the Agent

Navigate to **Build** → **Agents** → **New agent** → **Build an agent**.

| Field | Value |
| --- | --- |
| Agent name | `customer-knowledge-agent-$postfix` |
| Model deployment | `gpt-5` |

## 4. Configure Instructions

Use instructions that keep the agent focused on grounded customer support:

```text
You are the customer support Knowledge Agent.

Use the connected Azure AI Search knowledge source for product, warranty,
service, and support-policy questions.

Rules:
- Answer in the language used by the customer.
- Use only information supported by the retrieved knowledge.
- If the knowledge source does not contain the answer, say that you cannot
  confirm it and recommend human follow-up.
- Do not invent policies, dates, prices, or service commitments.
- Keep spoken answers concise and easy to understand.
- Do not create tickets or claim that a business action was completed.
```

## 5. Connect the Search Tool

1. Open **Tools** for the new agent.
2. Select **Add** → **Browse all tools**.
3. Select **Azure AI Search**.
4. Select the workshop Search connection.
5. Select `customer-operations-knowledge`.
6. Add the tool and save the agent.

## 6. Run a Playground Test

Test a supported question:

```text
What information do I need to check product warranty coverage?
```

Then test an unsupported question:

```text
What is the guaranteed refund amount for every product?
```

The second answer must not invent a refund amount.

## 7. Record the Agent Reference

Update `.env`:

```env
AZURE_AI_PROJECT_ENDPOINT=<project-endpoint>
FOUNDRY_AGENT_ID=<customer-knowledge-agent-reference>
FOUNDRY_MODEL_DEPLOYMENT=gpt-5
```

## Validation

- [ ] Knowledge Agent exists in New Foundry
- [ ] Agent uses `gpt-5`
- [ ] Search connection and index are attached
- [ ] Instructions prohibit unsupported claims and ticket creation
- [ ] Supported and unsupported questions behave differently
- [ ] `FOUNDRY_AGENT_ID` is recorded
