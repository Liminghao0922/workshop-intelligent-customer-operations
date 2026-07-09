# Exercise 2 - Define Agent Tools

## Objective

Define tool schemas that allow the agent to call backend business actions.

## Example Tool

```json
{
  "name": "getServiceRequestStatus",
  "description": "Get service request status by request ID",
  "parameters": {
    "type": "object",
    "properties": {
      "requestId": {
        "type": "string",
        "description": "Service request ID"
      }
    },
    "required": ["requestId"]
  }
}
```

## Recommended Tool Set

- `getServiceRequestStatus`
- `getOrderStatus`
- `createEscalationRequest`
- `registerCallbackRequest`

## Validation

- [ ] Tool schema matches backend API inputs
- [ ] Agent can decide when a tool call is needed
- [ ] Tool names are clear and action-oriented
