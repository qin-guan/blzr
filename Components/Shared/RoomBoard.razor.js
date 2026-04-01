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
            resizeObserver,
            clockTimeoutId: null,
            clockIntervalId: null
        };

        boardState.set(root, state);
        startClock(root, state);
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
    clearTimeout(state.clockTimeoutId);
    clearInterval(state.clockIntervalId);
    boardState.delete(root);
}

function startClock(root, state) {
    const timeEl = root.querySelector(".clock-time");
    const dateEl = root.querySelector(".clock-date");

    if (!timeEl && !dateEl) {
        return;
    }

    const locale = navigator.language;
    const timeFormat = new Intl.DateTimeFormat(locale, { hour: "2-digit", minute: "2-digit", second: "2-digit", hour12: false });
    const dateFormat = new Intl.DateTimeFormat(locale, { weekday: "long", day: "numeric", month: "long", year: "numeric" });

    function tick() {
        const now = new Date();
        if (timeEl) {
            timeEl.textContent = timeFormat.format(now);
        }
        if (dateEl) {
            dateEl.textContent = dateFormat.format(now);
        }
    }

    tick();

    const msUntilNextSecond = 1000 - (Date.now() % 1000);
    state.clockTimeoutId = setTimeout(() => {
        tick();
        state.clockIntervalId = setInterval(tick, 1000);
    }, msUntilNextSecond);
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
