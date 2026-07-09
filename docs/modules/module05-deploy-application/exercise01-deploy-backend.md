# Exercise 1 - Deploy Backend

## Objective

Deploy the backend service used by the customer operations app.

## Backend Responsibilities

- Receive frontend requests
- Call Microsoft Foundry Agent
- Route tool calls to business APIs
- Return responses to frontend

## Tasks

1. Start the backend service or deploy it to the target host.
2. Confirm the health endpoint returns `200`.
3. Confirm the agent endpoint accepts a request and returns a response.
4. Record the backend URL for the app settings.

## Validation

- [ ] Health endpoint returns `200`
- [ ] Agent endpoint responds successfully
- [ ] Backend URL is recorded for configuration
