using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace UI.Converter
{
  public sealed class ToolTipPartConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      string parameterText = parameter as string ?? string.Empty;
      string? text = value switch
      {
        null => null,
        string s => s,
        FrameworkElement element when element.ToolTip is string tooltip => tooltip,
        _ => value.ToString(),
      };

      SplitTooltip(text, out string title, out string hotkey);

      return parameterText switch
      {
        "Hotkey" => hotkey,
        "HotkeyVisibility" => string.IsNullOrWhiteSpace(hotkey) ? Visibility.Collapsed : Visibility.Visible,
        _ => string.IsNullOrWhiteSpace(title) ? text ?? string.Empty : title,
      };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotSupportedException();
    }

    private static void SplitTooltip(string? text, out string title, out string hotkey)
    {
      title = text ?? string.Empty;
      hotkey = string.Empty;

      if (string.IsNullOrWhiteSpace(text))
      {
        return;
      }

      string[] separators = [" — ", " вЂ” ", " - "];
      foreach (string separator in separators)
      {
        int index = text.LastIndexOf(separator, StringComparison.Ordinal);
        if (index < 0)
        {
          continue;
        }

        string candidate = text[(index + separator.Length)..].Trim();
        if (!LooksLikeHotkey(candidate))
        {
          continue;
        }

        title = text[..index].Trim();
        hotkey = candidate;
        return;
      }
    }

    private static bool LooksLikeHotkey(string text)
    {
      if (string.IsNullOrWhiteSpace(text))
      {
        return false;
      }

      return text.Contains('+', StringComparison.Ordinal) ||
             text.Contains("Ctrl", StringComparison.OrdinalIgnoreCase) ||
             text.Contains("Alt", StringComparison.OrdinalIgnoreCase) ||
             text.Contains("Shift", StringComparison.OrdinalIgnoreCase) ||
             text.Contains('⌘');
    }
  }
}
