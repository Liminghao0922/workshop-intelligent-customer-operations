# Scenario 2 - Order or Service Request Status

## Scenario

The customer asks for order or service request status.

## Expected Flow

```text
Customer request
  ↓
Agent asks for missing identifier if needed
  ↓
Agent calls business API
  ↓
Agent summarizes status
```

## Validation

- [ ] Agent collects required information
- [ ] Tool call is executed
- [ ] Response includes clear next step
