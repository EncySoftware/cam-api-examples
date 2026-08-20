// "JSON" tab: lazy collapsible tree — children are rendered on expansion.

import { esc } from "./format.js";

const PAGE_SIZE = 100;

export function renderRaw(container, root) {
    container.innerHTML = `
        <div class="toolbar">
            <a href="/api/project" download="project.json">Download JSON</a>
        </div>
        <div class="json-tree" id="json-tree"></div>`;

    const tree = container.querySelector("#json-tree");
    tree.appendChild(nodeElement(null, root, true));
}

/** Tree node; for objects/arrays children are created only on first expansion. */
function nodeElement(key, value, initiallyOpen = false) {
    const wrapper = document.createElement("div");

    if (value === null || typeof value !== "object") {
        wrapper.innerHTML = `${keyHtml(key)}${scalarHtml(value)}`;
        return wrapper;
    }

    const isArray = Array.isArray(value);
    const count = isArray ? value.length : Object.keys(value).length;
    const header = document.createElement("span");
    header.className = "json-toggle";
    let open = false;
    let childrenEl = null;

    const setHeader = () => {
        header.innerHTML = `${keyHtml(key)}<span class="json-punct">${isArray ? "[" : "{"}</span>` +
            (open ? "" : `<span class="json-count"> ${count} ${isArray ? "items" : "keys"} </span><span class="json-punct">${isArray ? "]" : "}"}</span>`);
    };

    header.addEventListener("click", () => {
        open = !open;
        setHeader();
        if (open && !childrenEl) {
            childrenEl = buildChildren(value, isArray);
            wrapper.insertBefore(childrenEl, closer);
        }
        if (childrenEl) childrenEl.style.display = open ? "" : "none";
        closer.style.display = open ? "" : "none";
    });

    const closer = document.createElement("div");
    closer.innerHTML = `<span class="json-punct">${isArray ? "]" : "}"}</span>`;
    closer.style.display = "none";

    setHeader();
    wrapper.appendChild(header);
    wrapper.appendChild(closer);

    if (initiallyOpen) header.click();
    return wrapper;
}

function buildChildren(value, isArray) {
    const containerEl = document.createElement("div");
    containerEl.className = "json-node";
    const entries = isArray
        ? value.map((v, i) => [String(i), v])
        : Object.entries(value);

    let rendered = 0;
    const renderPage = () => {
        const page = entries.slice(rendered, rendered + PAGE_SIZE);
        for (const [childKey, childValue] of page)
            containerEl.appendChild(nodeElement(childKey, childValue));
        rendered += page.length;
        if (rendered < entries.length) {
            const moreBtn = document.createElement("button");
            moreBtn.className = "json-more";
            moreBtn.textContent = `… show more (${entries.length - rendered})`;
            moreBtn.addEventListener("click", () => {
                moreBtn.remove();
                renderPage();
            });
            containerEl.appendChild(moreBtn);
        }
    };
    renderPage();
    return containerEl;
}

function keyHtml(key) {
    return key === null ? "" : `<span class="json-key">"${esc(key)}"</span><span class="json-punct">: </span>`;
}

function scalarHtml(value) {
    if (value === null) return `<span class="json-bool">null</span>`;
    if (typeof value === "string") return `<span class="json-str">"${esc(value)}"</span>`;
    if (typeof value === "boolean") return `<span class="json-bool">${value}</span>`;
    return `<span class="json-num">${esc(String(value))}</span>`;
}
