using System;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace WinEnvEdit.Converters;

public class BoolToVisibilityConverter : IValueConverter {
  public object Convert(object value, Type targetType, object parameter, string language) {
    return value is bool boolValue && boolValue ? Visibility.Visible : Visibility.Collapsed;
  }

  public object ConvertBack(object value, Type targetType, object parameter, string language) {
    return value is Visibility visibility && visibility == Visibility.Visible;
  }
}

public class BoolToVisibilityInverseConverter : IValueConverter {
  public object Convert(object value, Type targetType, object parameter, string language) {
    return value is bool boolValue && boolValue ? Visibility.Collapsed : Visibility.Visible;
  }

  public object ConvertBack(object value, Type targetType, object parameter, string language) {
    return value is Visibility visibility && visibility == Visibility.Collapsed;
  }
}
