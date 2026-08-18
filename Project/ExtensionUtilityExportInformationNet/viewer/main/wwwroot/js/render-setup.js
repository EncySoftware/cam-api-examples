// "Setup" tab: setup-stage tree → parts with geometry and workpiece setup.
// Full coverage: known keys get labels + remainingFields for the rest.

import { esc, get, nn, orDash } from "./format.js";
import { kvTable, kvText, kvMono, matrixDetails, badge, remainingFields } from "./components.js";

const PART_HANDLED_KEYS = [
    "PartIndex", "IsCopy", "PrototypePartIndex", "PartExternalID",
    "PartStageName", "PartGeometry", "PartSetup",
];

const GEOMETRY_HANDLED_KEYS = [
    "GeometryType", "FileName", "SourceCADModelFileID", "ModelItems", "GeometryCS",
];

const SETUP_HANDLED_KEYS = [
    "WorkpieceConnectorIndex", "WorkpieceConnectorName",
    "WorldWorkpieceConnectorMatrix", "OffsetCS", "WorkpieceCSList",
];

export function renderSetup(container, project) {
    const stages = get(project, "MachineSetup.SetupStagesList", []);
    if (!stages.length) {
        container.innerHTML = `<div class="empty-state"><div class="big">🗂️</div>The file has no setup data (SetupStagesList).</div>`;
        return;
    }
    container.innerHTML = stages.map(renderStage).join("");
}

function renderStage(stage) {
    const parts = stage.PartStageList ?? [];
    const rest = remainingFields(stage, ["SetupStageIndex", "PartStageList"]);
    return `<div class="stage-block">
        <h2>Setup stage ${esc(String(stage.SetupStageIndex ?? "?"))}
            <span class="result-count">— parts: ${parts.length}</span></h2>
        ${rest}
        <div class="part-cards">${parts.map(renderPart).join("")}</div>
    </div>`;
}

function renderPart(part) {
    const badges = [];
    if (part.IsCopy) badges.push(badge("info", `Copy of part #${part.PrototypePartIndex}`));

    return `<div class="card">
        <h2>${esc(orDash(part.PartStageName))} <span class="result-count">#${esc(String(part.PartIndex))}</span> ${badges.join(" ")}</h2>
        ${kvTable([
            ["Part index (PartIndex)", kvText(part.PartIndex)],
            ["Copy (IsCopy)", part.IsCopy === undefined ? null : (part.IsCopy ? "yes" : "no")],
            ["Prototype index", kvText(part.PrototypePartIndex)],
            ["External ID (PartExternalID)", kvText(part.PartExternalID)],
        ])}
        ${remainingFields(part, PART_HANDLED_KEYS)}
        ${renderGeometry(part.PartGeometry)}
        ${renderPartSetup(part.PartSetup)}
    </div>`;
}

function renderGeometry(geometry) {
    if (!geometry) return "";
    const items = (geometry.ModelItems ?? [])
        .map(item => modelItemLine(item))
        .join("");
    const cs = geometry.GeometryCS;
    const csRest = cs ? remainingFields(cs, ["GeometryCSName", "GeometryCSMatrix"]) : "";
    return `<h3>Geometry</h3>
        ${kvTable([
            ["Format", kvText(geometry.GeometryType)],
            ["File", kvMono(geometry.FileName)],
            ["CAD source", geometry.SourceCADModelFileID !== undefined ? kvMono(geometry.SourceCADModelFileID) : null],
        ])}
        ${items ? `<table class="mini"><tr><th>Model item</th><th>Class</th></tr>${items}</table>` : ""}
        ${cs ? matrixDetails(`Geometry CS: ${nn(cs.GeometryCSName) ?? ""}`, cs.GeometryCSMatrix) : ""}
        ${csRest}
        ${remainingFields(geometry, GEOMETRY_HANDLED_KEYS)}`;
}

function modelItemLine(item) {
    const rest = remainingFields(item, ["Caption", "ModelItemClassName"]);
    return `<tr>
        <td><span class="mono">${esc(orDash(item.Caption))}</span>${rest}</td>
        <td>${esc(orDash(item.ModelItemClassName))}</td>
    </tr>`;
}

function renderPartSetup(setup) {
    if (!setup) return "";
    const csList = (setup.WorkpieceCSList ?? [])
        .map(cs => {
            const world = cs.WorkpieceCS_World ?? cs.workpieceCS_World;
            const rest = remainingFields(cs,
                ["WorkpieceCSID", "WorkpieceCS_World", "workpieceCS_World", "WorkpieceCS_WorkpieceConnector"]);
            return `<div>
                <span class="mono">Workpiece CS ${esc(String(nn(cs.WorkpieceCSID) ?? "?"))}</span>
                ${world ? matrixDetails("In world CS", world) : ""}
                ${cs.WorkpieceCS_WorkpieceConnector ? matrixDetails("In connector CS", cs.WorkpieceCS_WorkpieceConnector) : ""}
                ${rest}
            </div>`;
        })
        .join("");

    return `<h3>Setup</h3>
        ${kvTable([
            ["Workpiece connector", `${kvText(setup.WorkpieceConnectorName)} <span class="result-count">#${esc(String(setup.WorkpieceConnectorIndex ?? "?"))}</span>`],
        ])}
        ${matrixDetails("Connector matrix (world)", setup.WorldWorkpieceConnectorMatrix)}
        ${matrixDetails("CS offset (OffsetCS)", setup.OffsetCS)}
        ${csList}
        ${remainingFields(setup, SETUP_HANDLED_KEYS)}`;
}
