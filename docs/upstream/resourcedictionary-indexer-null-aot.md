# ResourceDictionary indexer returns null for a defined key under AOT (needs confirmation)

- **Target repo:** microsoft-ui-xaml (or CsWinRT) — TBD after confirmation
- **Kind:** needs-confirmation report
- **Confidence:** Low-Medium (observed, but may be a usage/timing artifact)

## Summary

Observed under AOT: inside a code-behind `ResourceDictionary` (x:Class), `this["SomeKey"]` returned **null**
for a key that **is** defined in that dictionary's XAML — including when captured in the constructor right
after `InitializeComponent()`. Resolving the same key by **walking `MergedDictionaries` at call time**
returned it correctly.

This one is **lower confidence** — it may be a timing/lifecycle nuance (resource not yet realized at ctor
time), a load-order issue, or genuinely an AOT codegen gap. It needs an isolated repro before filing.

## Workaround in WinEnvEdit

A small recursive lookup that walks `Application.Current.Resources.MergedDictionaries` and caches the result:
`WinEnvEdit/Resources/VariableTemplates.xaml.cs` → `FindResource<T>` / `FindIn`.

## Before filing

1. Build a minimal repro: a merged x:Class `ResourceDictionary` with a keyed `DataTemplate`; in its ctor (and
   later) read `this["Key"]`; compare JIT vs AOT and ctor-time vs after-load.
2. If it reproduces only at ctor time, it's likely lifecycle (document as guidance, not a bug).
3. If `this[key]` returns null after load under AOT but not JIT, file it (microsoft-ui-xaml) with the repro.

## The ask (if confirmed)

Make the `ResourceDictionary` indexer resolve XAML-defined keys under AOT consistently with JIT, or document
the lookup-by-merged-dictionaries requirement.
