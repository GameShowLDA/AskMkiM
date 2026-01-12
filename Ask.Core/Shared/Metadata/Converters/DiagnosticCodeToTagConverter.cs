using Ask.Core.Services.Errors.Models;
using System.Globalization;
using System.Windows.Data;

namespace Ask.Core.Shared.Metadata.Converters
{
  /// <summary>
  /// Универсальный конвертер для получения тега предупреждения или ошибки.
  /// Принимает:
  /// - ErrorCode
  /// - WarningCode
  /// - string (CodeString)
  /// - IDiagnosticItem
  /// и возвращает строковой тег (атака TRN001, WARNGEN005).
  /// </summary
  public class DiagnosticCodeToTagConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is null)
        return string.Empty;

      if (value is IDisplayIssue item)
      {
        return item.CodeString ?? string.Empty;
      }

      if (value is ErrorCode errCode)
      {
        return errCode.GetTag() ?? string.Empty;
      }

      if (value is WarningCode warnCode)
      {
        return warnCode.GetTag() ?? string.Empty;
      }

      if (value is string str)
      {
        return str;
      }

      return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
