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
        };
        host.Children.Add(list);

        if ((host.DataContext as VariableViewModel)?.PathItems is INotifyCollectionChanged items) {
          items.CollectionChanged += (_, e) => OnPathItemsChanged(list, e);
        }
      }

      ((ListView)host.Children[0]).ItemsSource = (host.DataContext as VariableViewModel)?.PathItems;
    });
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
