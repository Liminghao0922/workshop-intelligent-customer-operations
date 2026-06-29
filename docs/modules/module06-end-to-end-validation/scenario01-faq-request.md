# Scenario 1 - FAQ Request

## Scenario

The customer asks a product or warranty question.

## Expected Flow

```text
Customer question
  ↓
Agent understands intent
  ↓
Agent retrieves answer from Fabric IQ knowledge
  ↓
Agent returns grounded response
```

## Validation

- [ ] Response uses knowledge data
- [ ] No tool call is needed
- [ ] Response is concise and useful
