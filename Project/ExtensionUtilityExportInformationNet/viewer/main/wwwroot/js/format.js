// Formatting utilities and safe access to export data.

const TIME_RE = /^(\d+)h:(\d+)m:([\d.]+)s$/;

/** "08h:34m:38.530s" -> milliseconds; null if the format did not match. */
export function parseHms(text) {
    const m = TIME_RE.exec(text ?? "");
    if (!m) return null;
    return (Number(m[1]) * 3600 + Number(m[2]) * 60 + parseFloat(m[3])) * 1000;
}

/** Milliseconds -> compact string "8h 34m 39s" / "12.5s". */
export function fmtMs(ms) {
    if (ms === null || ms === undefined || Number.isNaN(ms)) return "—";
    const totalSeconds = ms / 1000;
    const h = Math.floor(totalSeconds / 3600);
    const min = Math.floor((totalSeconds % 3600) / 60);
    const sec = totalSeconds % 60;
    if (h > 0) return `${h}h ${min}m ${Math.round(sec)}s`;
    if (min > 0) return `${min}m ${Math.round(sec)}s`;
    return `${sec.toFixed(1)}s`;
}

/** The export writes the string "Null" instead of null — normalize it. */
export function nn(value) {
    if (value === undefined || value === null) return null;
    if (typeof value === "string" && (value === "Null" || value === "")) return null;
    return value;
}

/** Safe access by path "a.b.c"; missing keys -> dflt. */
export function get(obj, path, dflt = null) {
    let current = obj;
    for (const key of path.split(".")) {
        if (current === null || current === undefined || typeof current !== "object") return dflt;
        current = current[key];
    }
    return current === undefined || current === null ? dflt : current;
}

/** Number -> localized string; non-numbers -> "—". */
export function fmtNum(value, digits = 2) {
    if (typeof value !== "number" || Number.isNaN(value)) return "—";
    if (Number.isInteger(value)) return value.toLocaleString("en-US");
    return value.toLocaleString("en-US", { maximumFractionDigits: digits });
}

/** HTML escaping for inserting data into template strings. */
export function esc(text) {
    return String(text ?? "")
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;");
}

/** Value or a dash for display. */
export function orDash(value) {
    const v = nn(value);
    return v === null ? "—" : v;
}

/** Bytes -> "1.2 MB". */
export function fmtBytes(bytes) {
    if (typeof bytes !== "number" || bytes <= 0) return "—";
    const units = ["B", "KB", "MB", "GB"];
    let unitIdx = 0;
    let value = bytes;
    while (value >= 1024 && unitIdx < units.length - 1) {
        value /= 1024;
        unitIdx++;
    }
    return `${value.toFixed(unitIdx === 0 ? 0 : 1)} ${units[unitIdx]}`;
}
