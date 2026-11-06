// DevHub production service worker — offline-first cache of the published asset manifest.

self.importScripts('./service-worker-assets.js');
self.addEventListener('install', (event) => event.waitUntil(onInstall(event)));
self.addEventListener('activate', (event) => event.waitUntil(onActivate(event)));
self.addEventListener('fetch', (event) => event.respondWith(onFetch(event)));

const cacheNamePrefix = 'devhub-cache-';
const cacheName = `${cacheNamePrefix}${self.assetsManifest.version}`;
const offlineAssetsInclude = [/\.dll$/, /\.pdb$/, /\.wasm/, /\.html/, /\.js$/, /\.json$/, /\.css$/, /\.woff$/, /\.png$/, /\.jpe?g$/, /\.gif$/, /\.ico$/, /\.blat$/, /\.dat$/];
const offlineAssetsExclude = [/^service-worker\.js$/];

// The registration scope follows the app whether hosted at / or under a repository path.
const baseUrl = new URL(self.registration.scope);
const manifestUrlList = self.assetsManifest.assets.map((asset) => new URL(asset.url, baseUrl).href);

async function onInstall() {
    console.info('[DevHub] service worker: installing');

    const assetsRequests = self.assetsManifest.assets
        .filter((asset) => offlineAssetsInclude.some((pattern) => pattern.test(asset.url)))
        .filter((asset) => !offlineAssetsExclude.some((pattern) => pattern.test(asset.url)))
        .map((asset) => new Request(asset.url, { integrity: asset.hash, cache: 'no-cache' }));

    await caches.open(cacheName).then((cache) => cache.addAll(assetsRequests));
}

async function onActivate() {
    console.info('[DevHub] service worker: activating');

    const cacheKeys = await caches.keys();
    await Promise.all(
        cacheKeys
            .filter((key) => key.startsWith(cacheNamePrefix) && key !== cacheName)
            .map((key) => caches.delete(key))
    );
}

async function onFetch(event) {
    if (event.request.method !== 'GET') {
        return fetch(event.request);
    }

    // Serve the SPA shell for navigation requests, assets from the versioned cache.
    const shouldServeIndexHtml = event.request.mode === 'navigate';
    const request = shouldServeIndexHtml ? 'index.html' : event.request;
    const cache = await caches.open(cacheName);
    const cachedResponse = await cache.match(request);

    if (cachedResponse) {
        return cachedResponse;
    }

    const isManifestAsset = manifestUrlList.includes(new URL(event.request.url, self.origin).href);
    if (!isManifestAsset) {
        return fetch(event.request);
    }

    try {
        return await fetch(event.request);
    } catch {
        return new Response('Offline and asset is not cached.', { status: 504, statusText: 'Gateway Timeout' });
    }
}
