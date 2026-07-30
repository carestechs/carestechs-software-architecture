# Tauri 2 Desktop Shell

**Category:** deployment
**Stack:** angular
**Status:** Active
**Requires:** —
**Conflicts with:** —
**Last reviewed:** 2026-07-29

## Decision

Desktop applications are built using Tauri 2 as a lightweight native shell with an Angular frontend. The desktop app is its own purpose-built application — it is not a repackaged web app. The Rust backend provides native OS capabilities (filesystem access, dialogs, shell invocation) via Tauri plugins and custom commands. The Angular frontend communicates with Rust via Tauri's IPC mechanism (`invoke()`). For remote API calls, security-sensitive operations (auth, secrets, private APIs) go through Rust commands; public or CORS-friendly endpoints may use the Tauri HTTP plugin (`@tauri-apps/plugin-http`) or direct fetch. Vite serves the frontend in development; the production build bundles compiled static assets into the Tauri binary.

## Rationale

- Tauri produces significantly smaller binaries than Electron (~5-10 MB vs ~150 MB) by using the OS-native webview instead of bundling Chromium. This matters for distribution to industrial production environments where bandwidth may be limited.
- Alternatives considered: Electron (rejected — bloated, high memory usage, bundles Chromium), native .NET desktop (WPF/WinUI/MAUI) (rejected — platform-specific, would require maintaining a completely separate technology stack), Progressive Web App (rejected — lacks native filesystem access and offline capabilities needed for industrial environments).
- Tauri's IPC via `invoke()` provides a secure, typed communication channel between the frontend and the Rust backend. The Rust layer can perform local operations (file I/O, image processing, system commands) that a browser cannot, while the frontend handles all UI rendering.
- The hybrid approach to remote API calls follows the Tauri community consensus: secrets and auth tokens stay in Rust (never exposed to the inspectable webview), while simple public data fetching can go through the Tauri HTTP plugin to avoid unnecessary Rust boilerplate. Rust's HTTP client (reqwest) is not subject to browser CORS policies, making it the default choice for internal/private APIs.
- Tauri plugins (`@tauri-apps/plugin-fs`, `@tauri-apps/plugin-dialog`, `@tauri-apps/plugin-shell`) provide native capabilities, enabling features like local file browsing and direct image inspection.

## Constraints (non-negotiable for AI)

- Desktop applications MUST use Tauri 2 with the OS-native webview. NEVER bundle Chromium or use Electron.
- The desktop frontend MUST use Angular, but it is its own standalone application with its own components, services, and routes — NOT a copy of a web deployment.
- All native OS operations (filesystem, dialogs, shell commands, local processing) MUST go through Tauri IPC via `invoke()`. NEVER expose a local HTTP server from the Rust backend.
- API keys, auth tokens, and secrets MUST stay in Rust managed state. NEVER expose secrets to the frontend webview — proxy these calls through Rust commands.
- For remote API calls that involve secrets or authentication, MUST route through Rust commands. Public CORS-friendly APIs MAY use the Tauri HTTP plugin (`@tauri-apps/plugin-http`) or direct fetch.
- Tauri commands MUST be thin entry points — business logic belongs in separate Rust service modules, not in command functions.
- Command errors MUST return `Result<T, E>` where `E` implements `serde::Serialize`. Use `thiserror` for error variants with structured error information.
- Tauri capabilities MUST be explicitly declared in `src-tauri/capabilities/default.json` following least privilege. NEVER grant blanket permissions. Scope filesystem access to specific directories.
- For Rust-to-frontend communication (progress updates, background task results), MUST use Tauri events (`emit`/`listen`). NEVER poll Rust state from the frontend with `setInterval`.
- For large binary data (images, files), MUST use the filesystem + Tauri's asset protocol (`convertFileSrc()`). NEVER send multi-MB payloads over IPC as JSON.
- Development MUST use Vite's dev server with HMR. The Tauri dev server URL MUST be configured in `tauri.conf.json` under `devUrl`.
- Production builds MUST use `tauri build` which compiles the Angular frontend, bundles it into the Rust binary, and produces a platform-specific installer.
- The `src-tauri/` directory MUST be excluded from the Angular build's watch scope to prevent rebuild loops.
