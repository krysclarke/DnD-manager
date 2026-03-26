"use strict";

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/encounter-hub")
    .withAutomaticReconnect([0, 1000, 2000, 5000, 10000, 30000])
    .build();

let currentState = null;

// --- DOM helpers ---

function $(sel) { return document.querySelector(sel); }

function setConnectionStatus(status) {
    const dot = $("#status-dot");
    const text = $("#status-text");
    dot.className = "dot " + status;
    const labels = { connected: "Connected", disconnected: "Disconnected", reconnecting: "Reconnecting..." };
    text.textContent = labels[status] || status;
}

function applyTheme(theme) {
    if (!theme) return;
    const root = document.documentElement;
    root.style.setProperty("--surface", theme.surface);
    root.style.setProperty("--accent", theme.accent);
    root.style.setProperty("--accent-fg", theme.accentForeground);
    root.style.setProperty("--muted", theme.mutedText);
    root.style.setProperty("--active-highlight", theme.activeHighlight);
    root.style.setProperty("--dialog-bg", theme.dialogBg);
    root.style.setProperty("--overlay-bg", theme.overlayBg);
    root.style.setProperty("--hp-green", theme.hpGreen);
    root.style.setProperty("--hp-yellow", theme.hpYellow);
    root.style.setProperty("--hp-red", theme.hpRed);

    // Derive text color from theme darkness
    root.style.setProperty("--text", theme.isDark ? "#E0E0E0" : "#1A1A1A");
    root.style.setProperty("--border", theme.isDark ? "#444444" : "#CCCCCC");
}

function applyScale(scale) {
    document.documentElement.style.setProperty("--ui-scale", scale || 1);
}

function renderCharacters(characters, isEncounterActive, activeIndex) {
    const list = $("#character-list");
    list.innerHTML = "";

    if (!isEncounterActive && characters) {
        characters = characters.filter(function(c) { return c.isPc; });
    }

    if (!characters || characters.length === 0) {
        var msg = isEncounterActive ? "No characters in encounter." : "No players loaded.";
        list.innerHTML = '<div class="status-message"><p>' + msg + '</p></div>';
        return;
    }

    characters.forEach(function(char, i) {
        var card = document.createElement("div");
        card.className = "character-card " + (char.isPc ? "pc" : "npc");
        if (isEncounterActive && i === activeIndex) {
            card.className += " active";
        }

        var html = "";

        // Turn indicator
        if (isEncounterActive && i === activeIndex) {
            html += '<span class="turn-indicator">\u25B6</span>';
        }

        // Initiative badge
        if (isEncounterActive && char.initiative != null) {
            html += '<div class="initiative-badge">' + char.initiative + '</div>';
        } else if (isEncounterActive) {
            html += '<div class="initiative-badge no-init">--</div>';
        }

        // Character info
        html += '<div class="character-info">';
        html += '<span class="character-name">' + escapeHtml(char.displayName) + '</span>';

        // Conditions (NPC only)
        if (!char.isPc && char.conditions) {
            html += '<div class="conditions">';
            char.conditions.split(",").forEach(function(cond) {
                cond = cond.trim();
                if (cond) {
                    html += '<span class="condition-tag">' + escapeHtml(cond) + '</span>';
                }
            });
            html += '</div>';
        }

        html += '</div>';

        // Health bar (NPC only)
        if (!char.isPc && char.hpPercent != null) {
            var pct = Math.max(0, Math.min(100, Math.round(char.hpPercent * 100)));
            var cat = char.hpCategory || "green";
            html += '<div class="health-bar-container">';
            html += '<div class="health-bar-track">';
            html += '<div class="health-bar-fill ' + cat + '" style="width:' + pct + '%"></div>';
            html += '</div>';
            html += '</div>';
        }

        card.innerHTML = html;
        list.appendChild(card);
    });
}

function updateRoundInfo(isActive, round) {
    var info = $("#round-info");
    if (isActive && round > 0) {
        info.classList.remove("hidden");
        $("#round-number").textContent = round;
    } else {
        info.classList.add("hidden");
    }
}

function renderFullState(state) {
    currentState = state;
    applyTheme(state.theme);
    applyScale(state.uiScale);
    updateRoundInfo(state.isEncounterActive, state.roundNumber);
    renderCharacters(state.characters, state.isEncounterActive, state.activeCharacterIndex);
}

function escapeHtml(str) {
    var div = document.createElement("div");
    div.appendChild(document.createTextNode(str));
    return div.innerHTML;
}

// --- SignalR event handlers ---

connection.on("ReceiveFullState", function(state) {
    renderFullState(state);
});

connection.on("EncounterStarted", function(state) {
    renderFullState(state);
});

connection.on("EncounterStopped", function(state) {
    renderFullState(state);
});

connection.on("TurnAdvanced", function(activeIndex, round) {
    if (!currentState) return;
    currentState.activeCharacterIndex = activeIndex;
    currentState.roundNumber = round;
    updateRoundInfo(currentState.isEncounterActive, round);
    renderCharacters(currentState.characters, currentState.isEncounterActive, activeIndex);
});

connection.on("CharacterUpdated", function(character, index) {
    if (!currentState || !currentState.characters) return;
    if (index >= 0 && index < currentState.characters.length) {
        currentState.characters[index] = character;
        renderCharacters(currentState.characters, currentState.isEncounterActive, currentState.activeCharacterIndex);
    }
});

connection.on("CharacterAdded", function(character, index) {
    if (!currentState) return;
    if (!currentState.characters) currentState.characters = [];
    currentState.characters.splice(index, 0, character);
    renderCharacters(currentState.characters, currentState.isEncounterActive, currentState.activeCharacterIndex);
});

connection.on("CharacterRemoved", function(index) {
    if (!currentState || !currentState.characters) return;
    currentState.characters.splice(index, 1);
    renderCharacters(currentState.characters, currentState.isEncounterActive, currentState.activeCharacterIndex);
});

connection.on("ThemeChanged", function(theme) {
    applyTheme(theme);
    if (currentState) currentState.theme = theme;
});

connection.on("ScaleChanged", function(scale) {
    applyScale(scale);
    if (currentState) currentState.uiScale = scale;
});

// --- Connection lifecycle ---

connection.onreconnecting(function() {
    setConnectionStatus("reconnecting");
});

connection.onreconnected(function() {
    setConnectionStatus("connected");
});

connection.onclose(function() {
    setConnectionStatus("disconnected");
    // Try manual reconnect after a delay
    setTimeout(startConnection, 5000);
});

function startConnection() {
    connection.start()
        .then(function() {
            setConnectionStatus("connected");
        })
        .catch(function() {
            setConnectionStatus("disconnected");
            setTimeout(startConnection, 5000);
        });
}

startConnection();
