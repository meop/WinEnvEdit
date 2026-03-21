# MVVM & ViewModel Patterns

---

## ObservableProperty Initialization

Initialize complex `[ObservableProperty]` types in the constructor, not inline.

**Problem:** Initializing `ObservableCollection` or properties with `[NotifyPropertyChangedFor]` dependencies inline causes `NullReferenceException` during WinUI 3 source generation — property change handlers fire before dependent objects are instantiated.

```csharp
[ObservableProperty]
public partial string Name { get; set; } = string.Empty; // Simple types: safe inline

[ObservableProperty]
public partial ObservableCollection<Item> Items { get; set; }

public MyViewModel() {
    Items = []; // Collections and NotifyPropertyChangedFor dependencies: constructor only
}
```

**Rule:** Simple types (`bool`, `int`, `string`) are safe inline. Collections and objects with `[NotifyPropertyChangedFor]` must use constructor initialization.

---

## Incremental List Reconciliation

Update `ObservableCollection` items incrementally instead of clearing and rebuilding.

**Problem:** `Clear()` followed by re-adding items causes UI flicker and loses scroll position. $O(N^2)$ reconciliation with `IndexOf`/`Contains` in a loop is slow for large lists.

**Solution:** $O(N)$ reconciliation with a `HashSet`, and a fast path for bulk updates:

```csharp
public void UpdateList(List<T> targetList, T? changedItem = null) {
  // Fast Path: Bulk update (initial load, search)
  if (changedItem == null) {
    if (!Collection.SequenceEqual(targetList)) {
      Collection.Clear();
      foreach (var item in targetList) Collection.Add(item);
    }
    return;
  }

  // Incremental Path: Targeted update
  var targetSet = new HashSet<T>(targetList);
  // 1. Remove missing items
  // 2. Move/insert to match order
  // 3. Force refresh of changedItem via indexer: Collection[i] = item
}
```

`Collection[i] = item` forces WinUI to re-evaluate the DataTemplate — useful for type toggles.

---

## Background System Notification

Always run `WM_SETTINGCHANGE` broadcasts on a background thread.

**Problem:** `SendMessageTimeout` for environment change notifications is synchronous and can block for several seconds if other apps are slow to respond, freezing the UI after every Save.

```csharp
public async Task SaveAsync(IEnumerable<T> items) {
  await Task.Run(() => PerformSave(items));
  await Task.Run(() => NotifySystemOfChanges()); // SendMessageTimeout on background thread
}
```
