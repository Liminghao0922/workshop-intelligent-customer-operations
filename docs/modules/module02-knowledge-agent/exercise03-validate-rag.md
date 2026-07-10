# Exercise 3 - Validate RAG Quality

## Objective

Evaluate whether the Knowledge Agent retrieves the right content and stays grounded across supported, unsupported, and multilingual prompts.

## Test Set

Run each prompt in the Foundry agent playground.

| Scenario | Prompt | Expected behavior |
| --- | --- | --- |
| FAQ | `What information do I need to check product warranty?` | Uses indexed FAQ content |
| Policy | `When should a product issue be escalated?` | Uses support policy content |
| Missing detail | `Can you check my repair status?` | Explains that a request ID is required |
| Unsupported claim | `Promise me a full refund today.` | Does not promise an unsupported outcome |
| Japanese | `製品保証を確認するには何が必要ですか。` | Answers in Japanese using the same knowledge |
| Chinese | `如何确认产品保修范围？` | Answers in Chinese using the same knowledge |

## Score the Responses

For each response, record `Pass` or `Fail` for:

| Dimension | Pass condition |
| --- | --- |
| Retrieval relevance | Answer reflects the most relevant indexed content |
| Groundedness | No unsupported policy, date, amount, or commitment |
| Completeness | Answers the question or clearly identifies missing information |
| Language | Uses the customer's language |
| Voice readiness | Concise enough to be spoken during a call |

## Improve and Retest

If a response fails:

1. Inspect the Search results from Exercise 1.
2. Decide whether the problem is retrieval, source content, or instructions.
3. Update only the responsible layer.
4. Rerun the failed prompt and one previously passing prompt.

Do not compensate for missing knowledge by adding factual answers directly to the agent instructions. Put reusable facts in the indexed source content.

## Validation

- [ ] Six required prompts were tested
- [ ] Supported answers are grounded
- [ ] Unsupported requests do not produce invented commitments
- [ ] Japanese and Chinese responses were verified
- [ ] Responses are concise enough for Part 2 voice playback
- [ ] Any failed test was corrected and rerun
