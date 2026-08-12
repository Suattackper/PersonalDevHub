# DevHub

An offline-first developer toolbox built as a **.NET 8 Blazor WebAssembly** application.
It runs entirely in the browser: no backend, no API, no database, no authentication.
All state lives in the browser's `localStorage` under the `devhub.` namespace.

| | |
|---|---|
| **Stack** | .NET 8, Blazor WebAssembly, C# 12, MudBlazor 8 |
| **Storage** | `localStorage` via a JS module, behind `IStorageService` |
| **Delivery** | Installable PWA, offline cache via service worker |
| **Theme** | Dark by default, light theme available, VS Code / GitHub inspired |

## Getting started

```bash
dotnet run --project DevhubWebApp/DevhubWebApp/DevhubWebApp.csproj
```

Run the tests:

```bash
dotnet test DevhubWebApp/DevhubWebApp.sln
```

Publish a production build (service worker + offline cache):

```bash
dotnet publish DevhubWebApp/DevhubWebApp/DevhubWebApp.csproj -c Release
```

Performance work must be measured in a **Release** build — Debug WebAssembly is several times
slower and will send you chasing the wrong bottleneck.

## Project structure

```
DevhubWebApp/
└── DevhubWebApp/
    ├── Components/
    │   ├── Navigation/     Sidebar, top bar — shell navigation
    │   └── Shared/         Reusable UI primitives (states, headers, splash)
    ├── Constants/          App constants, storage keys, tool ids, routes
    ├── Extensions/         DI composition root and small helper extensions
    ├── Helpers/            Presentation mapping helpers (labels, icons)
    ├── Interfaces/         Service contracts — every dependency is injected by interface
    ├── JSInterop/          Typed facades over browser APIs
    ├── Layout/             MainLayout — the desktop-style application shell
    ├── Models/             Records and settings POCOs
    ├── Pages/              Routable pages; Pages/Tools hosts the tool modules
    ├── Services/           Application logic (registry, preferences, history, theme)
    ├── Storage/            IStorageService implementations
    ├── Themes/             MudBlazor palette and typography
    ├── Utilities/          Pure, dependency-free algorithms used by tools
    └── wwwroot/js/         ES modules imported through JS interop
```

### Folder rules

| Folder | Contains | Must not contain |
|---|---|---|
| `Pages` | Routing, layout composition, parameter binding | Business logic, formatting algorithms |
| `Components` | Reusable, parameterised UI | Direct storage access |
| `Services` | All application logic and state ownership | Razor markup |
| `Utilities` | Pure static functions | Injected dependencies, JS interop |
| `JSInterop` | `IJSRuntime` calls | Business rules |

## Architecture

```
Pages / Components  ──▶  Interfaces  ──▶  Services  ──▶  IStorageService  ──▶  JS module  ──▶  localStorage
                                             │
                                             └──▶  IBrowserInteropService (clipboard, download, media query)
```

Rules that hold everywhere:

1. **No logic in code-behind.** Pages orchestrate; services decide. A page may call a service and
   bind its result, nothing more.
2. **Interface-first DI.** Components inject contracts (`IToolRegistry`, `IUserPreferenceService`, …),
   never concrete types. Registration lives in `Extensions/ServiceCollectionExtensions.cs`.
3. **One owner per piece of state.** `UserPreferenceService` owns settings/favorites/pins/recents,
   `HistoryService` owns the activity log. They broadcast a `Changed` event; components subscribe and
   unsubscribe in `Dispose`.
4. **State is hydrated once**, in `App.razor`, before the router renders. Nothing downstream has to
   handle a half-loaded application.
5. **No magic strings.** Storage keys, tool ids and routes are constants (`Constants/`).

### Tool framework

Tools are **discovered, not registered**. A tool is a Razor component annotated with `[Tool]`;
`AttributeToolDiscovery` scans the assembly at start-up and `ToolRegistry` validates and orders the
result. Nothing in the shell branches on a tool id — no switch, no if-per-tool.

```
[Tool] components ──▶ IToolDiscovery ──▶ IToolRegistry ──┐
                                                         ├──▶ IToolCatalog ──▶ Sidebar / Search / Dashboard
IUserPreferenceService (favorites, pins, usage) ─────────┘
```

- **Metadata** (`ToolDescriptor`): id, name, category, description, keywords, icon, route, version,
  order, tags, color, `IsExperimental`. Route is *derived* from the id, so it can never disagree.
- **Personal state** (`IsFavorite`, `IsPinned`, usage counters) lives in preferences and is joined
  onto metadata by `IToolCatalog` as a `ToolView`. One fact, one owner.
- **Categories** are data too (`Constants/ToolCategories.cs`): label, icon, order and description
  travel with the category, so rendering one never requires a switch statement.
- **Routing** goes through the single `/tools/{ToolId}` host, which renders the discovered component
  with `DynamicComponent` and cascades a `ToolContext` carrying the tool's metadata and a pre-bound
  history writer — a tool never repeats its own id.
- An unknown category id or a duplicate tool id throws at start-up rather than producing an
  invisible or unreachable tool.

### Adding a tool module

1. Add its id to `Constants/ToolIds.cs`.
2. Create `Components/Tools/MyTool.razor` with an `@attribute [Tool(...)]` declaration, inheriting
   `ToolComponentBase<TWorkspace>` — that supplies the workspace, undo history, debouncing,
   shortcut registration, copy/download helpers and history recording.
3. Put the algorithm in `Utilities/` (pure) or a service in `Services/` (stateful) — never in the
   component. Unit-test it.
4. Record notable actions with `RecordAsync("Formatted", preview)`.

That is the whole checklist. Sidebar, quick search, global search, categories, dashboard sections,
favorites, pins, history and usage counters pick the tool up with no further edits.

## Tools

### JSON

Formatting logic lives in `Utilities/JsonFormatter.cs` — pure, dependency-free and unit-tested;
the component only binds it to the UI. Documents are rewritten through `Utf8JsonWriter`, so
escaping, number precision and Unicode survive a round-trip. Errors are values, not exceptions:
the parser's position is normalised to 1-based line/column, shown in the status bar and pushed to
the editor as a marker.

Editing is Monaco (`Components/Shared/CodeEditor.razor`, reusable by any future tool), vendored
locally through the BlazorMonaco package so the PWA still works offline.

**Measured performance** (Release build, 20 000-object fixture, this machine):

| Document | Pretty |
|---|---|
| 30 KB | ~0.19 s |
| 185 KB | ~0.85 s |
| 1.27 MB → 2.6 MB / 180 k lines | ~6.2 s (≈2.4 s of it is Monaco's own `setValue`) |

Getting there needed four fixes, each of which is a rule worth keeping:

1. The tree materialised every node up front — thousands of components. Children are now loaded
   when a branch is expanded, and capped at 100 per container.
2. Every operation re-parsed the document up to three times (validate, duplicates, tree).
   `JsonFormatter.Inspect` does all three in one parse, and formatter output is not re-validated.
3. Pushing a value into Monaco echoed back as a change event, marshalling megabytes to C# for a
   value already in hand. The echo is now suppressed.
4. Rewriting walked the document node by node even when no sorting or de-duplication was asked
   for; that path now hands the whole document to the runtime in one pass.

Above 512 KB the duplicate scan and the tree pause (validation still runs) and the status bar says
so; above 256 KB the document is not written to the auto-saved workspace, which would otherwise
risk the origin's localStorage quota.

### Markdown

`IMarkdownService` (Markdig) renders and analyses; `IHtmlExportService` builds the standalone HTML
export. Raw HTML in the source is **disabled** in the pipeline — the preview renders text the user
may have pasted from anywhere, and it must never be able to execute it. Toolbar actions are a pure
lookup (`MarkdownSyntax`), applied to the Monaco selection, so they land in the editor's own undo
stack.

### Text Diff

`DiffEngine` computes a line diff over the longest common subsequence (the model Git uses) and
`DiffExporter` renders it as unified text or an HTML table. Monaco's diff view does the on-screen
rendering; the engine supplies the statistics and the exports. Above 4 000 lines the LCS table
would need tens of millions of cells, so the engine falls back to a linear comparison — it still
reports every difference, and the UI badges the result as approximate.

### Base64

`Base64Converter` encodes, decodes and **sniffs content from magic bytes** rather than trusting a
declared MIME type. It accepts what people actually paste: data URIs, the URL-safe alphabet,
missing padding and embedded line breaks. Images preview inline; other binaries are offered as a
download with the detected extension.

### URL

`UrlToolkit` separates the two escaping scopes that get confused in practice: component encoding
(escapes `&`, `=`, `/` — for one query value) and whole-URL encoding (escapes only illegal
characters). The parser feeds an editable query list, and every edit rebuilds the URL field, which
stays the single source of truth.

### JWT

`JwtDecoder` reads a token; it never claims to verify one. Signature verification needs the
issuer's key, which a client-side tool has no business holding — so the UI reports what the token
*claims*, labels unsigned tokens (`alg: none`) as untrustworthy, and warns on expiry. It accepts a
token pasted with its `Bearer ` prefix and reads `NumericDate` claims that issuers emit as strings.

### Hash

`HashCalculator` computes MD5, SHA-1, SHA-256, SHA-384 and SHA-512, detects an algorithm from a
digest's length and verifies in constant time — a hash check is a security check, and
timing-dependent string equality is exactly the pattern that leaks in one.

**MD5 is implemented in `Md5Digest`** (RFC 1321, tested against the full published vector suite):
`System.Security.Cryptography.MD5` is *unsupported on the browser runtime*, because WebAssembly
delegates hashing to SubtleCrypto, which deliberately does not offer MD5. MD5 and SHA-1 are badged
as legacy in the UI and exist for checksums and legacy interop only.

### UUID, Password, Lorem Ipsum

- `UuidGenerator` — version 4 batches, five textual layouts, TXT/CSV/JSON export.
- `PasswordGenerator` — every choice from `RandomNumberGenerator`; `System.Random` would make
  generated secrets guessable. Entropy is `length × log2(alphabet)`, and a *pronounceable* secret
  reports the entropy of its syllable set, not of the full character space — it is weaker than it
  looks and the meter says so. Passphrase entropy comes from word choices, not characters.
  Generated secrets are never persisted or written to the activity log.
- `LoremGenerator` — seeded and deterministic, so a passage can be reproduced in a test or a
  screenshot. Line endings are always `\n`, never the host's.

### Regex

`RegexTester` bounds every run with a 2-second timeout. A hand-written pattern can backtrack
catastrophically — `(a+)+$` against a long line will hang the tab — so the timeout is not a nicety,
it is the difference between a tool and a denial of service on the user. A pinned test proves it.
Pattern library and cheatsheet live in `Constants/RegexLibrary.cs` as data; every shipped template
is tested to compile and match its own sample.

### Case, Number base

- `CaseConverter` routes every style through **one tokeniser**, which is why
  `parseHTTPResponse` → `parse_http_response` and acronym runs stay intact.
- `NumberBaseConverter` validates before converting, accepts prefixes (`0x`, `0b`, `0o`),
  separators and signs, reports overflow instead of throwing, and reads text as runes so an emoji
  is one code point rather than two UTF-16 halves.

### QR and barcodes

QR uses QRCoder's **managed** renderers only (`PngByteQRCode`, `SvgQRCode`); its bitmap renderers
need System.Drawing, which the browser runtime does not have. `QrPayloadBuilder` owns the payload
grammars — the Wi-Fi escaping rule in particular, where an unescaped `;` silently truncates the
SSID a scanner reads.

Barcodes are encoded **in-house** (`BarcodeEncoder`) for the same reason: the common .NET barcode
packages depend on System.Drawing.

> **Scope note.** Code 39, Codabar and UPC-E are *not* shipped. Their pattern tables could not be
> verified against a published specification in this environment — a structural audit found 18 of
> 44 Code 39 entries with the wrong number of wide elements — and a barcode that looks plausible
> but does not scan is worse than one that is absent. Code 128, EAN-13, EAN-8, UPC-A and ITF are
> implemented, with module counts and check digits pinned by tests. Before printing production
> labels, verify output with a real scanner.

### Colour

`ColorConverter` parses every notation a developer pastes (`#rgb`, `#rrggbb`, `#rrggbbaa`, `rgb()`,
`rgba()`, `hsl()`) and converts between HEX, RGB, HSL, HSV and CMYK. Contrast uses the WCAG 2
relative-luminance formula, and `GetReadableTextColor` picks black or white **by measured contrast**
rather than by a lightness threshold, which is what makes mid-tone backgrounds come out right.
`PaletteGenerator` builds 50-900 scales (step 500 is the base colour, pinned by a test) and
harmonies, exporting to CSS custom properties, SCSS variables plus a map, or JSON.

### Timestamp, MIME, HTTP status

- `TimestampConverter` reads Unix seconds or milliseconds (detected from magnitude), ISO 8601 and
  common layouts, and treats a value without an offset as UTC so results never depend on the host.
  The browser runtime ships full time-zone data — 313 zones are available and verified in the app.
- `MimeCatalog` and `HttpStatusCatalog` are data; their tools are list rendering over a lookup
  service. Search ranks exact hits first, so typing `png` or `404` shows that entry, not the first
  substring match. A test asserts the HTTP catalogue has **no dangling related-code references** —
  it caught five when it was first run.

### Cron

`CronParser` supports both dialects: five-field Linux cron and Quartz with seconds and an optional
year. Parsing expands every field into its explicit value set, which is what makes description and
scheduling simple. Two behaviours are worth knowing, and both are pinned by tests:

- When **both** day fields are restricted, cron takes their **union** — `0 9 1 * MON` fires on the
  1st *and* on every Monday. Getting this wrong is the classic cron bug.
- The next-run search is bounded to five years, so an unsatisfiable schedule such as `0 9 30 2 *`
  (30 February) terminates and reports "this schedule never runs" instead of hanging.

### Data converter

`DataConversionService` routes all six directions through one in-memory shape (`JsonNode`): three
readers, three writers, no bespoke translators. XML attributes round-trip as `@name` properties and
repeated elements as arrays. JSON keys that are illegal XML names are sanitised rather than throwing.

## Dashboard

`Pages/Dashboard.razor` is composition only — it holds no state, subscribes to nothing and blocks
re-renders after the first one. Each section under `Components/Dashboard/` derives from
`DashboardSectionBase`, which gives it:

- its own load through `IDashboardService` (the single data source: no component computes a number),
- its own skeleton, empty and error state, with retry,
- its own subscription to `IDashboardService.Changed`.

The consequence is the performance requirement: toggling a favorite re-renders the sections that
show favorites, not the page. Pinned, Favorites, Recent and Most Used are the *same* `ToolSection`
component with a different loader delegate, so the four-state layout exists exactly once.

## Design system

Every UI surface is composed from `Components/Shared`. Feature code must not re-implement any of
these; if a variant is missing, extend the component instead of copying it. `/playground` renders
all of them live.

| Component | Purpose |
|---|---|
| `AppCard` | Base surface with header / body / footer slots |
| `ToolCard` | Catalogue tile with favorite and pin affordances |
| `StatCard` | Single metric tile |
| `EmptyState` / `ErrorState` | Wrappers over the shared `StateMessage` primitive |
| `LoadingSkeleton` | Text, card, list and block placeholders |
| `LoadingOverlay` | Scoped or full-screen blocking progress |
| `PageHeader` | H1, subtitle, meta badges, page actions |
| `SectionTitle` / `DividerTitle` | Block heading and inline rule |
| `Toolbar` | Action strip with automatic start/end split |
| `SearchBox` | Debounced search input |
| `CopyButton` / `DownloadButton` | Clipboard and file output with feedback |
| `IconButton` | Icon action with a mandatory tooltip and ARIA label |
| `FileDropZone` | Drag-and-drop or browse, text or binary |
| `ConfirmDialog` | Reached through `IDialogService.ConfirmAsync` |
| `Badge` / `Chip` | Static label and interactive tag |
| `SettingItem` | Label + description + control row |

Component rules: no business logic, no hardcoded colors (MudBlazor palette variables only), both
themes supported by construction, XML documentation on every parameter.

## Coding convention

- File-scoped namespaces, `var` when the type is apparent, braces always.
- One public type per file; components use `.razor` + `.razor.cs` code-behind once they hold logic.
- Async methods end with `Async` and return `Task`/`ValueTask`; no `async void`.
- `StateHasChanged` only in event-handler callbacks that Blazor cannot observe (service events).
- Warnings are errors — `TreatWarningsAsErrors` is on, with rule exceptions documented in
  `.editorconfig`.
- Public members carry XML documentation; comments explain *why*, not *what*.

## Contributing

1. Branch from `main`.
2. Implement **one module per change**: it must build with zero warnings and zero errors.
3. Run `dotnet build` before committing; the analyzer gate is part of the build.
4. Refactor before extending — if the architecture no longer fits the feature, fix the architecture
   first.
5. Commit messages: imperative mood, scope prefix, e.g. `tools(json): add minify action`.

## Status

| Module | State |
|---|---|
| 0 — Foundation (shell, DI, storage, theme, PWA) | done |
| 1 — Design system (20 components + `/playground`) | done |
| 2 — Tool framework (metadata, discovery, catalog) | done |
| 3 — Dashboard (statistics, search, sections, history) | done |
| 4 — JSON formatter (Monaco, tree, undo/redo, shortcuts) | done |
| 6 — Markdown editor (live preview, toolbar, export) | done |
| 7 — Text diff (Monaco diff, statistics, export) | done |
| 8 — Base64 toolkit (text, files, previews) | done |
| 9 — URL toolkit (encode/decode, parser, query editor) | done |
| 10 — JWT decoder (three panels, expiry, validation) | done |
| 11 — Hash generator (incl. managed MD5 for WASM) | done |
| 12 — UUID generator | done |
| 13 — Regex tester | done |
| 14 — Password generator | done |
| 15 — Lorem Ipsum generator | done |
| 16 — Case converter | done |
| 17 — Number base converter | done |
| 18 — QR code generator | done |
| 19 — Barcode generator (5 of 8 symbologies — see the scope note) | done |
| 20 — Colour toolkit | done |
| 21 — Timestamp converter | done |
| 22 — MIME type explorer | done |
| 23 — HTTP status explorer | done |
| 24 — Cron expression builder | done |
| 25 — JSON / YAML / XML converter | done |
| Remaining tool workspaces (SQL formatter, HTML escape) | declared and routable; implemented one module at a time |
| Settings, History pages | placeholder pages, full modules pending |
