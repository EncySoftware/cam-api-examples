// Operations model: normalization of raw records and tree building by ParentOperationID.

import { get, nn, parseHms } from "./format.js";

/** Labels for the error flags from the Status block. */
const ERROR_FLAG_LABELS = {
    IsError: "General error",
    IsRapidError: "Rapid moves error",
    IsCollisionError: "Collision",
    IsCalculationError: "Calculation error",
    IsMachineError: "Machine error",
    IsGougeError: "Gouge",
    IsToolError: "Tool error",
    IsLimitError: "Limits exceeded",
};

/**
 * Normalizes the operations list into flat model rows.
 * Returns { rows, byId } — rows in the original file order.
 */
export function buildOpsModel(rawList) {
    const rows = (rawList ?? []).map((raw, index) => normalizeOperation(raw, index));
    const byId = new Map(rows.filter(r => r.id).map(r => [r.id, r]));

    for (const row of rows) {
        const parent = row.parentId ? byId.get(row.parentId) : null;
        if (parent) {
            parent.children.push(row);
            row.parent = parent;
        }
    }
    for (const row of rows)
        row.isGroup = row.children.length > 0 || /Group/i.test(row.type ?? "");

    return { rows, byId };
}

function normalizeOperation(raw, index) {
    const status = raw.Status ?? {};
    const errorFlags = Object.keys(status)
        .filter(key => /^Is.*Error$/.test(key) || key === "IsError")
        .filter(key => status[key] === true);

    return {
        raw,
        index,
        id: nn(raw.OperationID),
        parentId: nn(raw.ParentOperationID),
        parent: null,
        children: [],
        isGroup: false,
        name: nn(raw.OperationName) ?? "(unnamed)",
        type: nn(raw.OperationType) ?? "",
        partIdx: raw.OperationPartIndex,
        stageIdx: raw.OperationSetupStageIndex,
        enabled: status.Enabled !== false,
        calculated: status.Calculated === true,
        simulated: status.Simulated === true,
        hasError: errorFlags.length > 0,
        errorFlags,
        totalMs: parseHms(get(raw, "Statistics.Time.TotalTime")),
        effectiveMs: parseHms(get(raw, "Statistics.Time.EffectiveWorkTime")),
        rapidMs: parseHms(get(raw, "Statistics.Time.RapidTime")),
        totalBlocks: get(raw, "Statistics.Blocks.TotalBlocks"),
        workLength: get(raw, "Statistics.Lengths.WorkLength"),
        rapidLength: get(raw, "Statistics.Lengths.RapidLength"),
        removalRate: get(raw, "Statistics.Volumes.VolumeRemovalRate"),
        toolpathFile: nn(get(raw, "Toolpath.ToolpathFileName")),
    };
}

/** Labels of the triggered error flags for the tooltip. */
export function describeErrorFlags(row) {
    return row.errorFlags
        .map(flag => ERROR_FLAG_LABELS[flag] ?? flag)
        .join(", ");
}

/** Leaf operations (not groups) — for totals without double counting. */
export function leafRows(rows) {
    return rows.filter(r => !r.isGroup);
}
