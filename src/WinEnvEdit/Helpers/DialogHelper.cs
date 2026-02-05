using System.Collections.Generic;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WinEnvEdit.Helpers;

/// <summary>
/// Helper methods for creating consistent dialog UI elements
/// </summary>
public static class DialogHelper {
  /// <summary>
  /// Creates a grid with a label in column 0 and a control in column 1
  /// </summary>
  /// <param name="labelText">The label text</param>
  /// <param name="control">The control to place in column 1</param>
  /// <param name="labelWidth">Width of the label column (default 100)</param>
  /// <param name="labelAlignment">Vertical alignment of the label (default Center)</param>
  /// <returns>A configured Grid with two columns</returns>
  public static Grid CreateLabelValueGrid(string labelText, FrameworkElement control, int labelWidth = 100, VerticalAlignment labelAlignment = VerticalAlignment.Center) {
    var label = CreateDialogLabel(labelText, labelAlignment);
    Grid.SetColumn(label, 0);
    Grid.SetColumn(control, 1);

    return new Grid {
      ColumnDefinitions = {
        new ColumnDefinition { Width = new GridLength(labelWidth) },
        new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
      },
      Children = {
        label,
        control,
      },
    };
  }

  /// <summary>
  /// Creates a TextBlock styled as a dialog label
  /// </summary>
  /// <param name="text">The label text</param>
  /// <param name="alignment">Vertical alignment (default Center)</param>
  /// <returns>A configured TextBlock</returns>
  public static TextBlock CreateDialogLabel(string text, VerticalAlignment alignment = VerticalAlignment.Center) {
    return new TextBlock {
      Text = text,
      Style = Application.Current.Resources["DialogLabelStyle"] as Style,
      VerticalAlignment = alignment,
    };
  }

  /// <summary>
  /// Creates a TextBox styled for use in dialogs
  /// </summary>
  /// <param name="initialText">Initial text value (default empty)</param>
  /// <param name="acceptsReturn">Whether the TextBox accepts return key (default false)</param>
  /// <returns>A configured TextBox</returns>
  public static TextBox CreateDialogTextBox(string initialText = "", bool acceptsReturn = false) {
    return new TextBox {
      Text = initialText,
      TextWrapping = TextWrapping.Wrap,
      AcceptsReturn = acceptsReturn,
      Style = Application.Current.Resources["DialogTextBoxStyle"] as Style,
    };
  }

  /// <summary>
  /// Creates a TextBlock styled for displaying values in dialogs
  /// </summary>
  /// <param name="text">The text to display</param>
  /// <param name="alignment">Vertical alignment (default Center)</param>
  /// <param name="isSelectable">Whether text can be selected (default true)</param>
  /// <returns>A configured TextBlock</returns>
  public static TextBlock CreateDialogValue(string text, VerticalAlignment alignment = VerticalAlignment.Center, bool isSelectable = true) {
    return new TextBlock {
      Text = text,
      TextWrapping = TextWrapping.Wrap,
      IsTextSelectionEnabled = isSelectable,
      VerticalAlignment = alignment,
    };
  }

  /// <summary>
  /// Creates a StackPanel configured with standard dialog content styling.
  /// </summary>
  public static StackPanel CreateDialogPanel(IEnumerable<FrameworkElement>? children = null) {
    var panel = new StackPanel {
      Style = Application.Current.Resources["DialogContentPanel"] as Style,
      IsTabStop = true,
    };

    if (children != null) {
      foreach (var child in children) {
        panel.Children.Add(child);
      }
    }

    return panel;
  }

  /// <summary>
  /// Creates a ContentDialog with standard application styling and title centering.
  /// </summary>
  public static ContentDialog CreateStandardDialog(XamlRoot xamlRoot, string title, object content, string primaryButtonText = "", string closeButtonText = "Cancel") {
    return new ContentDialog {
      Title = title,
      TitleTemplate = Application.Current.Resources["CenteredTitleTemplate"] as DataTemplate,
      Content = content,
      PrimaryButtonText = primaryButtonText,
      CloseButtonText = closeButtonText,
      DefaultButton = string.IsNullOrEmpty(primaryButtonText) ? ContentDialogButton.Close : ContentDialogButton.Primary,
      Style = Application.Current.Resources["StandardContentDialogStyle"] as Style,
      XamlRoot = xamlRoot,
    };
  }
}
