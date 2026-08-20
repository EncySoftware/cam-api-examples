// "Overview" tab: project and machine cards + summary metrics across operations.

import { esc, fmtMs, fmtNum, get } from "./format.js";
import { kvTable, kvText, kvMono, remainingFields } from "./components.js";
import { buildOpsModel, leafRows } from "./ops-model.js";

const PROJECT_HANDLED_KEYS = [
    "FilePath", "Id", "MachineSetup",
    "ReorderedOperationsList", "DesignedOperationsList", "ToolsList",
    "Screenshots", "ScreenshotsError",
];

const MACHINE_SETUP_HANDLED_KEYS = ["MachineInfo", "Machine", "SetupStagesList"];

export function renderOverview(container, project) {
    const { rows } = buildOpsModel(project.ReorderedOperationsList ?? project.DesignedOperationsList);
    const leaves = leafRows(rows);

    const totalMs = sum(leaves.map(r => r.totalMs));
    const totalBlocks = sum(leaves.map(r => r.totalBlocks));
    const calculatedCount = leaves.filter(r => r.calculated).length;
    const errorCount = leaves.filter(r => r.hasError).length;
    const partsCount = countParts(project);

    container.innerHTML = `
        ${statTiles({ totalMs, leaves, calculatedCount, errorCount, totalBlocks, partsCount })}
        <div class="cards-row">
            ${screenshotCard(project)}
            <div class="card">
                <h2>Project</h2>
                ${kvTable([
                    ["File", kvMono(project.FilePath)],
                    ["Identifier", kvMono(project.Id)],
                ])}
                ${remainingFields(project, PROJECT_HANDLED_KEYS)}
            </div>
            ${machineCard(project)}
        </div>`;
}

function statTiles({ totalMs, leaves, calculatedCount, errorCount, totalBlocks, partsCount }) {
    const tiles = [
        tile("Total machining time", fmtMs(totalMs), "across all operations"),
        tile("Operations", String(leaves.length), `calculated: ${calculatedCount}`),
        errorCount > 0
            ? tile("With errors", String(errorCount), "need attention", "err")
            : tile("With errors", "0", "no errors", "ok"),
        tile("Toolpath blocks", fmtNum(totalBlocks), "lines + arcs"),
        tile("Parts in setup", String(partsCount), ""),
    ];
    return `<div class="stat-tiles">${tiles.join("")}</div>`;
}

function tile(label, value, sub, kind = "") {
    return `<div class="stat-tile ${kind}">
        <div class="label">${esc(label)}</div>
        <div class="value">${esc(value)}</div>
        ${sub ? `<div class="sub">${esc(sub)}</div>` : ""}
    </div>`;
}

// Card with the project screenshot from .stcp: the main preview shown large,
// the rest of the Thumbnails files as miniatures. No section — the card is not rendered.
function screenshotCard(project) {
    if (project?.ScreenshotsError) {
        return `<div class="card"><h2>Project screenshot</h2>
                    <p class="screenshot-error">Failed to extract: ${esc(project.ScreenshotsError)}</p>
                </div>`;
    }
    const shots = project?.Screenshots;
    if (!Array.isArray(shots))
        return "";
    if (shots.length === 0) {
        return `<div class="card"><h2>Project screenshot</h2>
                    <div class="screenshot-empty">The project has no saved preview</div>
                </div>`;
    }
    const main = shots.find(s => s.IsProjectPreview) ?? shots[0];
    const others = shots.filter(s => s !== main);
    return `<div class="card screenshot-card">
        <h2>Project screenshot</h2>
        <img class="screenshot-main" src="${esc(screenshotSrc(main))}" alt="${esc(main.Name)}" title="${esc(main.StoragePath)}">
        ${others.length ? `<div class="screenshot-thumbs">${others.map(s =>
            `<img src="${esc(screenshotSrc(s))}" alt="${esc(s.Name)}" title="${esc(s.StoragePath)}">`).join("")}</div>` : ""}
    </div>`;
}

function screenshotSrc(shot) {
    return shot.File
        ? `/api/screenshot?file=${encodeURIComponent(shot.File)}`
        : (shot.DataUri ?? "");
}

function machineCard(project) {
    const machineSetup = get(project, "MachineSetup", {});
    const info = machineSetup.MachineInfo ?? {};
    const machine = machineSetup.Machine ?? {};
    return `<div class="card">
        <h2>Machine (MachineInfo)</h2>
        ${kvTable([
            ["Name", kvText(info.MachineCaption)],
            ["Type", kvText(info.MachineTypeName)],
            ["GUID", kvMono(info.GUID)],
            ["Schema", kvMono(info.SchemaFilePath)],
            ["XML node", kvText(info.XMLNodeName)],
        ])}
        ${remainingFields(info, ["MachineCaption", "MachineTypeName", "GUID", "SchemaFilePath", "XMLNodeName"])}
        <h3>Machine (instance in the project)</h3>
        ${kvTable([
            ["Name", kvText(machine.MachineCaption)],
            ["GUID", kvMono(machine.GUID)],
            ["XML node", kvText(machine.XMLNodeName)],
        ])}
        ${remainingFields(machine, ["MachineCaption", "GUID", "XMLNodeName"])}
        ${remainingFields(machineSetup, MACHINE_SETUP_HANDLED_KEYS)}
    </div>`;
}

function countParts(project) {
    const stages = get(project, "MachineSetup.SetupStagesList", []);
    return stages.reduce((acc, stage) => acc + (stage.PartStageList?.length ?? 0), 0);
}

function sum(values) {
    const numbers = values.filter(v => typeof v === "number" && !Number.isNaN(v));
    return numbers.length ? numbers.reduce((a, b) => a + b, 0) : null;
}
