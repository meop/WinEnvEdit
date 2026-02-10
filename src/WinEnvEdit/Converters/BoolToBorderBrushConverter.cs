using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace WinEnvEdit.Converters;

public partial class BoolToBorderBrushConverter : IValueConverter {
  public bool Invert { get; set; }

  public object Convert(object value, Type targetType, object parameter, string language) {
    if (value is not bool boolValue) {
      return GetDefaultBrush();
    }

    var shouldWarn = Invert ? !boolValue : boolValue;
    if (shouldWarn) {
      return Application.Current?.Resources["SystemFillColorCautionBrush"] as SolidColorBrush
        ?? new SolidColorBrush(Colors.Coral);
    }
    return GetDefaultBrush();
  }

  private static object GetDefaultBrush() =>
    Application.Current?.Resources["TextControlBorderBrush"] as SolidColorBrush
      ?? new SolidColorBrush(Colors.Transparent);

  public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

public partial class BoolToBorderBrushInverseConverter : BoolToBorderBrushConverter {
  public BoolToBorderBrushInverseConverter() => Invert = true;
}
