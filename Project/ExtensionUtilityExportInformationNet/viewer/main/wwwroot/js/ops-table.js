// "Operations" tab: operations tree/table with sorting, filters, and details.

import { esc, fmtMs, fmtNum } from "./format.js";
import { badge } from "./components.js";
import { buildOpsModel, describeErrorFlags, leafRows } from "./ops-model.js";
import { renderOperationDetail, buildToolsByOperation } from "./op-detail.js";

const COLUMNS = [
    { key: "name", label: "Operation", sortable: true, numeric: false },
    { key: "type", label: "Type", sortable: true, numeric: false },
    { key: "partIdx", label: "Part", sortable: true, numeric: true },
    { key: "stageIdx", label: "Stage", sortable: true, numeric: true },
    { key: "status", label: "Status", sortable: false, numeric: false },
    { key: "totalMs", label: "Total time", sortable: true, numeric: true },
    { key: "effectiveMs", label: "Effective", sortable: true, numeric: true },
    { key: "rapidMs", label: "Rapid", sortable: true, numeric: true },
    { key: "totalBlocks", label: "Blocks", sortable: true, numeric: true },
    { key: "workLength", label: "Work length", sortable: true, numeric: true },
    { key: "rapidLength", label: "Rapid length", sortable: true, numeric: true },
];

const state = {
    source: "Reordered",
    search: "",
    onlyErrors: false,
    hideGroups: false,
    partFilter: "all",
    sortKey: null,
    sortDir: 1,
    collapsed: new Set(),
    expandedDetails: new Set(),
};

let projectRef = null;
let containerRef = null;
let toolsByOperationRef = null;

export function renderOperations(container, project) {
    projectRef = project;
    containerRef = container;
    toolsByOperationRef = buildToolsByOperation(project.ToolsList);
    container.innerHTML = `<div class="toolbar" id="ops-toolbar"></div>
        <div class="table-scroll"><table class="data" id="ops-table"></table></div>`;
    renderToolbar();
    renderTable();
}

/* ---------- Data ---------- */

function currentRawList() {
    return state.source === "Designed"
        ? projectRef.DesignedOperationsList ?? []
        : projectRef.ReorderedOperationsList ?? [];
}

function isFlatMode() {
    return state.sortKey !== null || state.search !== ""
        || state.onlyErrors || state.partFilter !== "all" || state.hideGroups;
}

function matchesFilters(row) {
    if (state.onlyErrors && !row.hasError) return false;
    if (state.partFilter !== "all" && String(row.partIdx) !== state.partFilter) return false;
    if (state.search) {
        const needle = state.search.toLowerCase();
        const haystack = `${row.name} ${row.type} ${row.id ?? ""}`.toLowerCase();
        if (!haystack.includes(needle)) return false;
    }
    return true;
}

/** Visible rows: a flat filtered list or a collapsible tree. */
function visibleRows(rows) {
    if (isFlatMode()) {
        const leaves = leafRows(rows).filter(matchesFilters);
        if (state.sortKey) sortRows(leaves);
        return leaves.map(row => ({ row, depth: 0 }));
    }

    const roots = rows.filter(r => !r.parent);
    const result = [];
    const walk = (row, depth) => {
        result.push({ row, depth });
        if (row.isGroup && !state.collapsed.has(row.id))
            for (const child of row.children) walk(child, depth + 1);
    };
    for (const root of roots) walk(root, 0);
    return result;
}

function sortRows(rows) {
    const { sortKey, sortDir } = state;
    rows.sort((a, b) => compareValues(a[sortKey], b[sortKey]) * sortDir);
}

function compareValues(a, b) {
    const aEmpty = a === null || a === undefined;
    const bEmpty = b === null || b === undefined;
    if (aEmpty && bEmpty) return 0;
    if (aEmpty) return 1;   // empty values always go last
    if (bEmpty) return -1;
    if (typeof a === "number" && typeof b === "number") return a - b;
    return String(a).localeCompare(String(b), "en");
}

/* ---------- Toolbar ---------- */

function renderToolbar() {
    const rawList = currentRawList();
    const { rows } = buildOpsModel(rawList);
    const partIndexes = [...new Set(leafRows(rows).map(r => r.partIdx).filter(v => v !== undefined))]
        .sort((a, b) => a - b);

    const toolbar = containerRef.querySelector("#ops-toolbar");
    toolbar.innerHTML = `
        <div class="segmented" id="ops-source">
            <button data-source="Reordered" class="${state.source === "Reordered" ? "active" : ""}">By execution order</button>
            <button data-source="Designed" class="${state.source === "Designed" ? "active" : ""}">By project tree</button>
        </div>
        <input type="search" id="ops-search" placeholder="Search: name, type, GUID…" value="${esc(state.search)}">
        <select id="ops-part">
            <option value="all">All parts</option>
            ${partIndexes.map(p => `<option value="${p}" ${state.partFilter === String(p) ? "selected" : ""}>Part #${p}</option>`).join("")}
        </select>
        <label class="check"><input type="checkbox" id="ops-errors" ${state.onlyErrors ? "checked" : ""}> Errors only</label>
        <label class="check"><input type="checkbox" id="ops-nogroups" ${state.hideGroups ? "checked" : ""}> Hide groups</label>
        <span class="result-count" id="ops-count"></span>`;

    toolbar.querySelector("#ops-source").addEventListener("click", (e) => {
        const btn = e.target.closest("button[data-source]");
        if (!btn || btn.dataset.source === state.source) return;
        state.source = btn.dataset.source;
        state.expandedDetails.clear();
        state.collapsed.clear();
        renderToolbar();
        renderTable();
    });
    toolbar.querySelector("#ops-search").addEventListener("input", (e) => {
        state.search = e.target.value.trim();
        renderTable();
    });
    toolbar.querySelector("#ops-part").addEventListener("change", (e) => {
        state.partFilter = e.target.value;
        renderTable();
    });
    toolbar.querySelector("#ops-errors").addEventListener("change", (e) => {
        state.onlyErrors = e.target.checked;
        renderTable();
    });
    toolbar.querySelector("#ops-nogroups").addEventListener("change", (e) => {
        state.hideGroups = e.target.checked;
        renderTable();
    });
}

/* ---------- Table ---------- */

function renderTable() {
    const { rows } = buildOpsModel(currentRawList());
    const visible = visibleRows(rows);
    const table = containerRef.querySelector("#ops-table");

    table.innerHTML = `
        <thead><tr>${COLUMNS.map(columnHeader).join("")}</tr></thead>
        <tbody>${visible.map(entry => rowHtml(entry.row, entry.depth)).join("")}</tbody>
        <tfoot>${footerHtml(visible)}</tfoot>`;

    updateResultCount(visible);
    bindTableEvents(table, rows);
}

function columnHeader(col) {
    const arrow = state.sortKey === col.key
        ? `<span class="sort-arrow">${state.sortDir > 0 ? " ▲" : " ▼"}</span>`
        : "";
    const classes = `${col.sortable ? "sortable" : ""} ${col.numeric ? "num" : ""}`;
    return `<th class="${classes}" data-key="${col.key}" data-sortable="${col.sortable}">${esc(col.label)}${arrow}</th>`;
}

function rowHtml(row, depth) {
    const indent = depth * 18;
    const expanded = state.expandedDetails.has(row.id);
    const chevron = row.isGroup && !isFlatMode()
        ? `<span class="chevron ${state.collapsed.has(row.id) ? "" : "open"}">▶</span>`
        : `<span class="chevron leaf ${expanded ? "open" : ""}">▶</span>`;

    const mainRow = `<tr class="op-row ${row.isGroup ? "group-row" : ""} ${expanded ? "expanded" : ""}" data-id="${esc(row.id ?? "")}">
        <td><span class="op-name" style="padding-left:${indent}px" title="${esc(row.name)}">${chevron}${esc(row.name)}</span></td>
        <td title="${esc(row.type)}">${esc(shortType(row.type))}</td>
        <td class="num">${esc(String(row.partIdx ?? "—"))}</td>
        <td class="num">${esc(String(row.stageIdx ?? "—"))}</td>
        <td>${statusBadges(row)}</td>
        <td class="num">${fmtMs(row.totalMs)}</td>
        <td class="num">${fmtMs(row.effectiveMs)}</td>
        <td class="num">${fmtMs(row.rapidMs)}</td>
        <td class="num">${fmtNum(row.totalBlocks, 0)}</td>
        <td class="num">${fmtNum(row.workLength, 1)}</td>
        <td class="num">${fmtNum(row.rapidLength, 1)}</td>
    </tr>`;

    if (!expanded) return mainRow;
    return mainRow + `<tr class="detail-row"><td colspan="${COLUMNS.length}">${renderOperationDetail(row, toolsByOperationRef)}</td></tr>`;
}

function shortType(type) {
    return (type ?? "").replace(/^TST/, "").replace(/Op$/, "") || "—";
}

function statusBadges(row) {
    const badges = [];
    if (!row.enabled) badges.push(badge("muted", "Off"));
    if (row.hasError) badges.push(badge("err", "Error", describeErrorFlags(row)));
    if (!row.isGroup) {
        badges.push(row.calculated
            ? badge("ok", "Calculated")
            : badge("warn", "Not calculated"));
        if (row.simulated) badges.push(badge("info", "Sim."));
    }
    return badges.join(" ");
}

function footerHtml(visible) {
    const leaves = visible.map(v => v.row).filter(r => !r.isGroup);
    const total = (selector) => {
        const values = leaves.map(selector).filter(v => typeof v === "number" && !Number.isNaN(v));
        return values.length ? values.reduce((a, b) => a + b, 0) : null;
    };
    return `<tr>
        <td colspan="5">Total for ${leaves.length} operations</td>
        <td class="num">${fmtMs(total(r => r.totalMs))}</td>
        <td class="num">${fmtMs(total(r => r.effectiveMs))}</td>
        <td class="num">${fmtMs(total(r => r.rapidMs))}</td>
        <td class="num">${fmtNum(total(r => r.totalBlocks), 0)}</td>
        <td class="num">${fmtNum(total(r => r.workLength), 1)}</td>
        <td class="num">${fmtNum(total(r => r.rapidLength), 1)}</td>
    </tr>`;
}

function updateResultCount(visible) {
    const leaves = visible.filter(v => !v.row.isGroup).length;
    containerRef.querySelector("#ops-count").textContent =
        `Operations shown: ${leaves} · click a row for details (feeds, spindle, coolant, stocks)`;
}

function bindTableEvents(table, rows) {
    table.querySelector("thead").addEventListener("click", (e) => {
        const th = e.target.closest("th[data-sortable='true']");
        if (!th) return;
        const key = th.dataset.key;
        if (state.sortKey === key) {
            if (state.sortDir === 1) {
                state.sortDir = -1;
            } else {
                state.sortKey = null;   // the third click resets the sorting
                state.sortDir = 1;
            }
        } else {
            state.sortKey = key;
            state.sortDir = 1;
        }
        renderTable();
    });

    const byId = new Map(rows.filter(r => r.id).map(r => [r.id, r]));
    table.querySelector("tbody").addEventListener("click", (e) => {
        const tr = e.target.closest("tr.op-row");
        if (!tr) return;
        const row = byId.get(tr.dataset.id);
        if (!row) return;

        if (row.isGroup && !isFlatMode()) {
            if (state.collapsed.has(row.id)) state.collapsed.delete(row.id);
            else state.collapsed.add(row.id);
        } else {
            if (state.expandedDetails.has(row.id)) state.expandedDetails.delete(row.id);
            else state.expandedDetails.add(row.id);
        }
        renderTable();
    });
}
