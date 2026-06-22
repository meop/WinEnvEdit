# Upstream reports & PRs (drafts)

Findings from making WinEnvEdit run under Native AOT that are worth giving back. Each file is a
self-contained draft an agent (or you) can turn into a GitHub issue or PR on the target repo. They are
**drafts living in this repo** — nothing has been filed yet.

| Draft | Target repo | Kind | Confidence it's a real gap |
|-------|-------------|------|----------------------------|
| [winui-nested-list-in-datatemplate-aot.md](upstream/winui-nested-list-in-datatemplate-aot.md) | microsoft/microsoft-ui-xaml | issue + repro | High |
| [winui-datatemplate-events-aot.md](upstream/winui-datatemplate-events-aot.md) | microsoft/microsoft-ui-xaml | issue + repro | High |
| [cswinrt-collection-expression-ccw.md](upstream/cswinrt-collection-expression-ccw.md) | microsoft/CsWinRT | issue + repro | Medium-High |
| [resourcedictionary-indexer-null-aot.md](upstream/resourcedictionary-indexer-null-aot.md) | microsoft-ui-xaml / CsWinRT | needs-confirmation | Low-Medium |

Before filing any of these, **search the target repo** — WinUI 3 + AOT is actively evolving and some may
already be tracked or marked "by design / tracked by the AOT effort." Each draft has a "Could this be
working-as-designed?" note.

Environment for all reports: Windows 11 (25H2), .NET 10, Windows App SDK 2.1.x, `PublishAot=true`,
self-contained, full trim, x64. A `WinEnvEdit/Resources/VariableTemplates.xaml(.cs)` is the in-product
example of each workaround; `docs/aot.md` is the narrative.

## Filed & resolved

Already submitted upstream and closed — kept here as a record, not a draft to file.

| Record | Target repo | Outcome |
|--------|-------------|---------|
| [tomlyn-sourcegen.md](upstream/tomlyn-sourcegen.md) | xoofx/Tomlyn | `#128` array-of-tables headers — **accepted** (fixed in 2.7); `#129`/PR `#130` public ctors — **declined** (won't fix) |
