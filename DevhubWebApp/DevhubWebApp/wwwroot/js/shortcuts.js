// DevHub — global keyboard shortcuts.
// Combos are lower-case strings such as "ctrl+enter", "ctrl+s", "ctrl+shift+k".

const registrations = new Map();
let nextId = 1;

function describe(event) {
    const parts = [];
    if (event.ctrlKey || event.metaKey) parts.push('ctrl');
    if (event.altKey) parts.push('alt');
    if (event.shiftKey) parts.push('shift');

    const key = event.key.length === 1 ? event.key.toLowerCase() : event.key.toLowerCase();
    parts.push(key);
    return parts.join('+');
}

// Ctrl+C must keep working normally whenever the user has actually selected something;
// only an empty selection means "copy the whole document" for the tool.
function shouldYieldToNativeCopy(combo) {
    if (combo !== 'ctrl+c') {
        return false;
    }

    const selection = window.getSelection();
    return selection !== null && selection.toString().length > 0;
}

export function register(dotNetRef, combos) {
    const id = nextId++;
    const wanted = new Set(combos.map((combo) => combo.toLowerCase()));

    const handler = (event) => {
        const combo = describe(event);
        if (!wanted.has(combo) || shouldYieldToNativeCopy(combo)) {
            return;
        }

        event.preventDefault();
        event.stopPropagation();
        dotNetRef.invokeMethodAsync('OnShortcutAsync', combo);
    };

    document.addEventListener('keydown', handler, true);
    registrations.set(id, handler);
    return id;
}

export function unregister(id) {
    const handler = registrations.get(id);
    if (handler) {
        document.removeEventListener('keydown', handler, true);
        registrations.delete(id);
    }
}
