// "Tools" tab: ToolsList with assembly items and the operations where each tool is used.

import { esc, nn, orDash } from "./format.js";
import { remainingFields } from "./components.js";

export function renderTools(container, project) {
    const tools = project.ToolsList;
    if (!Array.isArray(tools) || !tools.length) {
        container.innerHTML = `<div class="empty-state">
            <div class="big">🔧</div>
            This file has no tools list.<br>
            <span class="result-count">The ToolsList block was added in newer export versions — regenerate the JSON with an up-to-date build of the extension.</span>
        </div>`;
        return;
    }

    container.innerHTML = `<div class="cards-row">${tools.map(toolCard).join("")}</div>`;
}

function toolCard(tool) {
    const items = Array.isArray(tool.AssemblyItems) ? tool.AssemblyItems : [];
    const rows = items.map(item => {
        const entries = Object.entries(item)
            .map(([key, value]) => `<tr><td>${esc(key)}</td><td>${esc(formatValue(value))}</td></tr>`)
            .join("");
        return entries;
    }).join(`<tr><td colspan="2" style="border-bottom:2px solid var(--grid)"></td></tr>`);

    return `<div class="card">
        <h2>${esc(orDash(tool.ToolName))}</h2>
        ${remainingFields(tool, ["ToolName", "AssemblyItems", "UsedInOperations"])}
        ${usedInOperations(tool.UsedInOperations)}
        <h3>Assembly items</h3>
        ${items.length
            ? `<table class="mini">${rows}</table>`
            : `<span class="result-count">No assembly items</span>`}
    </div>`;
}

function usedInOperations(usages) {
    if (!Array.isArray(usages)) return "";
    if (!usages.length)
        return `<h3>Used in operations</h3><span class="result-count">not used</span>`;
    const rows = usages.map(usage => `<tr>
            <td title="${esc(nn(usage.OperationID) ?? "")}">${esc(orDash(usage.OperationCaption))}</td>
        </tr>`).join("");
    return `<h3>Used in operations</h3><table class="mini">${rows}</table>`;
}

function formatValue(value) {
    if (value === null || value === undefined) return "—";
    if (typeof value === "object") return JSON.stringify(value);
    return String(value);
}
