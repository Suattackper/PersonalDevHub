const sessions = new Map();
const maxFileBytes = 20 * 1024 * 1024;
const maxPixels = 24_000_000;
const supportedTypes = new Set(['image/png', 'image/jpeg', 'image/webp', 'image/gif']);

export function pixelHex(data, offset) {
    return `#${[data[offset], data[offset + 1], data[offset + 2]].map(value => value.toString(16).padStart(2, '0')).join('').toUpperCase()}`;
}

export function removeConnectedBackground(source, width, height, tolerance, seedIndex = 0) {
    const data = new Uint8ClampedArray(source);
    if (width < 1 || height < 1 || data.length !== width * height * 4 || seedIndex < 0 || seedIndex >= width * height) return data;
    const seed = seedIndex * 4;
    const reference = [data[seed], data[seed + 1], data[seed + 2]];
    const threshold = Number(tolerance) * 4.42;
    const visited = new Uint8Array(width * height);
    const queue = [seedIndex];
    visited[seedIndex] = 1;
    for (let head = 0; head < queue.length; head++) {
        const index = queue[head];
        const offset = index * 4;
        if (Math.hypot(data[offset] - reference[0], data[offset + 1] - reference[1], data[offset + 2] - reference[2]) > threshold) continue;
        data[offset + 3] = 0;
        const x = index % width;
        const y = Math.floor(index / width);
        if (x > 0 && !visited[index - 1]) { visited[index - 1] = 1; queue.push(index - 1); }
        if (x + 1 < width && !visited[index + 1]) { visited[index + 1] = 1; queue.push(index + 1); }
        if (y > 0 && !visited[index - width]) { visited[index - width] = 1; queue.push(index - width); }
        if (y + 1 < height && !visited[index + width]) { visited[index + width] = 1; queue.push(index + width); }
    }
    return data;
}

export function initialize(id) {
    dispose(id);
    const root = document.getElementById(id);
    if (!root) return;
    const abort = new AbortController();
    sessions.set(id, abort);
    const { signal } = abort;
    const vi = root.dataset.language === 'vi';
    const pick = selector => root.querySelector(selector);
    const fileInput = pick('.devhub-image__file');
    const drop = pick('.devhub-image__drop');
    const workspace = pick('.devhub-image__workspace');
    const canvas = pick('.devhub-image__canvas');
    const context = canvas.getContext('2d', { willReadFrequently: true });
    const status = pick('.devhub-image__status');
    const compare = pick('.devhub-image__compare');
    const tolerance = pick('.devhub-image__tolerance');
    let original = null;
    let result = null;
    let fileName = 'image';
    let mode = 'pick';
    let pickedHex = '';
    let backgroundIndex = 0;

    const say = (english, vietnamese) => { status.textContent = vi ? vietnamese : english; };
    const render = () => {
        if (!original) return;
        const pixels = compare.checked || !result ? original : result;
        context.putImageData(new ImageData(pixels, canvas.width, canvas.height), 0, 0);
    };
    const setMode = next => {
        mode = next;
        pick('.devhub-image__mode-pick').setAttribute('aria-pressed', String(next === 'pick'));
        pick('.devhub-image__mode-background').setAttribute('aria-pressed', String(next === 'background'));
    };
    const showSample = (hex, swatchSelector, valueSelector) => {
        pick(swatchSelector).style.backgroundColor = hex;
        pick(valueSelector).textContent = hex;
    };

    async function loadFile(file) {
        if (!file) return;
        if (!supportedTypes.has(file.type)) {
            say('Choose a PNG, JPEG, WebP or GIF image.', 'Hãy chọn ảnh PNG, JPEG, WebP hoặc GIF.');
            return;
        }
        if (file.size > maxFileBytes) {
            say('The image exceeds the 20 MB limit.', 'Ảnh vượt quá giới hạn 20 MB.');
            return;
        }
        const url = URL.createObjectURL(file);
        try {
            const image = new Image();
            image.src = url;
            await image.decode();
            if (!image.naturalWidth || !image.naturalHeight || image.naturalWidth * image.naturalHeight > maxPixels) {
                say('Image dimensions exceed the 24-megapixel limit.', 'Kích thước ảnh vượt quá giới hạn 24 megapixel.');
                return;
            }
            canvas.width = image.naturalWidth;
            canvas.height = image.naturalHeight;
            context.clearRect(0, 0, canvas.width, canvas.height);
            context.drawImage(image, 0, 0);
            original = new Uint8ClampedArray(context.getImageData(0, 0, canvas.width, canvas.height).data);
            result = null;
            backgroundIndex = 0;
            compare.checked = false;
            fileName = file.name.replace(/\.[^.]+$/, '') || 'image';
            pick('.devhub-image__meta').textContent = `${file.name} · ${canvas.width} × ${canvas.height} · ${(file.size / 1024 / 1024).toFixed(2)} MB`;
            showSample(pixelHex(original, 0), '.devhub-image__background-swatch', '.devhub-image__background-hex');
            drop.hidden = true;
            workspace.hidden = false;
            say('Image ready. Pick a colour or sample the background.', 'Ảnh đã sẵn sàng. Hãy lấy mã màu hoặc chọn màu nền.');
        } catch {
            say('Could not decode this image.', 'Không thể đọc ảnh này.');
        } finally {
            URL.revokeObjectURL(url);
        }
    }

    drop.addEventListener('click', () => fileInput.click(), { signal });
    drop.addEventListener('keydown', event => {
        if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); fileInput.click(); }
    }, { signal });
    fileInput.addEventListener('change', () => { void loadFile(fileInput.files?.[0]); fileInput.value = ''; }, { signal });
    pick('.devhub-image__replace').addEventListener('click', () => fileInput.click(), { signal });
    for (const eventName of ['dragenter', 'dragover']) {
        root.addEventListener(eventName, event => {
            if (!event.dataTransfer?.types.includes('Files')) return;
            event.preventDefault();
            drop.classList.add('devhub-image__drop--active');
        }, { signal });
    }
    root.addEventListener('dragleave', event => {
        if (!root.contains(event.relatedTarget)) drop.classList.remove('devhub-image__drop--active');
    }, { signal });
    root.addEventListener('drop', event => {
        event.preventDefault();
        drop.classList.remove('devhub-image__drop--active');
        void loadFile(event.dataTransfer?.files?.[0]);
    }, { signal });
    document.addEventListener('paste', event => {
        if (!root.isConnected || document.querySelector('#image-tools-workspace') !== root) return;
        const target = event.target;
        if (target instanceof HTMLElement && (target.isContentEditable || ['INPUT', 'TEXTAREA'].includes(target.tagName))) return;
        const image = [...(event.clipboardData?.items ?? [])].find(item => item.kind === 'file' && item.type.startsWith('image/'))?.getAsFile();
        if (image) { event.preventDefault(); void loadFile(image); }
    }, { signal });

    pick('.devhub-image__mode-pick').addEventListener('click', () => setMode('pick'), { signal });
    pick('.devhub-image__mode-background').addEventListener('click', () => setMode('background'), { signal });
    canvas.addEventListener('click', event => {
        if (!original) return;
        const bounds = canvas.getBoundingClientRect();
        const x = Math.min(canvas.width - 1, Math.max(0, Math.floor((event.clientX - bounds.left) * canvas.width / bounds.width)));
        const y = Math.min(canvas.height - 1, Math.max(0, Math.floor((event.clientY - bounds.top) * canvas.height / bounds.height)));
        const index = y * canvas.width + x;
        const offset = index * 4;
        const hex = pixelHex(original, offset);
        if (mode === 'background') {
            backgroundIndex = index;
            showSample(hex, '.devhub-image__background-swatch', '.devhub-image__background-hex');
            say(`Background sample: ${hex}.`, `Màu nền đã chọn: ${hex}.`);
        } else {
            pickedHex = hex;
            showSample(hex, '.devhub-image__swatch', '.devhub-image__hex');
            pick('.devhub-image__rgb').textContent = `RGB ${original[offset]}, ${original[offset + 1]}, ${original[offset + 2]}`;
            pick('.devhub-image__copy').disabled = false;
            say(`Picked ${hex}.`, `Đã chọn màu ${hex}.`);
        }
    }, { signal });
    pick('.devhub-image__copy').addEventListener('click', async () => {
        if (!pickedHex) return;
        try { await navigator.clipboard.writeText(pickedHex); say(`${pickedHex} copied.`, `Đã sao chép ${pickedHex}.`); }
        catch { say(`Copy ${pickedHex} manually.`, `Hãy sao chép ${pickedHex} thủ công.`); }
    }, { signal });
    tolerance.addEventListener('input', () => { pick('.devhub-image__tolerance-value').textContent = tolerance.value; }, { signal });
    pick('.devhub-image__remove').addEventListener('click', () => {
        if (!original) return;
        result = removeConnectedBackground(original, canvas.width, canvas.height, tolerance.value, backgroundIndex);
        compare.checked = false;
        render();
        say('Background removed. Compare or download the PNG.', 'Đã xóa nền. Bạn có thể so sánh hoặc tải PNG.');
    }, { signal });
    compare.addEventListener('change', render, { signal });
    pick('.devhub-image__restore').addEventListener('click', () => {
        result = null;
        compare.checked = false;
        render();
        say('Original image restored.', 'Đã khôi phục ảnh gốc.');
    }, { signal });
    pick('.devhub-image__download').addEventListener('click', () => {
        if (!original) return;
        compare.checked = false;
        render();
        canvas.toBlob(blob => {
            if (!blob) { say('Could not export PNG.', 'Không thể xuất PNG.'); return; }
            const url = URL.createObjectURL(blob);
            const link = document.createElement('a');
            link.href = url;
            link.download = `${fileName}-devhub.png`;
            link.click();
            setTimeout(() => URL.revokeObjectURL(url), 30_000);
        }, 'image/png');
    }, { signal });
}

export function dispose(id) {
    sessions.get(id)?.abort();
    sessions.delete(id);
}
