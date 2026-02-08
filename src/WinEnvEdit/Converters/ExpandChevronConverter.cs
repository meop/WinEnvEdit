using System;

using Microsoft.UI.Xaml.Data;

using WinEnvEdit.Core.Constants;

namespace WinEnvEdit.Converters;

public partial class ExpandChevronConverter : IValueConverter {
  public object Convert(object value, Type targetType, object parameter, string language) => value is bool isExpanded && isExpanded ? Glyph.ChevronUp : Glyph.ChevronDown;

  public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}
