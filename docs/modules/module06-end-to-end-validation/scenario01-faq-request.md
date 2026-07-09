# Scenario 1 - FAQ Request

## Scenario

The customer asks a product or warranty question.

## Expected Flow

```text
Customer question
  ↓
Agent understands intent
  ↓
Agent retrieves answer from Azure AI Search knowledge
  ↓
Agent returns grounded response
```

## Validation

- [ ] Response uses knowledge data
- [ ] No tool call is needed
- [ ] Response is concise and useful
- [ ] Response is customer-friendly
- [ ] Response does not invent policy details
