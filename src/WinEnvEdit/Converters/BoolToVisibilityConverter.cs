using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace WinEnvEdit.Converters;

public partial class BoolToVisibilityConverter : IValueConverter {
  public bool Invert { get; set; }

  public object Convert(object value, Type targetType, object parameter, string language) {
    if (value is not bool boolValue) {
      return Visibility.Collapsed;
    }

    var shouldBeVisible = Invert ? !boolValue : boolValue;
    return shouldBeVisible ? Visibility.Visible : Visibility.Collapsed;
  }

  public object ConvertBack(object value, Type targetType, object parameter, string language) {
    if (value is not Visibility visibility) {
      return false;
    }

    var isVisible = visibility == Visibility.Visible;
    return Invert ? !isVisible : isVisible;
  }
}

public partial class BoolToVisibilityInverseConverter : BoolToVisibilityConverter {
  public BoolToVisibilityInverseConverter() => Invert = true;
}
