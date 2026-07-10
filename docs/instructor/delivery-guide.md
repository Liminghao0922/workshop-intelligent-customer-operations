# Instructor Delivery Guide

## Recommended Narrative

This workshop should be delivered as a business journey:

```text
Knowledge preparation → Live voice support → Call-ended event → Analysis → Conditional follow-up
```

Avoid positioning it as separate product training. The value comes from connecting a real-time customer experience to a durable, governed post-call workflow.

## Suggested Opening Message

Today we will build a voice support solution that answers from approved enterprise knowledge, reviews completed calls, and creates a Dynamics Case only when follow-up is required.

## Delivery Tips

- Start from the business problem.
- Show the end-to-end architecture early.
- Keep Azure AI Search positioned as the trusted knowledge grounding layer.
- Name the two agents consistently: Knowledge Agent and Call Analysis Agent.
- Reinforce that Voice Channel is not an agent.
- Show that deterministic Function code, not the agent, performs the Dynamics write.
