# Creating a New Release

Release label = `<version>_<build>`, e.g. `2.0.0_1`.

- **version** — semver, source of truth is `AssemblyVersion` in
  `FloatingClock\Properties\AssemblyInfo.cs` (first three parts, e.g. `2.0.0.0` → `2.0.0`).
  Bump by hand when needed (also bump `AssemblyFileVersion` to match).
- **build** — plain integer stored in `build_version.txt` at the repo root.

## Version number

| Task | Command |
|------|---------|
| Get full release label (e.g. `2.0.0_1`) | `tools\version_get.bat` |
| Get build number | `tools\build_get.bat` |
| Increment build number | `tools\build_increment.bat` |
| Decrement build number (undo) | `tools\build_decrement.bat` |

Typical flow: run `tools\build_increment.bat` once per release, then
`tools\version_get.bat` to get the label for the release notes folder.

## Release notes

Folder structure: `release_notes\<version>_<build>\` at the repo root, one JSON file
per locale. Example: `release_notes\2.0.0_1\en.json`.

**Create only `en.json` by hand.** Schema (see `release_notes\2.0.0_1\en.json`):

```json
{
  "version": "2.0.0",
  "build": 1,
  "date": "YYYY-MM-DD",
  "title": "Short headline",
  "notes": [
    "First change, user-facing wording",
    "Second change"
  ]
}
```

The actual release notes text goes into the **`notes`** array (one string per bullet).
`version`/`build` must match the folder name.

## ⚠️ Translation step — MANDATORY, do not skip

The non-English locale files (`de.json`, `fr.json`, … ~40 languages) are **never**
written by hand. After creating/editing `en.json`, always run:

```
tools\translator_release_notes.bat
```

This calls the shared **GPT-json-translator** tool
(`D:\GIT\BenjaminKobjolke\GPT-json-translator`) in recursive mode: it finds every
`release_notes\<label>\en.json` and generates all other locales beside it. It is
incremental (only missing keys hit the API) and idempotent — re-running is safe.

A release is **not finished** until this step has run.

## Building the release

```
tools\build_release.bat
```

MSBuild builds the solution in Release configuration. Output:
`FloatingClock\bin\Release\FloatingClock.exe` (single self-contained exe via
Costura.Fody).

The whole `release_notes\` tree is **embedded into the exe** automatically — the
csproj contains a wildcard `EmbeddedResource` for `..\release_notes\**\*.json`
(logical names `FloatingClock.release_notes.<label>\<locale>.json`). New release
folders and translated locales are picked up on the next build; no csproj edit
needed.

(`tools\build_debug.bat` / `build_debug_and_run.bat` are for development only.)

## Publishing the release

```
tools\publish_release.bat
```

Wraps the shared **release-tool** (`D:\GIT\BenjaminKobjolke\release-tool`). It:

1. Copies the exe to `//XIDA-SERVER/SigningExecutables/`, waits for the signing
   service, verifies the signer (XIDA GmbH).
2. Uploads the signed exe to FTP (`workflow-tools.com`, `/downloads/floating-clock/`),
   moving the previous exe into `versions/<previous-version>/` first.
3. Uploads the `release_notes\` folder to
   `/public/sites/floating-clock/assets/release_notes`.

**Before running:** edit `--previous-version` in the bat to the version currently
live on FTP (names the backup folder).

Config lives in `tools\publish_settings.ini` (gitignored — FTP credentials;
`publish_settings_example.ini` is the tracked template).

## In-app release notes view

`FloatingClock\WhatsNewWindow.xaml(.cs)` — the "What's New" window.

- Opened from the **command palette**: right-click the clock or press `E`, then pick
  **What's New**.
- Shows the **newest release first**; **Older / Newer** buttons navigate through
  past releases.
- Locale = current UI language (`LocalizationManager.CurrentLanguage`), falls back
  to `en.json` when a locale file is missing.
- Reads the embedded resources directly, so the notes always ship inside the exe.

## Release checklist

1. Bump semver in `AssemblyInfo.cs` if needed (both `AssemblyVersion` and `AssemblyFileVersion`).
2. `tools\build_increment.bat`
3. `tools\version_get.bat` → create `release_notes\<label>\en.json` (schema above).
4. `tools\translator_release_notes.bat` ← **mandatory**
5. `tools\build_release.bat`
6. Edit `--previous-version` in `tools\publish_release.bat`, then run it to sign +
   upload the exe and release notes.
