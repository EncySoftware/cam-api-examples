import { esc, fmtNum, orDash } from "./format.js";
import { kvTable, kvMono, kvText, matrixDetails, badge, remainingFields } from "./components.js";

export function renderFixtures(fixtures) {
    if (!Array.isArray(fixtures)) return "";
    if (!fixtures.length) return "<h3>Fixtures</h3><p>No fixtures are assigned.</p>";
    return "<h3>Fixtures · " + fixtures.length + "</h3>" + fixtures.map(f => {
        const matrix = f.WorldPlacementMatrix;
        const position = matrix?.vT;
        const sources = (f.ModelItems ?? []).map(item =>
            "<details open><summary>" + esc(orDash(item.GeometryNodeFullName ?? item.Caption)) + "</summary>" +
            (item.Sources ?? []).map(source => kvTable([
                ["CAD source", kvMono(source.SourceCADModelFileID)],
                ["PLMGUID", source.PLMObjectID ? kvMono(source.PLMObjectID) : null],
                ["PLM object", source.IdInPLM ? kvMono(source.IdInPLM) : null],
                ["Connection", source.ConnectionId ? kvMono(source.ConnectionId) : null],
                ["PLM reference resolution", source.PLMObjectResolved === false
                    ? badge("warn", "Reference preserved; object unavailable in this session") : null],
            ])).join("") + "</details>").join("");
        const known = ["Id", "Caption", "Component", "ModelItemClassName", "IsInherited", "IsVisible",
            "WorkpieceConnectorIndex", "WorkpieceConnectorName", "DoNotUseConnectorMatrix", "MultiplyCount",
            "PlacementSpace", "SetupLCS", "NodeMatrix", "ConnectorSetupLCS", "WorldConnectorMatrix",
            "WorldPlacementMatrix", "ModelItems", "GeometryType", "GeometrySpace", "FileName", "FaceCount", "Warning"];
        return '<details class="fixture" open><summary>' +
            esc(orDash(f.Component)) + " / " + esc(orDash(f.Caption)) + " " +
            badge(f.IsInherited ? "info" : "muted", f.IsInherited ? "Inherited" : "Assigned here") + " " +
            (f.IsVisible === false ? badge("muted", "Disabled") : "") + "</summary>" +
            kvTable([
                ["Instance ID", kvMono(f.Id)],
                ["Machine connector", kvText(f.WorkpieceConnectorName) + " #" + esc(f.WorkpieceConnectorIndex)],
                ["World position, mm", position
                    ? ["X", "Y", "Z"].map(axis => axis + " = " + fmtNum(position[axis], 4)).join("; ") : null],
                ["Assigned face count", f.FaceCount === undefined ? null : kvText(f.FaceCount)],
                ["Geometry", f.FileName
                    ? '<a href="/api/model/' + encodeURIComponent(f.FileName) + '" download>Download STL</a> · source coordinates'
                    : null],
            ]) +
            (f.Warning ? badge("warn", f.Warning) : "") +
            matrixDetails("Placement in machine world coordinates", matrix) +
            "<details><summary>Source matrices</summary>" +
            matrixDetails("Node setup", f.SetupLCS) +
            matrixDetails("API node matrix (relative)", f.NodeMatrix) +
            matrixDetails("Connector setup", f.ConnectorSetupLCS) +
            matrixDetails("Connector in world coordinates", f.WorldConnectorMatrix) + "</details>" +
            sources + remainingFields(f, known) + "</details>";
    }).join("");
}
