const boardState = new WeakMap();

export function initialize(root) {
    if (!root) {
        return;
    }

    let state = boardState.get(root);
    if (!state) {
        const resizeHandler = () => refresh(root);
        const resizeObserver = new ResizeObserver(() => refresh(root));

        resizeObserver.observe(root);
        window.addEventListener("resize", resizeHandler);

        state = {
            resizeHandler,
            resizeObserver
        };

        boardState.set(root, state);
    }

    refresh(root);
}

export function refresh(root) {
    if (!root) {
        return;
    }

    requestAnimationFrame(() => fitBoard(root));
}

export function dispose(root) {
    const state = boardState.get(root);
    if (!state) {
        return;
    }

    state.resizeObserver.disconnect();
    window.removeEventListener("resize", state.resizeHandler);
    boardState.delete(root);
}

function fitBoard(root) {
    fitTitleElement(
        root.querySelector(".activity-card.current .activity-name, .activity-card.upcoming-main .activity-name"),
        14.2,
        2.8);

    fitTitleElement(
        root.querySelector(".activity-card.next .activity-name"),
        7.2,
        2.2);

    root.querySelectorAll(".meta-value").forEach((container) => {
        const isPrimary = Boolean(container.closest(".activity-card.current, .activity-card.upcoming-main"));
        fitSingleLineElement(container, isPrimary ? 3.2 : 2.45, isPrimary ? 1.2 : 1.05);
    });

}

function fitTitleElement(container, maxRem, minRem) {
    if (!container || container.clientHeight <= 0) {
        return;
    }

    const rootFontSize = getRootFontSize();
    const maxPx = maxRem * rootFontSize;
    const minPx = minRem * rootFontSize;

    container.style.fontSize = `${maxPx}px`;

    while (container.scrollHeight > container.clientHeight && parseFloat(container.style.fontSize) > minPx) {
        container.style.fontSize = `${parseFloat(container.style.fontSize) - 2}px`;
    }
}

function fitSingleLineElement(container, maxRem, minRem) {
    if (!container || container.clientWidth <= 0 || container.clientHeight <= 0) {
        return;
    }

    const rootFontSize = getRootFontSize();
    const maxPx = maxRem * rootFontSize;
    const minPx = minRem * rootFontSize;
    const target = container.firstElementChild || container;

    container.style.fontSize = `${maxPx}px`;

    while (
        (target.scrollWidth > container.clientWidth || target.scrollHeight > container.clientHeight) &&
        parseFloat(container.style.fontSize) > minPx
    ) {
        container.style.fontSize = `${parseFloat(container.style.fontSize) - 1}px`;
    }
}

function getRootFontSize() {
    return parseFloat(getComputedStyle(document.documentElement).fontSize) || 16;
}
