// DevHub — localStorage access, scoped to the "devhub." key namespace.
// Every function is defensive: storage can be full, disabled or partitioned by the browser.

const NAMESPACE = 'devhub.';

function namespacedKeys() {
    const keys = [];
    for (let index = 0; index < localStorage.length; index += 1) {
        const key = localStorage.key(index);
        if (key && key.startsWith(NAMESPACE)) {
            keys.push(key);
        }
    }
    return keys;
}

export function getItem(key) {
    try {
        return localStorage.getItem(key);
    } catch {
        return null;
    }
}

export function setItem(key, value) {
    try {
        localStorage.setItem(key, value);
    } catch (error) {
        console.warn('[DevHub] storage write failed', key, error);
    }
}

export function removeItem(key) {
    try {
        localStorage.removeItem(key);
    } catch {
        /* ignore */
    }
}

export function clearNamespace() {
    namespacedKeys().forEach((key) => localStorage.removeItem(key));
}

export function exportNamespace() {
    const payload = {};
    namespacedKeys().forEach((key) => {
        payload[key] = localStorage.getItem(key);
    });
    return JSON.stringify(payload);
}

export function importNamespace(json) {
    const payload = JSON.parse(json);
    Object.entries(payload)
        .filter(([key]) => key.startsWith(NAMESPACE))
        .forEach(([key, value]) => localStorage.setItem(key, value));
}
