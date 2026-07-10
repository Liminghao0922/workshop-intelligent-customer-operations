# Exercise 3 - Configure Dynamics Case Creation

## Objective

Allow deterministic Worker code to create one Dataverse `incident` record when validated analysis requires follow-up.

## 1. Prepare Dataverse

In the Power Platform admin center:

1. Select the Dynamics 365 Customer Service environment.
2. Create a custom text column on the Case table named `ico_callid`.
3. Create an alternate key using `ico_callid`.
4. Publish the table customization.

The alternate key makes the source `callId` the idempotency key. Do not rely on a pre-create query alone because concurrent retries can race.

## 2. Create the Application Identity

1. Create a single-tenant Microsoft Entra app registration.
2. Add it as an **Application User** in the target Power Platform environment.
3. Assign a custom security role with only the Case privileges needed by the workshop.
4. Create a client credential for the workshop or configure workload identity for production.

Do not assign System Administrator to the application user.

## 3. Review Dynamics Ownership in Worker

Open the Worker implementation:

- `DynamicsCaseClient.cs` upserts `incident` by the `ico_callid` alternate key.
- `CaseDecisionService.cs` applies application policy before calling Dataverse.
- `PostCallEventHubFunction.cs` stores completion only after the case decision succeeds.

Confirm `Microsoft.PowerPlatform.Dataverse.Client` is referenced by the Worker project and both services are registered in `Program.cs`.

Map validated analysis to Dataverse:

| Analysis field | Case field |
| --- | --- |
| `callId` | `ico_callid` |
| `summary` | `title` and `description` |
| `priority` | `prioritycode` |
| `followUpReason` | `description` |

Do not send the raw transcript to Dynamics.

## 4. Apply the Deterministic Decision Rule

The starter code creates or updates a case only when:

```text
followUpRequired == true
AND resolutionStatus is unresolved or follow_up
AND confidence >= 0.70
```

When confidence is below `0.70`, record `manual_review_required` in the analysis result instead of silently creating a case. This threshold is application policy, not an agent decision.

## 5. Configure Worker Secrets

Store the client secret in Key Vault and expose it to Container Apps as a secret reference. Configure Worker with:

```text
DYNAMICS_ORGANIZATION_URL=https://<organization>.crm.dynamics.com
DYNAMICS_TENANT_ID=<tenant-id>
DYNAMICS_CLIENT_ID=<application-id>
DYNAMICS_CLIENT_SECRET=<secret-reference>
```

Never place the secret in `.env`, Bicep parameter files, screenshots, or command history.

## 6. Store the Processing Result

Persist a result keyed by `eventId` containing:

- analysis status
- validated structured analysis
- Dynamics case ID and case number, when created
- completion timestamp
- failure category without sensitive payloads

Mark the event completed only after the Dataverse upsert succeeds or after a validated no-case decision is stored.

## Validation

- [ ] Dataverse Case table has unique alternate key `ico_callid`
- [ ] Application User has a least-privilege role
- [ ] Dynamics client runs in Worker, not the live-call path
- [ ] Decision policy is implemented in code
- [ ] Raw transcript is not written to Dynamics
- [ ] Credential is stored as a secret reference
- [ ] Retry with the same `callId` returns the same Case
