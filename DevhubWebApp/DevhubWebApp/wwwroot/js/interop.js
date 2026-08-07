// DevHub — clipboard, file download and media-query helpers.

export async function copyToClipboard(text) {
    try {
        await navigator.clipboard.writeText(text);
        return true;
    } catch {
        return legacyCopy(text);
    }
}

export async function readClipboard() {
    try {
        return await navigator.clipboard.readText();
    } catch {
        return '';
    }
}

export function downloadText(fileName, content, mimeType) {
    const blob = new Blob([content], { type: `${mimeType};charset=utf-8` });
    saveBlob(fileName, blob);
}

export function downloadBytes(fileName, base64Content, mimeType) {
    const binary = atob(base64Content);
    const buffer = new Uint8Array(binary.length);
    for (let index = 0; index < binary.length; index += 1) {
        buffer[index] = binary.charCodeAt(index);
    }
    saveBlob(fileName, new Blob([buffer], { type: mimeType }));
}

export function prefersDarkScheme() {
    return window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
}

function saveBlob(fileName, blob) {
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = fileName;
    document.body.appendChild(anchor);
    anchor.click();
    document.body.removeChild(anchor);
    URL.revokeObjectURL(url);
}

// Fallback for browsers/contexts without the async clipboard API (e.g. non-secure origins).
function legacyCopy(text) {
    const area = document.createElement('textarea');
    area.value = text;
    area.setAttribute('readonly', '');
    area.style.position = 'fixed';
    area.style.opacity = '0';
    document.body.appendChild(area);
    area.select();
    let copied = false;
    try {
        copied = document.execCommand('copy');
    } catch {
        copied = false;
    }
    document.body.removeChild(area);
    return copied;
}
