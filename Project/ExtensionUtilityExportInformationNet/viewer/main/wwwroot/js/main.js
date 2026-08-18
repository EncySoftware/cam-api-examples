// Entry point: data loading, theme, tab switching, launching the renderers.

import { esc, fmtBytes } from "./format.js";
import { renderOverview } from "./render-overview.js";
import { renderSetup } from "./render-setup.js";
import { renderOperations } from "./ops-table.js";
import { renderTools } from "./render-tools.js";
import { renderRaw } from "./render-raw.js";

const THEME_KEY = "camviewer-theme";
const HEARTBEAT_INTERVAL_MS = 5000;

initTheme();
initTabs();
startHeartbeat();
loadAndRender();

// While the tab is open the server stays alive; after closing it shuts down by idle timeout.
function startHeartbeat() {
    setInterval(() => fetch("/api/heartbeat").catch(() => {}), HEARTBEAT_INTERVAL_MS);
}

function initTheme() {
    const saved = localStorage.getItem(THEME_KEY);
    if (saved) document.documentElement.dataset.theme = saved;

    document.getElementById("theme-toggle").addEventListener("click", () => {
        const isDarkNow = document.documentElement.dataset.theme === "dark"
            || (!document.documentElement.dataset.theme
                && matchMedia("(prefers-color-scheme: dark)").matches);
        const next = isDarkNow ? "light" : "dark";
        document.documentElement.dataset.theme = next;
        localStorage.setItem(THEME_KEY, next);
    });
}

function initTabs() {
    document.getElementById("tabs").addEventListener("click", (e) => {
        const tab = e.target.closest(".tab");
        if (!tab) return;
        for (const t of document.querySelectorAll(".tab"))
            t.classList.toggle("active", t === tab);
        for (const p of document.querySelectorAll(".panel"))
            p.classList.toggle("active", p.id === `panel-${tab.dataset.tab}`);
    });
}

async function loadAndRender() {
    renderFileMeta();

    let rawText;
    try {
        const response = await fetch("/api/project");
        rawText = await response.text();
        if (!response.ok) {
            showLoadError(`Data file not found.<br><span class="mono">${esc(rawText)}</span>`);
            return;
        }
    } catch (e) {
        showLoadError(`Failed to fetch data from the server: ${esc(e.message)}`);
        return;
    }

    let root;
    try {
        root = JSON.parse(rawText);
    } catch (e) {
        showLoadError(
            `The file is corrupted or contains invalid JSON (${esc(e.message)}). ` +
            `<a href="/api/project" download="project.json">Download the file as is</a>`);
        return;
    }

    const project = root?.CAMProject;
    if (!project) {
        showLoadError("The JSON has no root <span class=\"mono\">CAMProject</span> object.");
        return;
    }

    renderHeader(project);
    renderOverview(document.getElementById("panel-overview"), project);
    renderSetup(document.getElementById("panel-setup"), project);
    renderOperations(document.getElementById("panel-operations"), project);
    renderTools(document.getElementById("panel-tools"), project);
    renderRaw(document.getElementById("panel-raw"), root);
}

function renderHeader(project) {
    const filePath = project.FilePath ?? "";
    const fileName = filePath.split(/[\\/]/).pop() || "CAM Project";
    document.getElementById("project-name").textContent = fileName;
    document.title = `${fileName} — project info`;
    const pathEl = document.getElementById("project-path");
    pathEl.textContent = filePath;
    pathEl.title = filePath;
}

async function renderFileMeta() {
    try {
        const meta = await (await fetch("/api/meta")).json();
        if (!meta.exists) return;
        const modified = meta.modified ? new Date(meta.modified).toLocaleString("en-US") : "";
        document.getElementById("file-meta").textContent =
            `${fmtBytes(meta.size)} · ${modified}`;
    } catch {
        /* metadata is not critical for display */
    }
}

function showLoadError(html) {
    const overview = document.getElementById("panel-overview");
    overview.innerHTML = `<div class="error-banner"><strong>Failed to load data.</strong><br>${html}</div>`;
}
