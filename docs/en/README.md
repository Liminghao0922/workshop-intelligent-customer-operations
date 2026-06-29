# Smart Call Center Architecture Quiz

Use this material to ask students to convert two Azure solution patterns into clear requirements and then design an architecture diagram that can be implemented.

## Teaching flow

1. Share [quiz.md](quiz.md) with students.
2. Ask them to draw an architecture that satisfies the functional and non-functional requirements.
3. Score their work with [rubric.md](rubric.md).
4. Use [reference-architecture.md](reference-architecture.md) for instructor discussion.
5. End with the local demo in [demo-guide.md](demo-guide.md).

## Expected architecture theme

The best answer combines two flows:

- Real-time voice agent: telephony integration, low-latency speech-to-speech conversation, knowledge retrieval, CRM action.
- Post-call analytics: recorded audio or transcript, batch transcription, PII redaction, summarization, sentiment, intent, reporting.
