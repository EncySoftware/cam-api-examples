// Operation detail panel: feeds, spindle, coolant, statistics, stocks, attributes.
// Full coverage: known keys get labels, the rest go through remainingFields.

import { esc, fmtNum, get, nn, orDash } from "./format.js";
import { kvTable, kvText, kvMono, matrixDetails, remainingFields, badge } from "./components.js";

const OPERATION_HANDLED_KEYS = [
    "ParentOperationID", "OperationID", "OperationType", "OperationName",
    "OperationPartIndex", "OperationSetupStageIndex", "WorkpieceCSList",
    "Toolpath", "Status", "Statistics", "Feeds", "Coolants", "Spindle",
    "Stocks", "CustomAttributes", "Notes",
];

const TIME_LABELS = {
    RapidTime: "Rapid moves",
    IdleWorkTime: "Idle moves",
    EffectiveWorkTime: "Effective work",
    AuxiliaryTime: "Auxiliary",
    TotalTime: "Total",
};

const BLOCK_LABELS = {
    Lines: "Lines",
    Arcs: "Arcs",
    Feeds: "Feed changes",
    TotalBlocks: "Total blocks",
};

const SPINDLE_LABELS = {
    RotationMode: "Rotation mode",
    SurfaceSpeedValue: "Cutting speed, m/min",
    MaxRPMValue: "Max RPM",
    RPMValue: "RPM",
    RotationDirection: "Direction",
    Range: "Range",
};

const STOCK_LABELS = {
    Profile: "Profile",
    Axial: "Axial",
    Radial: "Radial",
};

// Units verified against the data: rate == (removed volume / effective time) in cm³/min.
const VOLUME_LABELS = {
    MachResultVolume: "Volume after machining, mm³",
    WorkpieceVolume: "Workpiece volume, mm³",
    VolumeRemovalRate: "Removal rate, cm³/min",
};

const STATUS_LABELS = {
    Enabled: "Enabled",
    Calculated: "Calculated",
    Simulated: "Simulated",
    IsMachiningResultCalculated: "Machining result calculated",
    IsError: "Error (general flag)",
    IsRapidError: "Rapid moves error",
    IsCollisionError: "Collision",
    IsGougeError: "Gouge",
    IsHolderError: "Holder error",
    IsPlungeError: "Plunge error",
    IsToolOverloadError: "Tool overload",
    IsTravelError: "Travel limits exceeded",
    IsTurnDirectionError: "Rotation direction error",
    IsCompensationError: "Compensation error",
};

/**
 * Map "operation → tools" built from ToolsList[].UsedInOperations.
 * IDs are normalized ({}, case) to survive different GUID formats.
 */
export function buildToolsByOperation(toolsList) {
    const map = new Map();
    for (const tool of toolsList ?? []) {
        for (const usage of tool.UsedInOperations ?? []) {
            const id = normalizeId(usage.OperationID);
            if (!id) continue;
            if (!map.has(id)) map.set(id, []);
            map.get(id).push(tool);
        }
    }
    return map;
}

function normalizeId(id) {
    const value = nn(id);
    return value === null ? null : String(value).replace(/[{}]/g, "").toLowerCase();
}

/** HTML content of the detail row for an operation. */
export function renderOperationDetail(row, toolsByOperation = null) {
    const raw = row.raw;
    const sections = [
        toolSection(row, toolsByOperation),
        statusSection(raw.Status),
        feedsSection(raw.Feeds),
        spindleSection(raw.Spindle),
        coolantsSection(raw.Coolants),
        timeSection(get(raw, "Statistics.Time")),
        blocksSection(get(raw, "Statistics.Blocks")),
        lengthsSection(get(raw, "Statistics.Lengths")),
        volumesSection(get(raw, "Statistics.Volumes")),
        statisticsRestSection(raw.Statistics),
        stocksSection(raw.Stocks),
        attributesSection(raw.CustomAttributes),
        notesSection(raw.Notes),
        workpieceCSSection(raw.WorkpieceCSList),
        miscSection(row),
        restSection(raw),
    ].filter(Boolean);

    return `<div class="detail-grid">${sections.join("")}</div>`;
}

function section(title, bodyHtml) {
    return `<div class="detail-section"><h4>${esc(title)}</h4>${bodyHtml}</div>`;
}

/* ---------- Operation tool (from ToolsList[].UsedInOperations) ---------- */

function toolSection(row, toolsByOperation) {
    const tools = toolsByOperation?.get(normalizeId(row.id)) ?? [];
    if (!tools.length) return null;
    const rows = tools.map(tool => `<tr>
        <td>${esc(orDash(tool.ToolCaption ?? tool.ToolName))}</td>
        <td class="num">${esc(String(tool.ToolNumber ?? "—"))}</td>
        <td class="num">${esc(String(tool.MagazineNumber ?? "—"))}</td>
    </tr>`).join("");
    return section("Tool", `<table class="mini">
        <tr><th>Name</th><th>Tool #</th><th>Magazine</th></tr>${rows}</table>`);
}

/* ---------- Status: all flags ---------- */

function statusSection(status) {
    if (!status || typeof status !== "object") return null;
    const rows = Object.entries(status).map(([key, value]) => {
        const label = STATUS_LABELS[key] ?? key;
        const isErrorFlag = /Error/i.test(key);
        const mark = value === true
            ? (isErrorFlag ? badge("err", "yes") : badge("ok", "yes"))
            : `<span class="not-supported">no</span>`;
        return `<tr><td title="${esc(key)}">${esc(label)}</td><td>${mark}</td></tr>`;
    }).join("");
    return section("Status (all flags)", `<table class="mini">${rows}</table>`);
}

/* ---------- Feeds ---------- */

function feedsSection(feeds) {
    if (!Array.isArray(feeds) || !feeds.length) return null;
    const rows = feeds.map(feed => {
        const name = esc(orDash(feed.FeedType));
        if (nn(feed.NotSupported) !== null)
            return `<tr><td>${name}</td><td colspan="2" class="not-supported"
                title="${esc(feed.NotSupported)}">${esc(shortNotSupported(feed.NotSupported))}</td></tr>`;
        const rest = remainingFields(feed,
            ["FeedType", "Measurement", "NotSupported",
             "ValuePerMinute", "ValuePerRevolution", "ValuePerTooth", "ValuePercent"]);
        return `<tr>
            <td>${name}</td>
            <td class="num">${esc(feedValue(feed))}${rest}</td>
            <td class="not-supported">${esc(orDash(feed.Measurement))}</td>
        </tr>`;
    }).join("");
    return section("Feeds",
        `<table class="mini"><tr><th>Type</th><th>Value</th><th>Units (Measurement)</th></tr>${rows}</table>`);
}

/** Short form of NotSupported messages; the full text stays in the tooltip. */
function shortNotSupported(message) {
    const notFound = /^Feed property not found: (.+)$/.exec(message ?? "");
    if (notFound) return `no property ${notFound[1]}`;
    if (message === "Feed type is not supported") return "not supported";
    return message ?? "";
}

function feedValue(feed) {
    const parts = [];
    if (feed.ValuePerMinute !== undefined) parts.push(`${fmtNum(feed.ValuePerMinute)} mm/min`);
    if (feed.ValuePerRevolution !== undefined) parts.push(`${fmtNum(feed.ValuePerRevolution)} mm/rev`);
    if (feed.ValuePerTooth !== undefined) parts.push(`${fmtNum(feed.ValuePerTooth)} mm/tooth`);
    if (feed.ValuePercent !== undefined) parts.push(`${fmtNum(feed.ValuePercent)} %`);
    return parts.join(" · ") || "—";
}

/* ---------- Spindle ---------- */

function spindleSection(spindle) {
    if (!spindle || typeof spindle !== "object") return null;
    const pairs = Object.entries(SPINDLE_LABELS)
        .filter(([key]) => spindle[key] !== undefined)
        .map(([key, label]) => [label, kvText(formatScalar(spindle[key]))]);
    const rest = remainingFields(spindle, Object.keys(SPINDLE_LABELS));
    if (!pairs.length && !rest) return null;
    return section("Spindle", kvTable(pairs) + rest);
}

/* ---------- Coolant ---------- */

function coolantsSection(coolants) {
    if (!Array.isArray(coolants) || !coolants.length) return null;
    const rows = coolants.map(coolant => {
        if (nn(coolant.NotSupported) !== null)
            return `<tr><td colspan="4" class="not-supported">${esc(coolant.NotSupported)}</td></tr>`;
        const rest = remainingFields(coolant,
            ["Name", "Available", "Enabled", "TubeIndex", "NotSupported"]);
        return `<tr>
            <td>${esc(orDash(coolant.Name))}${rest}</td>
            <td>${coolant.Available ? "yes" : "no"}</td>
            <td>${coolant.Enabled ? "on" : "off"}</td>
            <td class="num">${esc(String(coolant.TubeIndex ?? "—"))}</td>
        </tr>`;
    }).join("");
    return section("Coolant", `<table class="mini">
        <tr><th>Channel</th><th>Available</th><th>State</th><th>Tube</th></tr>${rows}</table>`);
}

/* ---------- Statistics ---------- */

function timeSection(time) {
    if (!time || typeof time !== "object") return null;
    const pairs = Object.entries(TIME_LABELS)
        .filter(([key]) => time[key] !== undefined)
        .map(([key, label]) => [label, kvText(time[key])]);
    const rest = remainingFields(time, Object.keys(TIME_LABELS));
    return (pairs.length || rest) ? section("Time", kvTable(pairs) + rest) : null;
}

function blocksSection(blocks) {
    if (!blocks || typeof blocks !== "object") return null;
    const pairs = Object.entries(BLOCK_LABELS)
        .filter(([key]) => blocks[key] !== undefined)
        .map(([key, label]) => [label, kvText(fmtNum(blocks[key], 0))]);
    const rest = remainingFields(blocks, Object.keys(BLOCK_LABELS));
    return (pairs.length || rest) ? section("Toolpath blocks", kvTable(pairs) + rest) : null;
}

function lengthsSection(lengths) {
    if (!lengths || typeof lengths !== "object") return null;
    const rows = Object.entries(lengths)
        .map(([key, value]) => `<tr><td>${esc(key)}</td><td class="num">${fmtNum(value)}</td></tr>`)
        .join("");
    return section("Lengths, mm", `<table class="mini">${rows}</table>`);
}

function volumesSection(volumes) {
    if (!volumes || typeof volumes !== "object") return null;
    const rows = Object.entries(VOLUME_LABELS)
        .filter(([key]) => volumes[key] !== undefined)
        .map(([key, label]) =>
            `<tr><td title="${esc(key)}">${esc(label)}</td><td class="num">${fmtNum(volumes[key])}</td></tr>`)
        .join("");
    const rest = remainingFields(volumes, Object.keys(VOLUME_LABELS));
    return section("Volumes", `<table class="mini">${rows}</table>${rest}`);
}

/** Statistics keys beyond Time/Blocks/Lengths/Volumes. */
function statisticsRestSection(statistics) {
    const rest = remainingFields(statistics, ["Time", "Blocks", "Lengths", "Volumes"]);
    return rest ? section("Statistics — other", rest) : null;
}

/* ---------- Stocks ---------- */

function stocksSection(stocks) {
    if (!stocks || typeof stocks !== "object") return null;
    const pairs = Object.entries(STOCK_LABELS)
        .filter(([key]) => stocks[key] !== undefined)
        .map(([key, label]) => [label, kvText(formatScalar(stocks[key]))]);
    const rest = remainingFields(stocks, Object.keys(STOCK_LABELS));
    return (pairs.length || rest) ? section("Stocks, mm", kvTable(pairs) + rest) : null;
}

/* ---------- Attributes, notes ---------- */

function attributesSection(attributes) {
    if (!Array.isArray(attributes) || !attributes.length) return null;
    const rows = attributes
        .map(attr => {
            const rest = remainingFields(attr, ["AttributeName", "Value"]);
            return `<tr><td>${esc(orDash(attr.AttributeName))}${rest}</td><td>${esc(orDash(attr.Value))}</td></tr>`;
        })
        .join("");
    return section("Attributes", `<table class="mini">${rows}</table>`);
}

function notesSection(notes) {
    const text = nn(notes);
    return text === null ? null : section("Notes", `<div>${esc(text)}</div>`);
}

/* ---------- Workpiece coordinate systems ---------- */

function workpieceCSSection(csList) {
    if (!Array.isArray(csList) || !csList.length) return null;
    const blocks = csList.map(cs => {
        // the export writes the key in lower case: workpieceCS_World
        const world = cs.workpieceCS_World ?? cs.WorkpieceCS_World;
        const connector = cs.WorkpieceCS_WorkpieceConnector;
        const rest = remainingFields(cs,
            ["WorkpieceCSID", "workpieceCS_World", "WorkpieceCS_World", "WorkpieceCS_WorkpieceConnector"]);
        return `<div>
            <span class="mono">Workpiece CS ${esc(String(nn(cs.WorkpieceCSID) ?? "?"))}</span>
            ${world ? matrixDetails("In world CS", world) : ""}
            ${connector ? matrixDetails("In connector CS", connector) : ""}
            ${rest}
        </div>`;
    }).join("");
    return section("Workpiece CS", blocks);
}

/* ---------- Identifiers and misc ---------- */

function miscSection(row) {
    const toolpathRest = remainingFields(row.raw.Toolpath, ["ToolpathFileName"]);
    return section("Identifiers", kvTable([
        ["Operation ID", kvMono(row.id)],
        ["Parent ID", row.parentId ? kvMono(row.parentId) : "none (root)"],
        ["Type (OperationType)", kvMono(row.type)],
        ["Part index", kvText(row.partIdx)],
        ["Stage index", kvText(row.stageIdx)],
        ["Toolpath file", row.toolpathFile ? kvMono(row.toolpathFile) : "none"],
    ]) + toolpathRest);
}

/** Operation keys not covered by any section. */
function restSection(raw) {
    const rest = remainingFields(raw, OPERATION_HANDLED_KEYS);
    return rest ? section("Other fields", rest) : null;
}

function formatScalar(value) {
    if (typeof value === "number") return fmtNum(value);
    if (typeof value === "boolean") return value ? "yes" : "no";
    return orDash(value);
}
