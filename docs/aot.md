# Native AOT (WinUI 3) — What Works, What Doesn't, and Why

WinEnvEdit ships as a **self-contained, fully-trimmed, Native AOT** executable: no external
Windows App Runtime, no .NET Desktop Runtime, smallest possible single build. Getting WinUI 3 to
run under `PublishAot` took a long bisection because the failures are silent — bindings, events, and
list virtualization simply do nothing instead of throwing. This doc records the dead ends, the
working patterns, and the diagnostic method, so we never re-walk this path.

> TL;DR of the mental model: under AOT in this app, **classic `{Binding}` works** (because the
> bound types are `[GeneratedBindableCustomProperty]`), but **anything the XAML compiler has to
> generate per-template is silently absent**: `x:Bind`, `x:Load`, in-template event handlers, and
> item-container generation for a list nested inside a `DataTemplate`.

---

## Where WinUI 3 + AOT stands (context)

WinUI 3 + Native AOT is **not fully shipped/GA** — it's still maturing
([microsoft-ui-xaml #8082](https://github.com/microsoft/microsoft-ui-xaml/discussions/8082),
[WindowsAppSDK #2478](https://github.com/microsoft/WindowsAppSDK/issues/2478)). The gaps below are the
platform's, not a sign the app is doing anything exotic (dual-pane lists, templated cards, a nested
editable list, and context menus are ordinary WinUI).

Two layers, two owners:
- **CsWinRT** ([aot-trimming.md](https://github.com/microsoft/CsWinRT/blob/master/docs/aot-trimming.md))
  covers the *interop* layer — CCWs, vtables, `[GeneratedBindableCustomProperty]`, `partial`. We follow it.
- **WinUI XAML** (microsoft-ui-xaml) owns the *higher-level* gaps CsWinRT does **not** address —
  `DataTemplateSelector`, `x:Bind`/events inside `DataTemplate`s, item realization for nested lists.

The ecosystem deals with this by **both** changing patterns (prefer `x:Bind`, drop selectors, use source
generators, avoid reflection) **and** rooting the app assembly for the dynamic bits. We did both.

### Which of our 1.0.x → AOT changes are keepers vs. workarounds

Useful when the platform closes a gap and a workaround can be retired:

- **Genuine improvements / fixes (keep regardless of AOT):** symmetric dirty-state reconciliation;
  direct-HKCU + self-elevation save (faster, no PowerShell); drag-overwrite, contextual paste, batch undo,
  busy-block, blank-line export, stricter path validation; `GlyphExtension` switch (cleaner *and* AOT-safe).
- **AOT workarounds (revisit if/when WinUI fixes the gap):**
  - **Code-hosted path list** (attached property + dispatcher) — honestly *less* elegant than a declarative
    nested `ListView`; the one to delete first once nested item realization works in a template.
  - Single `Visibility`-gated template replacing `DataTemplateSelector` — lateral.
  - ListView-level context menu instead of in-template `ContextRequested` — lateral.
  - Manual `IRelayCommand` instead of `[RelayCommand]` — more boilerplate, forced by `MVVMTK0046`.
  - `[GeneratedBindableCustomProperty]` + `TrimmerRootAssembly` — required scaffolding.

Repro projects and proposed upstream reports live in [docs/upstream/](upstream/).

---

## Build configuration

`WinEnvEdit.csproj` (functional groups; the toggles drive the rest):

```xml
<!-- Deployment toggles -->
<SelfContained>true</SelfContained>
<PublishAot>true</PublishAot>

<!-- Derived (separate PropertyGroup so conditions see the values above) -->
<WindowsAppSDKSelfContained>$(SelfContained)</WindowsAppSDKSelfContained>
<!-- self-contained bundles the runtime; the bootstrapper otherwise hunts for an installed one
     and fail-fasts with 0xC0000602 -->
<WindowsAppSDKBootstrapInitialize Condition="'$(SelfContained)' == 'true'">false</WindowsAppSDKBootstrapInitialize>
<WindowsAppSDKBootstrapInitialize Condition="'$(SelfContained)' != 'true'">true</WindowsAppSDKBootstrapInitialize>
<CsWinRTAotOptimizerEnabled Condition="'$(PublishAot)' == 'true'">true</CsWinRTAotOptimizerEnabled>
<CsWinRTAotWarningLevel    Condition="'$(PublishAot)' == 'true'">2</CsWinRTAotWarningLevel>
<PublishReadyToRun>false</PublishReadyToRun>
```

```xml
<!-- Root the app assemblies: full trim strips types only reached via XAML
     (converters, markup extensions); rooting keeps the framework fully trimmed but the UI working. -->
<ItemGroup Condition="'$(PublishAot)' == 'true'">
  <TrimmerRootAssembly Include="WinEnvEdit" />
  <TrimmerRootAssembly Include="WinEnvEdit.Core" />
</ItemGroup>
```

Reference the **component** packages (`Microsoft.WindowsAppSDK.WinUI` + `Microsoft.WindowsAppSDK.Runtime`),
**not** the `Microsoft.WindowsAppSDK` metapackage — the metapackage pulls AI/ML payloads
(onnxruntime/DirectML) that bloat the MSI from ~9 MB to ~51 MB.

Publish:

```powershell
$env:PATH = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer;$env:PATH"
dotnet publish WinEnvEdit\WinEnvEdit.csproj -c Release -p:Platform=x64 -r win-x64
```

### Project & dev setup (and why)

A few project-file / tooling choices exist specifically because of AOT:

- **Custom entry point** (`Program.cs` + `<DefineConstants>DISABLE_XAML_GENERATED_MAIN</DefineConstants>`):
  to write system (`HKLM`) variables, the app re-launches itself **elevated** in a headless `--apply-system`
  mode that writes the registry and exits — no PowerShell. A custom `Main` handles that path before any UI
  starts; otherwise it does the same `ComWrappersSupport.InitializeComWrappers()` + `Application.Start(...)`
  the generated main would.
- **Drop the unused WPF/WinForms WebView2 assemblies** — a WinUI app only uses `Microsoft.Web.WebView2.Core`;
  the transitive `…Wpf`/`…WinForms` assemblies are built against legacy `WindowsBase` and cause an `MSB3277`
  version-conflict warning. Remove them at the source (no `NoWarn` suppression):
  ```xml
  <Target Name="RemoveUnusedWebView2Assemblies" BeforeTargets="ResolveAssemblyReferences">
    <ItemGroup>
      <Reference Remove="@(Reference)"
                 Condition="'%(Filename)' == 'Microsoft.Web.WebView2.Wpf' Or '%(Filename)' == 'Microsoft.Web.WebView2.WinForms'" />
    </ItemGroup>
  </Target>
  ```
- **`CsWinRTAotOptimizerEnabled` + `CsWinRTAotWarningLevel=2`** — the optimizer emits the CCW/vtable code AOT
  needs; level 2 surfaces every AOT/trim binding warning at build time.
- **`TrimmerRootAssembly` for the app + core** — full trim would otherwise strip app types only reached via
  XAML (converters, markup extensions); rooting keeps the framework fully trimmed but the UI working.
- **Debugging** (`Properties/launchSettings.json`): `"debugEngines": "managed,native"` enables mixed
  managed+native debugging (stepping across the CsWinRT/AOT boundary), and `"hotReloadEnabled": true` keeps
  XAML/C# hot reload for the unpackaged Debug profile. Dev-only; not shipped.

---

## Trimming — what we chose and why

**Trim modes.** `TrimMode=partial` ("link") trims only assemblies that opt in (`IsTrimmable`), leaving the
rest whole; `full` removes unreachable code across *all* assemblies. **`PublishAot=true` implies full trim** —
AOT must drop all unreachable code to emit a minimal native image, so "partial" isn't a real choice for the
AOT build. The framework is always fully trimmed; the only decision is **what to protect**.

**Two layers, both needed:**

1. **Source generators (official, do most of the work):** `x:Bind` is source-generated; classic `{Binding}` /
   `DisplayMemberPath` are made AOT-safe by marking the bound types `partial` + `[GeneratedBindableCustomProperty]`,
   and `CsWinRTAotOptimizerEnabled` emits the CCW/vtable code. This is the approach the
   [CsWinRT AOT docs](https://github.com/microsoft/CsWinRT/blob/master/docs/aot-trimming.md) prescribe, and
   it's what we use.

2. **Root the app assemblies (covers what the generators can't):**
   ```xml
   <ItemGroup Condition="'$(PublishAot)' == 'true'">
     <TrimmerRootAssembly Include="WinEnvEdit" />
     <TrimmerRootAssembly Include="WinEnvEdit.Core" />
   </ItemGroup>
   ```
   The generators cover *statically discoverable* binding. They do **not** cover types reached only
   *dynamically* — a code-hosted `DataTemplate` applied to a code-created `ListView`, converters/markup
   extensions resolved through XAML. We verified this: **removing the root and relying on the generators alone
   builds with zero trim warnings but breaks at runtime** (path rows render as `PathItemViewModel.ToString()` —
   the row template's machinery was trimmed). WinUI provides no documented per-type fix for this scenario, and
   per-type `[DynamicDependency]`/`TrimmerRootDescriptor` annotations would be whack-a-mole (every new
   converter is a silent-breakage risk). Rooting the two *small* app assemblies is the robust choice — it
   protects every XAML-reached type automatically, and costs almost nothing because the bulk of the bundle is
   the **native** WinUI runtime, which the trimmer doesn't touch.

`CsWinRTAotWarningLevel=2` keeps the build honest for layer 1: it surfaces every AOT/trim binding warning so an
unsafe pattern is caught at build time. (It does **not** catch the layer-2 dynamic cases — those have no
static signal, which is exactly why the root is required.)

---

## What does NOT work under AOT

| Technique | Symptom | Use instead |
|-----------|---------|-------------|
| `x:Bind` inside a `DataTemplate` / `ResourceDictionary` | binding never evaluates (getter never runs); no error, no warning | `{Binding}` on a `[GeneratedBindableCustomProperty]` type |
| `x:Load="{x:Bind ...}"` in a template | element never loads → nothing renders | `Visibility="{Binding ..., Converter=BoolToVisibility}"` |
| `DataTemplateSelector` | selector-applied template gets no per-template codegen; `x:Bind` dead and nested lists won't realize | one `DataTemplate` with `Visibility`-gated layout roots |
| Event handlers in a template (`Loaded`, `DataContextChanged`) | never fire — the template's connect code isn't generated | a binding to an attached `DependencyProperty` (callback is plain code) |
| `ListView`/`ItemsControl` **declared** inside a `DataTemplate` | collection is set, but **no item containers are generated** — zero rows | a **code-created** items host (see below) |
| Items host created while in a `Visibility=Collapsed` subtree | never realizes rows, even after it becomes visible | build it on the dispatcher queue *after* it is shown |
| `ResourceDictionary` indexer `this["Key"]` for a defined key | returns `null` (and ctor-time capture returns null) | walk `MergedDictionaries` at call time |
| `[RelayCommand]` on a `[GeneratedBindableCustomProperty]` VM | `MVVMTK0046` build error | declare commands manually as `IRelayCommand` |
| Reflection over `const`/static fields (e.g. `typeof(Glyph).GetField(name)`) | field trimmed → returns null/throws | compile-time `switch` over `nameof(...) => ...` |
| `MenuFlyout` in a `Style` setter (`ContextFlyout`) | flyout is not in the visual tree → its `{Binding}`s see a null DataContext | build the menu in code on `ContextRequested` |
| Collection expression passed to a WinRT API expecting `IList<T>` (e.g. `FileSavePicker.FileTypeChoices.Add(desc, [ext])`) | `InvalidCastException: Failed to create a CCW for object of type List\`1 ... IVector<string>` | use an explicit `new List<string> { ext }` — the CsWinRT optimizer only emits the CCW vtable for a recognizable object-creation expression |
| `WindowsAppSDKBootstrapInitialize=true` + self-contained | immediate native fail-fast `0xC0000602` in `CoreMessagingXP.dll` | set it `false` for self-contained builds |

**What still works:** classic `{Binding}` (incl. `{Binding Command}`) on
`[GeneratedBindableCustomProperty]` types; page/window-level `x:Bind`; `ContextRequested` and other
routed input events declared in a template; converters; a `ListView` created in **code**.

---

## The nested path-list: the hard case

A `PATH`-style variable expands to a reorderable list of per-path rows. That is **a list nested
inside the variable's item template** — the single worst case for WinUI AOT. Every declarative
approach failed silently. The working design composes three separate workarounds.

### 1. No selector — one template with `Visibility` gates

The variable item template is a single `DataTemplate` (`x:DataType="vm:VariableViewModel"`) whose
three layout roots are mutually-exclusive, gated by computed VM booleans:

```xml
<Border Visibility="{Binding ShowReadOnly, Converter={StaticResource BoolToVisibilityConverter}, Mode=OneWay}" .../>
<Border Visibility="{Binding ShowEditable, Converter={StaticResource BoolToVisibilityConverter}, Mode=OneWay}" .../>
<Border Visibility="{Binding ShowPathList, Converter={StaticResource BoolToVisibilityConverter}, Mode=OneWay}" .../>
```

```csharp
public bool ShowPathList => IsPathList;
public bool ShowReadOnly => !IsPathList && IsLocked;
public bool ShowEditable => !IsPathList && !IsLocked;
// IsPathList / IsLocked carry [NotifyPropertyChangedFor] for the three Show* so type-toggle is live.
```

`App.xaml` sets `ItemTemplate` (not `ItemTemplateSelector`).

### 2. Host the inner list from code, triggered by a binding

A `ListView` written in XAML inside the template never generates containers. A **code-created**
`ListView` does. But in-template events don't fire, so the trigger is a binding to an attached
`DependencyProperty`, and the build is deferred to the dispatcher so the `ListView` is created
*after* the expand makes the panel visible (a host built while collapsed never realizes):

```xml
<!-- host panel inside the (IsExpanded-gated) expander body -->
<StackPanel Grid.Row="0"
            res:VariableTemplates.PathExpanded="{Binding IsExpanded, Mode=OneWay}" />
```

```csharp
public static readonly DependencyProperty PathExpandedProperty =
  DependencyProperty.RegisterAttached("PathExpanded", typeof(bool), typeof(VariableTemplates),
    new PropertyMetadata(false, OnPathExpandedChanged));

private static void OnPathExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
  if (d is not Panel host || e.NewValue is not true) return;

  host.DispatcherQueue?.TryEnqueue(() => {              // run after the panel is visible
    pathItemTemplate ??= FindResource<DataTemplate>("PathItemTemplate");
    pathRowContainerStyle ??= FindResource<Style>("PathRowContainerStyle");

    if (host.Children.Count == 0) {
      host.Children.Add(new ListView {
        ItemTemplate = pathItemTemplate,               // keyed template still drives the rows
        ItemContainerStyle = pathRowContainerStyle,
        SelectionMode = ListViewSelectionMode.None,
        AllowDrop = true, CanDragItems = true, CanReorderItems = true,   // native reorder survives
        IsTabStop = false, Padding = new Thickness(0),
        TabFocusNavigation = KeyboardNavigationMode.Local,
      });
    }
    ((ListView)host.Children[0]).ItemsSource = (host.DataContext as VariableViewModel)?.PathItems;
  });
}
```

The `ListView`'s `ItemTemplate` is the keyed `PathItemTemplate` (all `{Binding}`; `PathItemViewModel`
is `[GeneratedBindableCustomProperty]`). Native `CanReorderItems` drag-reorder, add/remove, and
validation borders all work once the host is code-created.

### 3. Resolve keyed resources by walking merged dictionaries

`this["PathItemTemplate"]` returns `null` under AOT (even in the dictionary that defines the key,
even after `InitializeComponent`). The indexer does not search `MergedDictionaries`. Resolve at call
time and cache:

```csharp
private static T? FindResource<T>(string key) where T : class =>
  FindIn(Application.Current.Resources, key) as T;

private static object? FindIn(ResourceDictionary dict, string key) {
  if (dict.TryGetValue(key, out var value)) return value;
  foreach (var merged in dict.MergedDictionaries)
    if (FindIn(merged, key) is { } found) return found;
  return null;
}
```

---

## Approaches tried for the nested list (in order) — all failed before the code host

1. `x:Bind ItemsSource` on the nested `ListView` → getter never runs (proved with an `XBindProbe`).
2. Keyed `PathItemTemplate` instead of inline → no change.
3. `partial` `DataTemplateSelector` (fixes the selector *crash*, issues #10302/#10310) → does not
   revive `x:Bind`/realization.
4. `{Binding} ItemsSource` on the virtualizing `ListView` → collection bound, **zero rows**.
5. Single template + `x:Load="{x:Bind Show*}"` gates → whole item renders nothing (`x:Bind` dead → all gates false).
6. Switch gates + all template bindings to `{Binding}` → outer card works again; inner list still empty.
7. Non-virtualizing `ItemsControl` (inline template, then explicit `ControlTemplate`+`ItemsPanel`,
   then a **literal-text** template) → still zero rows ⇒ it is container *generation*, not data or bindings.
8. **Code-created `ListView`** → rows realize, and its mere presence even revived the XAML hosts
   (confirming the machinery was trimmed for lack of a static reference). This became the basis of the fix.

Then, making the code host production-ready surfaced three more layers: in-template `Loaded`/
`DataContextChanged` never fire (→ attached-property binding trigger); a host built while collapsed
stays empty (→ dispatcher build on expand); `this[key]` returns null (→ merged-tree resolver).

---

## Diagnostic method (how to bisect silent AOT failures)

Because nothing throws, instrument and split the problem in the **running AOT build**:

1. **Force-visible** a gated element to separate a toggle/visibility bug from a content bug.
2. A **literal marker** (`<TextBlock Text="…"/>`) proves a panel renders at all.
3. A **top-level count** property (`PathCount`, not nested `PathItems.Count` — nested paths can fail
   AOT reflection) proves the collection is populated.
4. A **literal-text item template** separates container generation from data binding.
5. A **code-created control** separates "XAML-declared codegen missing" from "feature truly unsupported."
6. Log to a file from code-behind (events/callbacks fire silently) to confirm a handler runs and with
   what values. Remove the logging before shipping.

A `*` `RowDefinition` nested inside an `Auto`-measured parent collapses to **0 height** — verify
layout (use `Auto`) before blaming AOT for "invisible" content.

---

## Related

- `docs/xaml.md` — general WinUI/XAML patterns. **Note:** its *DataTemplateSelector* and
  *Style-setter context menu* sections are JIT-era; under AOT use the single-template + code-behind
  patterns above.
- `docs/deployment.md` — packaging, runtime dependencies, MSI size.
- `WinEnvEdit/Resources/VariableTemplates.xaml(.cs)` — the working implementation.
