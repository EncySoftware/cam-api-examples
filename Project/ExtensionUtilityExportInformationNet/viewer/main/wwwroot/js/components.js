// Reusable markup fragments: matrices, key/value tables, badges.

import { esc, fmtNum, orDash } from "./format.js";

/** 4x4 matrix (vX/vY/vZ/vT + A,B,C,D) in a collapsed <details>. */
export function matrixDetails(title, matrix) {
    if (!matrix || typeof matrix !== "object") return "";
    const rows = ["vX", "vY", "vZ", "vT"]
        .map(axis => {
            const v = matrix[axis] ?? {};
            return `<tr><th>${axis}</th>
                <td>${fmtNum(v.X, 4)}</td><td>${fmtNum(v.Y, 4)}</td><td>${fmtNum(v.Z, 4)}</td></tr>`;
        })
        .join("");
    const abcd = ["A", "B", "C", "D"]
        .map(k => `${k}=${fmtNum(matrix[k], 4)}`)
        .join(", ");
    return `<details class="matrix">
        <summary>${esc(title)}</summary>
        <table class="matrix-grid">
            <tr><th></th><th>X</th><th>Y</th><th>Z</th></tr>
            ${rows}
        </table>
        <div class="mono">${esc(abcd)}</div>
    </details>`;
}

/** Key/value table built from an array of [label, htmlValue] pairs. */
export function kvTable(pairs) {
    const rows = pairs
        .filter(([, value]) => value !== null && value !== undefined && value !== "")
        .map(([label, value]) => `<tr><td>${esc(label)}</td><td>${value}</td></tr>`)
        .join("");
    return `<table class="kv">${rows}</table>`;
}

/** Value as escaped text (for kvTable). */
export function kvText(value) {
    return esc(orDash(value));
}

/** Value in monospace — GUIDs, paths; full text in the tooltip. */
export function kvMono(value) {
    const text = orDash(value);
    return `<span class="mono" title="${esc(text)}">${esc(text)}</span>`;
}

/** Status badge: kind = ok | warn | err | muted | info. */
export function badge(kind, text, tooltip = "") {
    const title = tooltip ? ` title="${esc(tooltip)}"` : "";
    return `<span class="badge ${kind}"${title}>${esc(text)}</span>`;
}

/**
 * Full-coverage fallback: a table of object keys not handled explicitly.
 * Guarantees that no JSON field gets lost when the format changes.
 */
export function remainingFields(obj, handledKeys) {
    if (!obj || typeof obj !== "object" || Array.isArray(obj)) return "";
    const handled = new Set(handledKeys);
    const rest = Object.entries(obj).filter(([key]) => !handled.has(key));
    if (!rest.length) return "";
    const rows = rest
        .map(([key, value]) => `<tr><td>${esc(key)}</td><td>${genericValue(key, value)}</td></tr>`)
        .join("");
    return `<table class="mini">${rows}</table>`;
}

/** Universal value renderer: scalar, matrix, or compact JSON. */
export function genericValue(key, value) {
    if (value === null || value === undefined) return "—";
    if (typeof value !== "object") return esc(formatScalar(value));
    if (isMatrixLike(value)) return matrixDetails(key, value);
    return `<span class="mono">${esc(JSON.stringify(value))}</span>`;
}

function isMatrixLike(value) {
    return value && typeof value === "object" && "vX" in value && "vT" in value;
}

function formatScalar(value) {
    if (typeof value === "number") return fmtNum(value, 4);
    if (typeof value === "boolean") return value ? "yes" : "no";
    return orDash(value);
}
