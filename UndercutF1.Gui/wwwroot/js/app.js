const ICONS = {
  "Timing Tower": '<svg viewBox="0 0 24 24" fill="none"><path d="M4 6h16M4 12h16M4 18h10" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"/></svg>',
  "Timing History": '<svg viewBox="0 0 24 24" fill="none"><path d="M12 7v5l3 3" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"/><circle cx="12" cy="12" r="8" stroke="currentColor" stroke-width="1.8"/></svg>',
  "Race Control": '<svg viewBox="0 0 24 24" fill="none"><path d="M4 21V4l14 4-14 4" stroke="currentColor" stroke-width="1.8" stroke-linejoin="round"/></svg>',
  "Driver Tracker": '<svg viewBox="0 0 24 24" fill="none"><circle cx="12" cy="12" r="8" stroke="currentColor" stroke-width="1.8"/><circle cx="12" cy="12" r="2.4" fill="currentColor"/></svg>',
  "Tyre Stints": '<svg viewBox="0 0 24 24" fill="none"><circle cx="12" cy="12" r="7.5" stroke="currentColor" stroke-width="1.8"/><circle cx="12" cy="12" r="2.6" stroke="currentColor" stroke-width="1.8"/></svg>',
  "Team Radio": '<svg viewBox="0 0 24 24" fill="none"><path d="M12 15a3 3 0 0 0 3-3V7a3 3 0 1 0-6 0v5a3 3 0 0 0 3 3Z" stroke="currentColor" stroke-width="1.8"/><path d="M6 11a6 6 0 0 0 12 0M12 19v2" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"/></svg>',
  Championship: '<svg viewBox="0 0 24 24" fill="none"><path d="M6 4h12v4a6 6 0 0 1-12 0V4Z" stroke="currentColor" stroke-width="1.8"/><path d="M9 20h6M12 14v6" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"/></svg>',
  Logs: '<svg viewBox="0 0 24 24" fill="none"><path d="M6 4h9l3 3v13H6V4Z" stroke="currentColor" stroke-width="1.8" stroke-linejoin="round"/><path d="M9 12h6M9 16h6" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"/></svg>',
  Info: '<svg viewBox="0 0 24 24" fill="none"><circle cx="12" cy="12" r="8" stroke="currentColor" stroke-width="1.8"/><path d="M12 11v5M12 8v.01" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"/></svg>',
  Account: '<svg viewBox="0 0 24 24" fill="none"><circle cx="12" cy="9" r="3" stroke="currentColor" stroke-width="1.8"/><path d="M5 20a7 7 0 0 1 14 0" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"/></svg>',
};

const NAV_MAIN = [
  "Timing Tower",
  "Timing History",
  "Race Control",
  "Driver Tracker",
  "Tyre Stints",
  "Team Radio",
  "Championship",
];
const NAV_TOOLS = ["Logs", "Info", "Account"];

const COLUMNS = [
  "Pos",
  "Driver",
  "Gap",
  "Interval",
  "Best",
  "Last Lap",
  "S1",
  "S2",
  "S3",
  "Pit",
  "Tyre",
  "Compare",
];

const SCREEN_IDS = {
  "Timing Tower": "timingtower",
  "Timing History": "timinghistory",
  "Race Control": "racecontrol",
  "Driver Tracker": "drivertracker",
  "Tyre Stints": "tyrestints",
  "Team Radio": "teamradio",
  Championship: "championship",
  Logs: "logs",
  Info: "info",
  Account: "account",
};

let activeScreen = "Timing Tower";
let historyLap = null;

function renderNav() {
  const mainEl = document.getElementById("nav-main");
  const toolsEl = document.getElementById("nav-tools");

  mainEl.innerHTML = NAV_MAIN.map(
    (label) => `
    <button class="nav-item${label === activeScreen ? " active" : ""}" data-screen="${label}">
      ${ICONS[label] ?? ""}<span>${label}</span>
    </button>`
  ).join("");

  toolsEl.innerHTML = NAV_TOOLS.map(
    (label) => `
    <button class="nav-item${label === activeScreen ? " active" : ""}" data-screen="${label}">
      ${ICONS[label] ?? ""}<span>${label}</span>
    </button>`
  ).join("");

  document.querySelectorAll(".nav-item").forEach((el) => {
    el.addEventListener("click", () => setScreen(el.dataset.screen));
  });
}

function setScreen(screen) {
  activeScreen = screen;
  renderNav();

  for (const [label, id] of Object.entries(SCREEN_IDS)) {
    document.getElementById(`screen-${id}`).hidden = label !== screen;
  }
  document.getElementById("screen-title").textContent = screen;

  // Clicking a nav item can trigger the browser's own scrollIntoView on the
  // (overflow:hidden) app shell; force it back so the freshly-shown screen isn't clipped.
  document.querySelector(".app").scrollTop = 0;

  refreshActiveScreen();
}

function refreshActiveScreen() {
  switch (activeScreen) {
    case "Race Control":
      return refreshRaceControlPage();
    case "Tyre Stints":
      return refreshTyreStintsPage();
    case "Team Radio":
      return refreshTeamRadioPage();
    case "Championship":
      return refreshChampionshipPage();
    case "Driver Tracker":
      return refreshDriverTrackerPage();
    case "Timing History":
      return refreshTimingHistoryPage();
    case "Logs":
      return refreshLogsPage();
    case "Info":
      return refreshInfoPage();
    case "Account":
      return refreshAccountPage();
    default:
      return Promise.resolve();
  }
}

function renderGridHeader() {
  document.getElementById("grid-header").innerHTML = COLUMNS.map((c) => `<div>${c}</div>`).join(
    ""
  );
}

function fmtTyre(compound) {
  if (!compound) return "unknown";
  return compound.toLowerCase();
}

function sectorCell(sector) {
  if (sector.segments) {
    const segs = [...sector.segments]
      .map((c) => `<div class="seg ${c === "_" ? "" : c}"></div>`)
      .join("");
    return `<div class="sector-segments">${segs}</div>`;
  }
  const cls = sector.style === "best" ? "best" : sector.style === "pb" ? "pb" : "normal";
  return `<div class="badge small ${cls}">${sector.value || ""}</div>`;
}

function renderRows(drivers) {
  const rowsEl = document.getElementById("grid-rows");
  const emptyEl = document.getElementById("empty-state");

  if (!drivers || drivers.length === 0) {
    rowsEl.innerHTML = "";
    emptyEl.hidden = false;
    return;
  }
  emptyEl.hidden = true;

  rowsEl.innerHTML = drivers
    .map((d, i) => {
      const posChange =
        d.positionChange > 0
          ? '<span class="pos-change pos-down">▼</span>'
          : d.positionChange < 0
            ? '<span class="pos-change pos-up">▲</span>'
            : "";

      const rowClasses = ["grid-row"];
      if (i % 2 === 1) rowClasses.push("alt");
      if (d.retired) rowClasses.push("retired");
      if (d.stopped) rowClasses.push("stopped");

      const intervalCell = d.offTrack
        ? `<div class="cell-mono off-track">${d.interval}</div>`
        : `<div class="cell-mono ${d.intervalStyle === "fast" ? "fast" : ""}">${d.interval}</div>`;

      const bestCell = `<div class="cell-mono">${d.best}</div>`;
      const lastCell = `<div class="badge ${d.lastStyle}">${d.last}</div>`;

      const pitCell = `<div class="pit-cell ${d.pitStyle}">${d.pitStatus}</div>`;

      const tyreCell = d.tyreLetter
        ? `<div class="tyre-cell">
             <div class="tyre-chip ${fmtTyre(d.tyreCompound)}">${d.tyreLetter}</div>
             <div class="tyre-age">${d.tyreAge ?? ""}</div>
           </div>`
        : `<div class="tyre-cell"></div>`;

      return `<div class="${rowClasses.join(" ")}">
        <div class="cell-pos">${d.pos}</div>
        <div class="cell-driver">
          <div class="team-bar" style="background:#${d.teamColor}"></div>
          <div class="driver-num">${d.number}</div>
          <div class="driver-code" style="color:#${d.teamColor}">${d.code}</div>
          ${posChange}
        </div>
        <div class="cell-mono">${d.gap}</div>
        ${intervalCell}
        ${bestCell}
        ${lastCell}
        ${sectorCell(d.s1)}
        ${sectorCell(d.s2)}
        ${sectorCell(d.s3)}
        ${pitCell}
        ${tyreCell}
        <div class="cell-mono">${d.compare}</div>
      </div>`;
    })
    .join("");
}

function trackStatusClass(status) {
  switch (status) {
    case "1":
      return { cls: "status-clear", dot: "dot-green", label: "ALL CLEAR" };
    case "2":
    case "4":
    case "6":
    case "7":
      return { cls: "status-yellow", dot: "dot-yellow", label: "CAUTION" };
    case "5":
      return { cls: "status-red", dot: "dot-red", label: "RED FLAG" };
    default:
      return { cls: "", dot: "dot-off", label: "NO SESSION" };
  }
}

function renderRaceControl(messages) {
  const el = document.getElementById("rc-list");
  if (!messages || messages.length === 0) {
    el.innerHTML = '<div class="rc-empty">No messages yet</div>';
    return;
  }
  el.innerHTML = messages
    .map(
      (m) => `<div class="rc-item">
        <div class="rc-dot"></div>
        <div class="rc-text"><span class="rc-time mono">${m.utc}</span>${m.message}</div>
      </div>`
    )
    .join("");
}

async function pollSnapshot() {
  try {
    const res = await fetch("/api/snapshot");
    if (!res.ok) return;
    const snap = await res.json();
    applySnapshot(snap);
  } catch {
    // Server temporarily unreachable; try again next tick.
  }
}

function applySnapshot(snap) {
  const meta = snap.sessionRunning
    ? `${snap.location ?? ""} · ${snap.sessionType ?? snap.sessionName ?? ""}${
        snap.isRace && snap.currentLap ? ` · Lap ${snap.currentLap}/${snap.totalLaps ?? "?"}` : ""
      }`
    : "No session running";
  document.getElementById("session-meta").textContent = meta;

  const track = trackStatusClass(snap.sessionRunning ? snap.trackStatus : null);
  for (const chipId of ["track-status-chip", "rail-track-chip"]) {
    const chip = document.getElementById(chipId);
    chip.className = `status-chip ${track.cls}`;
    chip.querySelector(".dot").className = `dot ${track.dot}`;
  }
  document.getElementById("track-status-text").textContent = snap.sessionRunning
    ? (snap.trackStatusMessage || track.label).toUpperCase()
    : "NO SESSION";
  document.getElementById("rail-track-text").textContent = snap.sessionRunning
    ? (snap.trackStatusMessage || track.label).toUpperCase()
    : "—";

  document.getElementById("local-time").textContent = snap.localTime || "—";
  document.getElementById("session-remaining").textContent = snap.sessionRunning
    ? snap.sessionRemaining
    : "—";

  const pillDot = document.querySelector("#session-pill .dot");
  pillDot.className = `dot ${snap.sessionRunning ? "dot-green" : "dot-off"}`;
  document.getElementById("session-pill-text").textContent = snap.sessionRunning
    ? snap.sessionName
    : "No session running";

  const clockBtn = document.getElementById("clock-toggle-btn");
  clockBtn.disabled = !snap.sessionRunning;
  document.getElementById("clock-toggle-label").textContent = snap.clockPaused
    ? "Resume"
    : "Pause";

  renderRows(snap.drivers);
  renderRaceControl(snap.raceControl);

  if (snap.sessionRunning && snap.currentLap && historyLap === null) {
    historyLap = snap.currentLap;
  }
  if (!snap.sessionRunning) {
    historyLap = null;
  }
}

// ---- Session modal ----

function openSessionModal() {
  document.getElementById("session-modal-backdrop").hidden = false;
  document.getElementById("session-error").hidden = true;
  loadReplayList();
}

function closeSessionModal() {
  document.getElementById("session-modal-backdrop").hidden = true;
}

function showSessionError(message) {
  const el = document.getElementById("session-error");
  el.textContent = message;
  el.hidden = false;
}

async function loadReplayList() {
  const el = document.getElementById("replay-list");
  el.innerHTML = '<div class="replay-empty">Loading recordings…</div>';
  try {
    const res = await fetch("/api/sessions");
    const entries = await res.json();
    if (!entries.length) {
      el.innerHTML = '<div class="replay-empty">No recordings found in the data directory yet.</div>';
      return;
    }

    const groups = new Map();
    for (const e of entries) {
      const key = `${e.date} · ${e.location}`;
      if (!groups.has(key)) groups.set(key, []);
      groups.get(key).push(e);
    }

    let html = "";
    for (const [groupLabel, items] of groups) {
      html += `<div class="replay-group-label">${groupLabel}</div>`;
      html += items
        .map(
          (item) => `<div class="replay-item" data-directory="${encodeURIComponent(item.directory)}">
            <div class="replay-item-name">${item.session}</div>
            <div class="replay-item-go">Replay →</div>
          </div>`
        )
        .join("");
    }
    el.innerHTML = html;

    el.querySelectorAll(".replay-item").forEach((item) => {
      item.addEventListener("click", () =>
        startReplay(decodeURIComponent(item.dataset.directory))
      );
    });
  } catch {
    el.innerHTML = '<div class="replay-empty">Failed to load recordings.</div>';
  }
}

async function startLiveSession() {
  const btn = document.getElementById("start-live-btn");
  btn.disabled = true;
  btn.textContent = "Starting…";
  try {
    const res = await fetch("/api/sessions/live/start", { method: "POST" });
    if (!res.ok) {
      const err = await res.json().catch(() => ({}));
      showSessionError(err.detail || err.message || "Failed to start live session");
      return;
    }
    closeSessionModal();
  } catch {
    showSessionError("Failed to reach the server");
  } finally {
    btn.disabled = false;
    btn.textContent = "Start Live Session";
  }
}

async function startReplay(directory) {
  try {
    const res = await fetch("/api/sessions/replay/start", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ directory }),
    });
    if (!res.ok) {
      const err = await res.json().catch(() => ({}));
      showSessionError(err.message || "Failed to start replay");
      return;
    }
    closeSessionModal();
  } catch {
    showSessionError("Failed to reach the server");
  }
}

async function toggleClock() {
  await fetch("/api/control", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ operation: "ToggleClock" }),
  });
}

// ---- Race Control (full page) ----

async function refreshRaceControlPage() {
  const el = document.getElementById("rc-full-list");
  try {
    const res = await fetch("/api/race-control");
    const page = await res.json();
    if (!page.messages.length) {
      el.innerHTML = '<div class="rc-empty">No race control messages yet.</div>';
      return;
    }
    el.innerHTML = page.messages
      .map(
        (m) => `<div class="rc-full-item">
          <div class="rc-full-time">${m.utc}</div>
          <div class="rc-full-body">
            <div class="rc-full-message">${m.message}</div>
            <div class="rc-full-tags">
              ${m.category ? `<span class="rc-tag">${m.category}</span>` : ""}
              ${m.flag ? `<span class="rc-tag">${m.flag}</span>` : ""}
              ${m.scope ? `<span class="rc-tag">${m.scope}</span>` : ""}
              ${m.lap ? `<span class="rc-tag">Lap ${m.lap}</span>` : ""}
            </div>
          </div>
        </div>`
      )
      .join("");
  } catch {
    el.innerHTML = '<div class="rc-empty">Failed to load race control messages.</div>';
  }
}

// ---- Tyre Stints ----

const COMPOUND_COLORS = {
  SOFT: "#e10600",
  MEDIUM: "#e6c94a",
  HARD: "#d9d9d9",
  INTERMEDIATE: "#22c55e",
  WET: "#3b82f6",
};

function renderStintLegend() {
  const el = document.getElementById("stint-legend");
  el.innerHTML = Object.entries(COMPOUND_COLORS)
    .map(
      ([name, color]) =>
        `<div class="legend-item"><div class="legend-swatch" style="background:${color}"></div>${name}</div>`
    )
    .join("");
}

async function refreshTyreStintsPage() {
  renderStintLegend();
  const el = document.getElementById("stint-list");
  try {
    const res = await fetch("/api/tyre-stints");
    const page = await res.json();
    if (!page.sessionRunning || !page.drivers.length) {
      el.innerHTML = '<div class="rc-empty">No session running.</div>';
      return;
    }
    const totalLaps = page.totalLaps || Math.max(...page.drivers.flatMap((d) => d.stints.map((s) => s.pitLap + s.length)), 1);

    el.innerHTML = page.drivers
      .map((d) => {
        const bars = d.stints
          .map((s) => {
            const widthPct = (s.length / totalLaps) * 100;
            const color = COMPOUND_COLORS[s.compound] || "#2a2d33";
            const textColor = s.compound === "MEDIUM" || s.compound === "HARD" ? "#1a1d23" : "white";
            return `<div class="stint-bar" style="flex:${widthPct} 0 0; background:${color}; color:${textColor};" title="${s.compound} · Lap ${s.pitLap} · ${s.totalLaps} laps${s.bestLapTime ? " · Best " + s.bestLapTime : ""}">${s.letter}${s.isNew ? "" : "·"}</div>`;
          })
          .join("");
        return `<div class="stint-row">
          <div class="stint-row-label"><span style="color:#${d.teamColor}">${d.code}</span></div>
          <div class="stint-row-bars">${bars}</div>
        </div>`;
      })
      .join("");
  } catch {
    el.innerHTML = '<div class="rc-empty">Failed to load tyre stint data.</div>';
  }
}

// ---- Team Radio ----

let selectedRadioKey = null;

async function refreshTeamRadioPage() {
  const el = document.getElementById("radio-list");
  try {
    const res = await fetch("/api/team-radio");
    const captures = await res.json();
    if (!captures.length) {
      el.innerHTML = '<div class="rc-empty">No team radio captures yet.</div>';
      return;
    }
    el.innerHTML = captures
      .map(
        (c) => `<div class="radio-item${c.key === selectedRadioKey ? " active" : ""}" data-key="${c.key}">
          <div class="radio-item-time mono">${c.utc}</div>
          <div class="radio-item-driver" style="color:#${c.teamColor}">${c.code}</div>
          <div class="radio-item-actions">
            <button class="chip-btn" data-action="play" data-key="${c.key}">▶ Play</button>
            <button class="chip-btn" data-action="transcribe" data-key="${c.key}">Transcribe</button>
          </div>
        </div>`
      )
      .join("");

    el.querySelectorAll("[data-action='play']").forEach((btn) =>
      btn.addEventListener("click", (e) => {
        e.stopPropagation();
        playRadio(btn.dataset.key, captures);
      })
    );
    el.querySelectorAll("[data-action='transcribe']").forEach((btn) =>
      btn.addEventListener("click", (e) => {
        e.stopPropagation();
        transcribeRadio(btn.dataset.key);
      })
    );
  } catch {
    el.innerHTML = '<div class="rc-empty">Failed to load team radio captures.</div>';
  }
}

function playRadio(key, captures) {
  selectedRadioKey = key;
  const player = document.getElementById("radio-player");
  player.src = `/api/team-radio/${key}/audio`;
  player.hidden = false;
  player.play().catch(() => {});

  const capture = captures?.find((c) => c.key === key);
  document.getElementById("radio-transcription").textContent =
    capture?.transcription || "No transcription loaded. Click Transcribe to generate one.";
  refreshTeamRadioPage();
}

async function transcribeRadio(key) {
  const transcriptionEl = document.getElementById("radio-transcription");
  selectedRadioKey = key;
  transcriptionEl.textContent = "Transcribing… this can take a while the first time (downloading the model).";
  try {
    const res = await fetch(`/api/team-radio/${key}/transcribe`, { method: "POST" });
    const body = await res.json().catch(() => ({}));
    if (!res.ok) {
      transcriptionEl.textContent = body.detail || "Failed to transcribe.";
      return;
    }
    transcriptionEl.textContent = body.transcription || "No speech detected.";
    refreshTeamRadioPage();
  } catch {
    transcriptionEl.textContent = "Failed to reach the server.";
  }
}

// ---- Championship ----

async function refreshChampionshipPage() {
  const el = document.getElementById("championship-content");
  try {
    const res = await fetch("/api/championship");
    const page = await res.json();

    if (!page.available) {
      el.innerHTML = `<div class="info-message">${page.reason}</div>`;
      return;
    }

    const driverRows = page.drivers
      .map(
        (d) => `<tr>
          <td class="mono">${d.position}</td>
          <td style="color:#${d.teamColor}; font-weight:700;">${d.code}</td>
          <td class="mono">+${d.change.toFixed(0)}</td>
          <td class="mono">${d.predictedPoints.toFixed(0)}</td>
          <td class="mono">${d.gapToNext.toFixed(0)}</td>
        </tr>`
      )
      .join("");

    const teamRows = page.teams
      .map(
        (t) => `<tr>
          <td class="mono">${t.position}</td>
          <td style="color:#${t.teamColor}; font-weight:700;">${t.teamName}</td>
          <td class="mono">+${t.change.toFixed(0)}</td>
          <td class="mono">${t.predictedPoints.toFixed(0)}</td>
          <td class="mono">${t.gapToNext.toFixed(0)}</td>
        </tr>`
      )
      .join("");

    const speedTraps = page.speedTraps
      .map(
        (trap) => `<div class="speed-trap-card">
          <div class="speed-trap-title">${trap.trap}</div>
          ${trap.entries
            .map(
              (e) =>
                `<div class="speed-trap-row"><span style="color:#${e.teamColor}">${e.code}</span><span class="mono">${e.speed}</span></div>`
            )
            .join("")}
        </div>`
      )
      .join("");

    el.innerHTML = `
      <div class="champ-columns">
        <div style="flex:1; min-width:280px;">
          <div class="card-title">Drivers Championship</div>
          <table class="champ-table"><thead><tr><th>Pos</th><th>Driver</th><th>Chg</th><th>Points</th><th>Gap</th></tr></thead>
          <tbody>${driverRows}</tbody></table>
        </div>
        <div style="flex:1; min-width:280px;">
          <div class="card-title">Constructors Championship</div>
          <table class="champ-table"><thead><tr><th>Pos</th><th>Team</th><th>Chg</th><th>Points</th><th>Gap</th></tr></thead>
          <tbody>${teamRows}</tbody></table>
        </div>
      </div>
      <div class="speed-trap-grid">${speedTraps}</div>
    `;
  } catch {
    el.innerHTML = '<div class="info-message">Failed to load championship data.</div>';
  }
}

// ---- Driver Tracker ----

async function refreshDriverTrackerPage() {
  const listEl = document.getElementById("tracker-list");
  const svgEl = document.getElementById("tracker-svg");
  const messageEl = document.getElementById("tracker-message");

  try {
    const res = await fetch("/api/driver-tracker");
    const page = await res.json();

    listEl.innerHTML = page.drivers
      .map(
        (d) => `<div class="tracker-list-row">
          <span style="color:#${d.teamColor}; font-weight:700;">${d.code}</span>
          <span class="mono">${d.offTrack ? "OFF TRK" : d.gap}</span>
        </div>`
      )
      .join("");

    if (!page.hasCircuit || !page.hasPositions) {
      messageEl.hidden = false;
      messageEl.textContent = page.reason;
      svgEl.innerHTML = "";
      return;
    }
    messageEl.hidden = true;

    // Raw circuit coordinates span thousands of units, so size strokes/dots/text
    // as a proportion of the track's own scale rather than fixed pixel values.
    const scale = Math.max(page.viewWidth, page.viewHeight);
    const pad = scale * 0.04;
    const trackWidth = scale * 0.006;
    const dotRadius = scale * 0.012;
    const fontSize = scale * 0.016;

    svgEl.setAttribute(
      "viewBox",
      `${-pad} ${-pad} ${page.viewWidth + pad * 2} ${page.viewHeight + pad * 2}`
    );

    const trackPath = page.trackPoints.map((p) => `${p[0]},${p[1]}`).join(" ");
    const corners = page.corners
      .map(
        (c) =>
          `<text x="${c.x + fontSize * 0.6}" y="${c.y}" class="tracker-corner">${c.number}</text>`
      )
      .join("");
    const dots = page.drivers
      .map(
        (d) =>
          `<g>
            <circle cx="${d.x}" cy="${d.y}" r="${dotRadius}" fill="#${d.teamColor}" stroke="#0a0b0d" stroke-width="${dotRadius * 0.2}" />
            <text x="${d.x + dotRadius * 1.4}" y="${d.y + fontSize * 0.35}" class="tracker-label" fill="#${d.teamColor}">${d.code}</text>
          </g>`
      )
      .join("");

    svgEl.innerHTML = `
      <style>.tracker-corner{font-size:${fontSize}px;fill:#888;font-family:'JetBrains Mono',monospace;} .tracker-label{font-size:${fontSize}px;font-weight:700;font-family:'JetBrains Mono',monospace;}</style>
      <polyline points="${trackPath}" fill="none" stroke="#4b5563" stroke-width="${trackWidth}" stroke-linejoin="round" stroke-linecap="round" />
      ${corners}
      ${dots}
    `;
  } catch {
    messageEl.hidden = false;
    messageEl.textContent = "Failed to load driver tracker data.";
  }
}

// ---- Timing History ----

function stepHistoryLap(delta) {
  if (historyLap === null) return;
  historyLap = Math.max(1, historyLap + delta);
  refreshTimingHistoryPage();
}

const HISTORY_COLUMNS = ["Driver", "Gap", "Interval", "Last Lap", "S1", "S2", "S3"];

function renderHistoryHeader() {
  document.getElementById("history-header").innerHTML = HISTORY_COLUMNS.map(
    (c) => `<div>${c}</div>`
  ).join("");
}

async function refreshTimingHistoryPage() {
  renderHistoryHeader();
  const lapLabel = document.getElementById("history-lap-label");
  const rowsEl = document.getElementById("history-rows");
  const chartsEl = document.getElementById("history-charts");

  if (historyLap === null) {
    lapLabel.textContent = "No session";
    rowsEl.innerHTML = "";
    chartsEl.innerHTML = "";
    return;
  }

  lapLabel.textContent = `Lap ${historyLap}`;

  try {
    const res = await fetch(`/api/timing-history/${historyLap}`);
    const page = await res.json();

    if (!page.rows.length) {
      rowsEl.innerHTML = `<div class="rc-empty">No data for lap ${historyLap} yet — it hasn't finished.</div>`;
      chartsEl.innerHTML = "";
      return;
    }

    rowsEl.innerHTML = page.rows
      .map(
        (r) => `<div class="history-row">
          <div style="color:#${r.teamColor}; font-weight:700;">${r.code}</div>
          <div class="mono">${r.gapToLeader}<span class="delta">${r.gapDelta}</span></div>
          <div class="mono">${r.interval}<span class="delta">${r.intervalDelta}</span></div>
          <div class="badge ${r.lastLapStyle}">${r.lastLap}</div>
          <div class="badge small ${r.s1.style}">${r.s1.value}</div>
          <div class="badge small ${r.s2.style}">${r.s2.value}</div>
          <div class="badge small ${r.s3.style}">${r.s3.value}</div>
        </div>`
      )
      .join("");

    const chartRes = await fetch(`/api/timing-history/${historyLap}/chart`);
    const chart = await chartRes.json();

    let chartsHtml = "";
    if (chart.isRace) {
      chartsHtml += chartCard("Gap to Leader (s)", chart.laps, chart.gapSeries, (v) => v.toFixed(1));
    }
    chartsHtml += chartCard("Lap Time", chart.laps, chart.lapTimeSeries, formatLapTimeMs);
    chartsHtml += chartCard(
      "Delta to Average Lap",
      chart.laps,
      chart.deltaToAverageSeries,
      (v) => (v / 1000).toFixed(2) + "s"
    );
    chartsEl.innerHTML = chartsHtml;
  } catch {
    rowsEl.innerHTML = '<div class="rc-empty">Failed to load timing history.</div>';
  }
}

function formatLapTimeMs(ms) {
  const totalSeconds = ms / 1000;
  const minutes = Math.floor(totalSeconds / 60);
  const seconds = (totalSeconds % 60).toFixed(2);
  return `${minutes}:${seconds.padStart(5, "0")}`;
}

function chartCard(title, laps, series, formatter) {
  return `<div class="chart-card">
    <div class="chart-title">${title}</div>
    ${buildLineChartSvg(laps, series, formatter)}
  </div>`;
}

function buildLineChartSvg(laps, series, formatter) {
  const width = 600;
  const height = 180;
  const padLeft = 40;
  const padRight = 50;
  const padTop = 10;
  const padBottom = 20;

  const allValues = series.flatMap((s) => s.values).filter((v) => v !== null && v !== undefined);
  if (!allValues.length || laps.length < 2) {
    return `<svg class="chart-svg" viewBox="0 0 ${width} ${height}"><text x="10" y="20" fill="#6b7280" font-size="12">Not enough data yet</text></svg>`;
  }

  const minVal = Math.min(...allValues);
  const maxVal = Math.max(...allValues);
  const valRange = maxVal - minVal || 1;

  const xFor = (i) => padLeft + (i / (laps.length - 1)) * (width - padLeft - padRight);
  const yFor = (v) => height - padBottom - ((v - minVal) / valRange) * (height - padTop - padBottom);

  const gridLines = [0, 0.5, 1]
    .map((f) => {
      const y = padTop + f * (height - padTop - padBottom);
      return `<line x1="${padLeft}" y1="${y}" x2="${width - padRight}" y2="${y}" stroke="#1e2127" stroke-width="1" />`;
    })
    .join("");

  const paths = series
    .map((s) => {
      const segments = [];
      let current = [];
      s.values.forEach((v, i) => {
        if (v === null || v === undefined) {
          if (current.length > 1) segments.push(current);
          current = [];
          return;
        }
        current.push(`${xFor(i)},${yFor(v)}`);
      });
      if (current.length > 1) segments.push(current);

      const lines = segments
        .map(
          (seg) =>
            `<polyline points="${seg.join(" ")}" fill="none" stroke="#${s.teamColor}" stroke-width="2" />`
        )
        .join("");

      // Label at the last non-null point
      let lastIdx = -1;
      for (let i = s.values.length - 1; i >= 0; i--) {
        if (s.values[i] !== null && s.values[i] !== undefined) {
          lastIdx = i;
          break;
        }
      }
      const label =
        lastIdx >= 0
          ? `<text x="${xFor(lastIdx) + 6}" y="${yFor(s.values[lastIdx]) + 4}" fill="#${s.teamColor}" font-size="11" font-weight="700">${s.code}</text>`
          : "";

      return lines + label;
    })
    .join("");

  return `<svg class="chart-svg" viewBox="0 0 ${width} ${height}">
    ${gridLines}
    <text x="4" y="${padTop + 8}" fill="#6b7280" font-size="10">${formatter(maxVal)}</text>
    <text x="4" y="${height - padBottom}" fill="#6b7280" font-size="10">${formatter(minVal)}</text>
    ${paths}
  </svg>`;
}

// ---- Logs ----

async function refreshLogsPage() {
  const level = document.getElementById("logs-min-level").value;
  const el = document.getElementById("log-list");
  try {
    const res = await fetch(`/api/logs?minLevel=${level}`);
    const logs = await res.json();
    if (!logs.length) {
      el.innerHTML = '<div class="rc-empty">No logs yet.</div>';
      return;
    }
    el.innerHTML = logs
      .map(
        (l) => `<div class="log-row">
          <div class="log-level ${l.level.toLowerCase()}">${l.level.slice(0, 3).toUpperCase()}</div>
          <div class="log-message">${l.message}${l.exception ? "\n" + l.exception : ""}</div>
        </div>`
      )
      .join("");
  } catch {
    el.innerHTML = '<div class="rc-empty">Failed to load logs.</div>';
  }
}

// ---- Info ----

async function refreshInfoPage() {
  const el = document.getElementById("info-card");
  try {
    const res = await fetch("/api/info");
    const info = await res.json();
    const rows = [
      ["Data Directory", info.dataDirectory],
      ["Log Directory", info.logDirectory],
      ["Audible Notifications", info.notify],
      ["Verbose Mode", info.verbose],
      ["F1 TV Account", info.f1TvStatus],
      ["Token Expiry", info.f1TvExpiry || "—"],
      ["Config File", `${info.configFileExists ? "Found" : "Not found"} (${info.configFilePath})`],
      ["Version", info.version],
      ["OS", info.os],
      ["GUI Port", info.port],
    ];
    el.innerHTML =
      `<div class="card-title">Configuration</div>` +
      rows
        .map(
          ([label, value]) =>
            `<div class="info-row"><div class="stat-label">${label}</div><div>${value}</div></div>`
        )
        .join("");
  } catch {
    el.innerHTML = '<div class="info-message">Failed to load info.</div>';
  }
}

// ---- Account ----

async function refreshAccountPage() {
  const el = document.getElementById("account-status");
  try {
    const res = await fetch("/api/account");
    const account = await res.json();
    el.innerHTML = `
      <div class="info-row"><div class="stat-label">Status</div><div>${account.status}</div></div>
      ${account.subscriptionStatus ? `<div class="info-row"><div class="stat-label">Subscription</div><div>${account.subscriptionStatus}</div></div>` : ""}
      ${account.expiry ? `<div class="info-row"><div class="stat-label">Expires</div><div>${account.expiry}</div></div>` : ""}
      <div class="info-row"><div class="stat-label">Config File</div><div class="mono">${account.configFilePath}</div></div>
    `;
  } catch {
    el.innerHTML = '<div class="info-message">Failed to load account status.</div>';
  }
}

async function saveAccountToken() {
  const input = document.getElementById("account-token-input");
  const errorEl = document.getElementById("account-error");
  errorEl.hidden = true;
  try {
    const res = await fetch("/api/account/token", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ token: input.value.trim() }),
    });
    const body = await res.json().catch(() => ({}));
    if (!res.ok) {
      errorEl.textContent = body.detail || body.message || "Failed to save token";
      errorEl.hidden = false;
      return;
    }
    input.value = "";
    refreshAccountPage();
  } catch {
    errorEl.textContent = "Failed to reach the server";
    errorEl.hidden = false;
  }
}

async function accountLogout() {
  await fetch("/api/account/logout", { method: "POST" });
  refreshAccountPage();
}

function init() {
  renderNav();
  renderGridHeader();
  setScreen("Timing Tower");

  document.getElementById("open-session-panel").addEventListener("click", openSessionModal);
  document.getElementById("empty-state-btn").addEventListener("click", openSessionModal);
  document.getElementById("close-session-panel").addEventListener("click", closeSessionModal);
  document.getElementById("session-modal-backdrop").addEventListener("click", (e) => {
    if (e.target.id === "session-modal-backdrop") closeSessionModal();
  });
  document.getElementById("start-live-btn").addEventListener("click", startLiveSession);
  document.getElementById("clock-toggle-btn").addEventListener("click", toggleClock);

  document.getElementById("history-prev-lap").addEventListener("click", () => stepHistoryLap(-1));
  document.getElementById("history-next-lap").addEventListener("click", () => stepHistoryLap(1));
  document.getElementById("logs-min-level").addEventListener("change", refreshLogsPage);
  document.getElementById("account-save-token").addEventListener("click", saveAccountToken);
  document.getElementById("account-logout").addEventListener("click", accountLogout);

  pollSnapshot();
  setInterval(() => {
    pollSnapshot();
    refreshActiveScreen();
  }, 500);
}

init();
