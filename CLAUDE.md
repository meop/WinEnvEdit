# CLAUDE.md

Guidance for Claude Code when working with WinEnvEdit.

**See also:** [PATTERNS.md](PATTERNS.md) for MVVM, XAML binding, and WinUI implementation patterns.

---

## Platform Detection

`<Platform>` is used throughout this document. Detect once per session:
```bash
./src/Scripts/Platform.ps1   # prints "ARM64" or "x64" to stdout
```

---

## Commands

All build/test commands require `-p:Platform=<Platform>`.

**Develop** (prebuild, build, test, run):
```bash
./src/Scripts/Prebuild.ps1
dotnet build WinEnvEdit.slnx -c Debug -p:Platform=<Platform>
dotnet test WinEnvEdit.slnx -c Debug -p:Platform=<Platform>
src/WinEnvEdit/bin/<Platform>/Debug/net10.0-windows10.0.26100.0/WinEnvEdit.exe
```

**Release** (self-contained):
```bash
dotnet build WinEnvEdit.slnx -c Release -p:Platform=<Platform>
src/WinEnvEdit/bin/<Platform>/Release/net10.0-windows10.0.26100.0/win-<Platform>/WinEnvEdit.exe
```

**Scripts** (all in `src/Scripts/`):
- `Platform.ps1` – Detects host platform (`ARM64` or `x64`)
- `Prebuild.ps1` – Formats code, generates icons, syncs versions. **Run before full builds or after changing VERSION**
- `Publish.ps1 -Platform <Platform>` – Builds MSI via WiX v6

---

## Project Structure

- **WinEnvEdit.Core** – Business logic library (no UI dependencies, fully testable)
- **WinEnvEdit** – WinUI 3 application (UI, ViewModels, integration)
- **WinEnvEdit.Tests** – Pure unit tests (xUnit, FluentAssertions, no mocks needed)
- **WinEnvEdit.Installer** – WiX v6 MSI installer project

Solution: `WinEnvEdit.slnx`

---

## Workflows

**Pipeline:** `.github/workflows/pipeline.yaml`

| Job          | Runs on        | Condition                                             |
| ------------ | -------------- | ----------------------------------------------------- |
| **version**  | all pushes/PRs | Detects if `VERSION` has a new tag                    |
| **validate** | all pushes/PRs | Checks formatting, version sync, builds, tests        |
| **publish**  | main only      | Builds MSI for all platforms (if version changed)     |
| **release**  | main only      | Creates GitHub release with MSIs (if version changed) |
| **package**  | main only      | Submits WinGet update PR via wingetcreate             |

Always run `Prebuild.ps1` locally and commit before pushing — the validate job fails if it produces uncommitted changes.

---

## Git Safety (CRITICAL)

- **NO AUTOMATIC COMMITS:** Only commit when explicitly asked
- Safe: `git status`, `git diff`, `git log`, `git fetch`
- Never without explicit request: `git push`, `git reset`, `git checkout -- <file>`, `git clean`
- See [AGENTS.md](AGENTS.md) for full rules

---

## Coding Rules (priority order)

1. **var**: Always use `var` for local variables (takes precedence over collection expressions)
2. **Naming**: camelCase fields (no underscore), PascalCase public members, no `Async` suffix
3. **Usings**: System → Third-party → Project (blank lines between groups), no fully qualified names
4. **Expressions**: `=>` for simple members, `[]` for collections when type is known
5. **Initialization**: Simple types inline, complex types/collections in constructors (see [PATTERNS.md](PATTERNS.md))
6. **Style**: 2-space indent, LF line endings, UTF-8 without BOM, K&R braces, trailing commas
7. **Tests**: Pure unit tests only (no registry, OS APIs, file system access)
8. **CLI-first**: All tasks must work without Visual Studio

---

## Testing

- Pure unit tests only (no registry, OS APIs, file system)
- xUnit with FluentAssertions; no mocks needed (Core is pure code)
- Naming: `{Method}_{Scenario}_{Expected}`
- Use `EnvironmentVariableBuilder` for test data
- Coverage: Core >90%, ViewModels >80%, Validation 100%
