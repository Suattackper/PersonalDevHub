# DevHub: image workspace and localization

## 1. Localization audit

- P0: inventory visible strings in the shell, dashboard, Settings, History, About, Image tools, shared components and all 24 tool modules. Track each English source string and its Vietnamese counterpart.
- P0: translate action labels, placeholders, empty/error states, tool descriptions, confirm dialogs and success/error notifications. Translate generated text such as counters and relative dates at its source rather than post-processing the DOM.
- P0: keep technical values (JSON, SQL, URLs, MIME values), user input and exported documents unchanged.
- P1: move the current `UiText` switch into a resource catalog with one key per message, parameter support, and a missing-key check in tests. This avoids accidental reuse of the same English wording for different contexts.
- P1: verify language changes immediately on the active page, persists after reload, updates document `lang`, and preserves tool workspace state.
- P1: check long Vietnamese labels at mobile/tablet widths, keyboard focus, screen-reader labels and fallback text.

Acceptance: with Vietnamese selected, normal navigation through every route has no English UI text apart from technical terms and user data. Switching back to English restores every label without reloading.

## 2. Image workspace

- P0: accept PNG, JPEG, WebP and GIF via browse, drag and drop, or clipboard paste while the image page is active. Paste must not intercept input fields or other routes.
- P0: validate file type, 20 MB byte limit and 24 MP pixel limit before working with image data; report errors in the selected language.
- P0: present an empty drop area, then a responsive preview with file name, dimensions, size and clear actions to replace, restore and download.
- P0: pick a pixel by click, show a colour swatch and HEX/RGB, and copy HEX on demand.
- P0: choose the background sample by click, adjust tolerance, compare original/result and restore.
- P0: remove only contiguous pixels similar to the chosen sample. Preserve original resolution and transparency in PNG export.
- P1: keep image bytes in memory only; release object URLs and event listeners when leaving the page. Avoid creating large base64 data URLs.
- P1: add zoom and pan, precise sampling at high zoom, and a visible processing state for large images.
- P2: evaluate an on-device segmentation model for complex photographic backgrounds, including download size, speed, offline caching and edge quality. Present this as a separate mode only if it meets performance targets.

Acceptance: a user can drop or paste an image, select a colour, sample and remove a simple background, compare, restore and export a transparent PNG at the original dimensions without leaving the page.

## 3. Verification

- Exercise browse, drop, paste, invalid file, colour sampling, removal, restore and download in a browser, including keyboard-only navigation.
- Run pure image algorithm checks for seed location, connected regions, tolerance boundaries, untouched source data and transparent output.
- Run .NET tests, Debug build and Release publish.
- Confirm navigation and image page still work with Vietnamese and English selected; inspect the browser console and PWA offline behavior.

Current background removal is colour based. Photographic subject segmentation needs a separate on-device model and a performance review before it can be presented as general background removal.
