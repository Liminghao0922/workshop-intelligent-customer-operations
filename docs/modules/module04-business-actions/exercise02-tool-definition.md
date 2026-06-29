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

## Validation

The agent should be able to decide when a tool call is needed.

## v3 Artifact Location

Tool schema is stored at:

```text
contracts/tool-schemas/get-service-request-status.json
```
