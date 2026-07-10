# Exercise 2 - Connect ACS Inbound Calls

## Objective

Acquire an ACS phone number and route `IncomingCall` events to the public Voice Gateway.

## Event Boundary

Azure Communication Services uses two event paths:

| Event path | Purpose | Gateway endpoint |
| --- | --- | --- |
| Event Grid | Delivers the initial `IncomingCall` event | `/api/acs/events` |
| Call Automation webhook | Delivers mid-call events after Gateway answers | `/api/acs/callbacks/{callId}` |

The callback URL must be publicly reachable over HTTPS with a valid certificate. Azure Container Apps provides this endpoint for the workshop.

## 1. Locate the ACS Resource

```powershell
$acsId = az resource list `
  --resource-group $resourceGroup `
  --resource-type Microsoft.Communication/CommunicationServices `
  --query "[0].id" `
  --output tsv

$acsName = ($acsId -split '/')[-1]
```

## 2. Acquire a Phone Number

Phone-number availability and regulatory requirements vary by country or region, so complete this step in the Azure portal:

1. Open the ACS resource `$acsName`.
2. Select **Phone numbers** → **Get**.
3. Choose a country or region that supports inbound PSTN calling for your subscription.
4. Select a number with **Make calls** and **Receive calls** capabilities.
5. Complete the purchase and record the number in E.164 format.

Use a pre-provisioned instructor number when workshop subscriptions cannot purchase phone numbers.

## 3. Create the IncomingCall Subscription

```powershell
$eventEndpoint = "$gatewayUrl/api/acs/events"

az eventgrid event-subscription create `
  --name incoming-call-sub `
  --source-resource-id $acsId `
  --included-event-types Microsoft.Communication.IncomingCall `
  --endpoint-type webhook `
  --endpoint $eventEndpoint
```

Gateway handles Event Grid subscription validation at the same endpoint.

## 4. Verify the Subscription

```powershell
az eventgrid event-subscription show `
  --name incoming-call-sub `
  --source-resource-id $acsId `
  --query "{state:provisioningState, endpoint:destination.endpointUrl, events:filter.includedEventTypes}" `
  --output json
```

Confirm `provisioningState` is `Succeeded` and the event list contains only `Microsoft.Communication.IncomingCall`.

## 5. Confirm Gateway Configuration

```powershell
az containerapp show `
  --name $gatewayApp `
  --resource-group $resourceGroup `
  --query "properties.template.containers[0].env[?name=='PUBLIC_BASE_URL' || name=='ACS_CONNECTION_STRING'].name" `
  --output tsv
```

This command checks that the settings exist without printing the ACS connection string.

## Validation

- [ ] ACS phone number can receive calls
- [ ] Event Grid subscription state is `Succeeded`
- [ ] Subscription includes only `Microsoft.Communication.IncomingCall`
- [ ] Endpoint is `$gatewayUrl/api/acs/events`
- [ ] ACS connection setting exists without exposing its value
