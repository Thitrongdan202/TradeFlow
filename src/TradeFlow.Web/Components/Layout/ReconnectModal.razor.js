// Set up event handlers
const reconnectModal = document.getElementById("components-reconnect-modal");
if (reconnectModal) {
    reconnectModal.addEventListener("components-reconnect-state-changed", handleReconnectStateChanged);
}

const retryButton = document.getElementById("components-reconnect-button");
if (retryButton) {
    retryButton.addEventListener("click", retry);
}

const resumeButton = document.getElementById("components-resume-button");
if (resumeButton) {
    resumeButton.addEventListener("click", resume);
}

function handleReconnectStateChanged(event) {
    if (!reconnectModal) return;

    if (event.detail.state === "show") {
        try { reconnectModal.showModal(); } catch (e) { }
    } else if (event.detail.state === "hide") {
        try { reconnectModal.close(); } catch (e) { }
        document.removeEventListener("visibilitychange", retryWhenDocumentBecomesVisible);
    } else if (event.detail.state === "failed") {
        document.addEventListener("visibilitychange", retryWhenDocumentBecomesVisible);
    } else if (event.detail.state === "rejected") {
        location.reload();
    }
}

async function retry() {
    document.removeEventListener("visibilitychange", retryWhenDocumentBecomesVisible);

    try {
        if (!window.Blazor || !window.Blazor.reconnect) {
            location.reload();
            return;
        }

        const successful = await window.Blazor.reconnect();
        if (!successful) {
            // Circuit expired or discarded by server; reload to establish fresh circuit
            const resumeSuccessful = window.Blazor.resumeCircuit ? await window.Blazor.resumeCircuit() : false;
            if (!resumeSuccessful) {
                location.reload();
            } else {
                if (reconnectModal) reconnectModal.close();
            }
        } else {
            if (reconnectModal) reconnectModal.close();
        }
    } catch (err) {
        // Network or server unreachable; register visible hook
        document.addEventListener("visibilitychange", retryWhenDocumentBecomesVisible);
    }
}

async function resume() {
    try {
        const successful = window.Blazor.resumeCircuit ? await window.Blazor.resumeCircuit() : false;
        if (!successful) {
            location.reload();
        } else {
            if (reconnectModal) reconnectModal.close();
        }
    } catch {
        if (reconnectModal) {
            reconnectModal.classList.replace("components-reconnect-paused", "components-reconnect-resume-failed");
        }
    }
}

async function retryWhenDocumentBecomesVisible() {
    if (document.visibilityState === "visible") {
        await retry();
    }
}
