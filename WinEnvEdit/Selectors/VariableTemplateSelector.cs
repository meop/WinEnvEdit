using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using WinEnvEdit.ViewModels;

namespace WinEnvEdit.Selectors;

public partial class VariableTemplateSelector : DataTemplateSelector {
  public DataTemplate? ReadOnlyTemplate { get; set; }
  public DataTemplate? EditableTemplate { get; set; }
  public DataTemplate? PathListTemplate { get; set; }

  protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container) {
    if (item is not VariableViewModel variable) {
      return base.SelectTemplateCore(item, container);
    }

    if (variable.IsLocked) {
      return ReadOnlyTemplate;
    }

    if (variable.IsPathList) {
      return PathListTemplate;
    }

    return EditableTemplate;
  }
}
