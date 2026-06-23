using System.Collections.Specialized;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

using WinEnvEdit.ViewModels;

namespace WinEnvEdit.Resources;

public partial class VariableTemplates : ResourceDictionary {
  private static DataTemplate? pathItemTemplate;
  private static Style? pathRowContainerStyle;

  public VariableTemplates() => InitializeComponent();

  private static T? FindResource<T>(string key) where T : class =>
    FindIn(Application.Current.Resources, key) as T;

  private static object? FindIn(ResourceDictionary dict, string key) {
    if (dict.TryGetValue(key, out var value)) {
      return value;
    }

    foreach (var merged in dict.MergedDictionaries) {
      if (FindIn(merged, key) is { } found) {
        return found;
      }
    }

    return null;
  }

  // Path rows are hosted in code: a binding to IsExpanded triggers building the list on the dispatcher.
  public static readonly DependencyProperty PathExpandedProperty =
    DependencyProperty.RegisterAttached(
      "PathExpanded",
      typeof(bool),
      typeof(VariableTemplates),
      new PropertyMetadata(false, OnPathExpandedChanged));

  public static bool GetPathExpanded(DependencyObject obj) => (bool)obj.GetValue(PathExpandedProperty);
  public static void SetPathExpanded(DependencyObject obj, bool value) => obj.SetValue(PathExpandedProperty, value);

  private static void OnPathExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
    if (d is not Panel host || e.NewValue is not true) {
      return;
    }

    host.DispatcherQueue?.TryEnqueue(() => {
      pathItemTemplate ??= FindResource<DataTemplate>("PathItemTemplate");
      pathRowContainerStyle ??= FindResource<Style>("PathRowContainerStyle");

      if (host.Children.Count == 0) {
        var list = new ListView {
          ItemTemplate = pathItemTemplate,
          ItemContainerStyle = pathRowContainerStyle,
          SelectionMode = ListViewSelectionMode.None,
          AllowDrop = true,
          CanDragItems = true,
          CanReorderItems = true,
          IsTabStop = false,
          Padding = new Thickness(0),
          TabFocusNavigation = KeyboardNavigationMode.Local,
          // Off by default it's true: when the rows repopulate (e.g. a type-toggle undo re-parses the list),
          // placeholder gray boxes paint over the card for a frame before the real rows render — the flash.
          ShowsScrollingPlaceholders = false,
        };
        host.Children.Add(list);

        // A reorder is a Remove+Insert pair; bracket it so it lands as a single undo step instead of
        // recording the path-missing intermediate. DataContext is read at event time so a recycled host
        // brackets whichever variable it now hosts.
        list.DragItemsStarting += (_, _) => (host.DataContext as VariableViewModel)?.BeginPathReorder();
        list.DragItemsCompleted += (_, _) => (host.DataContext as VariableViewModel)?.EndPathReorder();

        // The outer ListView recycles this host onto other variables, and Undo/Refresh recreate the VM in
        // place — but this expand handler won't fire again. Re-point ItemsSource + the add-focus subscription
        // on every DataContext change so the rows track the current variable. (A code {Binding} on ItemsSource
        // silently no-ops under AOT, so the assignment must stay imperative.)
        host.DataContextChanged += (_, args) => SyncHostedList(list, args.NewValue as VariableViewModel);
      }

      SyncHostedList((ListView)host.Children[0], host.DataContext as VariableViewModel);
    });
  }

  private static void SyncHostedList(ListView list, VariableViewModel? vm) {
    list.ItemsSource = vm?.PathItems;
    WireAddFocus(list, vm);
  }

  // Tracks which PathItems collection the add-focus handler is subscribed to, so it can be detached when the
  // host is recycled onto a different variable.
  private sealed record AddFocusSubscription(INotifyCollectionChanged Collection, NotifyCollectionChangedEventHandler Handler);

  private static void WireAddFocus(ListView list, VariableViewModel? vm) {
    var collection = vm?.PathItems as INotifyCollectionChanged;

    if (list.Tag is AddFocusSubscription existing) {
      if (ReferenceEquals(existing.Collection, collection)) {
        return;
      }
      existing.Collection.CollectionChanged -= existing.Handler;
      list.Tag = null;
    }

    if (collection is null) {
      return;
    }

    void Handler(object? sender, NotifyCollectionChangedEventArgs e) => OnPathItemsChanged(list, e);
    collection.CollectionChanged += Handler;
    list.Tag = new AddFocusSubscription(collection, Handler);
  }

  // Focus the textbox of a freshly added (empty) path row so a "+" add is immediately editable. A parse adds
  // only non-empty rows, so a single empty addition uniquely identifies the user's add action.
  private static void OnPathItemsChanged(ListView list, NotifyCollectionChangedEventArgs e) {
    if (e.Action != NotifyCollectionChangedAction.Add || e.NewItems is not { Count: 1 }) {
      return;
    }

    if (e.NewItems[0] is not PathItemViewModel { PathValue: "" } added) {
      return;
    }

    list.DispatcherQueue?.TryEnqueue(() => {
      list.UpdateLayout(); // realize the new container before locating its textbox
      if (list.ContainerFromItem(added) is ListViewItem container && FindDescendant<TextBox>(container) is { } box) {
        box.Focus(FocusState.Programmatic);
      }
    });
  }

  private static T? FindDescendant<T>(DependencyObject root) where T : class {
    var count = VisualTreeHelper.GetChildrenCount(root);
    for (var i = 0; i < count; i++) {
      var child = VisualTreeHelper.GetChild(root, i);
      if (child is T match) {
        return match;
      }
      if (FindDescendant<T>(child) is { } nested) {
        return nested;
      }
    }
    return null;
  }
}
