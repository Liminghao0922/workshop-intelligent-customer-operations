# Exercise 3 - Test the Voice Conversation

## Objective

Place an inbound call and verify the complete speech-to-knowledge-to-speech loop.

## 1. Start with a Supported Question

Call the ACS phone number and ask:

```text
What information do I need to check product warranty coverage?
```

Expected behavior:

1. ACS sends `IncomingCall` through Event Grid.
2. Gateway answers and plays a welcome message.
3. ACS recognizes the customer utterance.
4. Gateway sends the text to the Knowledge Agent.
5. The agent retrieves approved content from Azure AI Search.
6. Gateway plays the concise grounded answer.

## 2. Test Grounding and Language

Ask an unsupported question:

```text
Can you guarantee a full refund today?
```

The agent must not invent a guarantee.

Then request another language and ask a support question. For example:

```text
Please switch to Japanese.
```

The next response should use the requested language.

## 3. End the Call

Say a supported closing phrase such as:

```text
Thank you, goodbye.
```

Wait for the closing prompt and disconnection.

## 4. Inspect the Call Record

```powershell
$calls = Invoke-RestMethod "$gatewayUrl/api/calls"
$latestCall = $calls | Select-Object -First 1
$latestCall | ConvertTo-Json -Depth 12
```

Verify:

- `status` is `completed`.
- `completedAt` has a value.
- Conversation turns contain customer and assistant messages.
- Artifacts include incoming and callback events.
- The assistant response does not claim a Dynamics case was created.

## 5. Review Gateway Logs

```powershell
az containerapp logs show `
  --name $gatewayApp `
  --resource-group $resourceGroup `
  --type console `
  --tail 100
```

Look for `Incoming ACS call received`, `Recognize completed`, and `Call connected callback received`. Do not include customer utterances or phone numbers in shared screenshots.

## Validation

- [ ] Inbound call is answered
- [ ] Supported question receives a grounded spoken answer
- [ ] Unsupported request does not receive an invented commitment
- [ ] Language switch works
- [ ] Closing phrase ends the call cleanly
- [ ] Latest call record is completed and contains the transcript
- [ ] No Dynamics case is created during the live conversation
