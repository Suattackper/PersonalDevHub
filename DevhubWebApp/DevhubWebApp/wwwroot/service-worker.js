// Development service worker: intentionally inert so debugging never serves stale assets.
// The production behaviour lives in service-worker.published.js.
self.addEventListener('fetch', () => { });
