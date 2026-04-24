using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using WinEnvEdit.ViewModels;

namespace WinEnvEdit.Selectors;

// partial is required: C#/WinRT interop crashes in Release without it. See PATTERNS.md → DataTemplateSelector Pattern.
public partial class VariableTemplateSelector : DataTemplateSelector {
  public DataTemplate? ReadOnlyTemplate { get; set; }
  public DataTemplate? EditableTemplate { get; set; }
  public DataTemplate? PathListTemplate { get; set; }

  protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container) {
    if (item is not VariableViewModel variable) {
      return base.SelectTemplateCore(item, container);
    }

    if (variable.IsPathList) {
      return PathListTemplate;
    }

    if (variable.IsLocked) {
      return ReadOnlyTemplate;
    }

    return EditableTemplate;
  }
}
