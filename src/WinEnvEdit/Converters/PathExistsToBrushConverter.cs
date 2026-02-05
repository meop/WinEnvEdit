using System;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace WinEnvEdit.Converters;

public partial class PathExistsToBrushConverter : IValueConverter {
  public object Convert(object value, Type targetType, object parameter, string language) {
    if (value is bool exists && !exists) {
      // Use WinUI system resource for critical/error state
      return Application.Current?.Resources["SystemFillColorCriticalBrush"] as SolidColorBrush ?? new SolidColorBrush(Microsoft.UI.Colors.Red);
    }
    // Transparent for valid paths (no bottom border)
    return new SolidColorBrush(Microsoft.UI.Colors.Transparent);
  }

  public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}
