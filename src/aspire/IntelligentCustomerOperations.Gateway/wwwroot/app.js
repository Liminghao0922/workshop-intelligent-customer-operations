const configEl = document.querySelector("#config");
const callsEl = document.querySelector("#calls");
const liveCallMetaEl = document.querySelector("#liveCallMeta");
const liveTranscriptEl = document.querySelector("#liveTranscript");
const simulateButton = document.querySelector("#simulate");
const seedButton = document.querySelector("#seed");
const languageSelect = document.querySelector("#language");

let selectedCallId = null;

async function request(path, options) {
  const response = await fetch(path, {
    headers: { "Content-Type": "application/json" },
    ...options
  });
  if (!response.ok) {
    throw new Error(`${response.status}: ${await response.text()}`);
  }
  return response.json();
}

async function loadConfig() {
  configEl.textContent = JSON.stringify(await request("/api/config"), null, 2);
}

async function loadCalls() {
  const calls = await request("/api/calls");
  callsEl.innerHTML = calls.length ? calls.map((call) => renderCall(call, call.id === selectedCallId)).join("") : "<p>No calls yet.</p>";

  if (selectedCallId && !calls.some((call) => call.id === selectedCallId)) {
    selectedCallId = null;
    renderLiveTranscript(null);
  }
}

function renderCall(call, selected) {
  const turns = (call.transcript || []).map((turn) => (
    `<div class="turn"><strong>${turn.speaker}</strong>: ${turn.text}</div>`
  )).join("");
  return `
    <article class="call-card ${selected ? "selected" : ""}" data-call-id="${call.id}">
      <strong>${call.id}</strong>
      <div>Status: ${call.status} | Language: ${call.language} | Analytics: ${call.analyticsStatus}</div>
      ${turns}
      ${call.ticket ? `<div>Ticket: ${call.ticket.id} (${call.ticket.reason})</div>` : ""}
      <button data-analyze="${call.id}">Run post-call analytics</button>
    </article>
  `;
}

async function loadSelectedCall() {
  if (!selectedCallId) {
    renderLiveTranscript(null);
    return;
  }

  try {
    const call = await request(`/api/calls/${selectedCallId}`);
    renderLiveTranscript(call);
  } catch {
    selectedCallId = null;
    renderLiveTranscript(null);
  }
}

function renderLiveTranscript(call) {
  if (!call) {
    liveCallMetaEl.textContent = "Select a call to start live view.";
    liveTranscriptEl.innerHTML = "";
    return;
  }

  liveCallMetaEl.textContent = `Call ${call.id} | Status: ${call.status} | Language: ${call.language}`;
  const turns = call.transcript || [];
  liveTranscriptEl.innerHTML = turns.length
    ? turns.map((turn) => `
      <div class="timeline-item ${turn.speaker === "assistant" ? "assistant" : "customer"}">
        <div class="timeline-head">${turn.speaker} • ${turn.at || ""}</div>
        <div>${turn.text}</div>
      </div>
    `).join("")
    : "<p class=\"subtle\">No transcript yet.</p>";
}

simulateButton.addEventListener("click", async () => {
  await request("/api/dev/simulate-call", {
    method: "POST",
    body: JSON.stringify({ language: languageSelect.value })
  });
  await loadCalls();
});

seedButton.addEventListener("click", async () => {
  const result = await request("/api/admin/knowledge/seed", { method: "POST" });
  alert(`Knowledge seed result: ${JSON.stringify(result)}`);
});

callsEl.addEventListener("click", async (event) => {
  const callCard = event.target?.closest?.("[data-call-id]");
  if (callCard) {
    selectedCallId = callCard.dataset.callId;
    await loadCalls();
    await loadSelectedCall();
  }

  const callId = event.target?.dataset?.analyze;
  if (!callId) return;
  const result = await request(`/api/admin/analyze/${callId}`, { method: "POST" });
  alert(`Post-call result: ${JSON.stringify(result)}`);
  await loadCalls();
  await loadSelectedCall();
});

await loadConfig();
await loadCalls();
await loadSelectedCall();
setInterval(loadCalls, 5000);
setInterval(loadSelectedCall, 1500);
