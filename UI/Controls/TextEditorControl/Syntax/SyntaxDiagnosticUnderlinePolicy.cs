using Ask.Core.Services.Config.Base;

namespace UI.Controls.TextEditorControl.Syntax
{
  /// <summary>
  /// Определяет, какие диагностики синтаксиса разрешено подчёркивать
  /// текущими настройками пользовательского интерфейса.
  /// </summary>
  public static class SyntaxDiagnosticUnderlinePolicy
  {
    public static bool HasEnabledUnderlines()
    {
      return UserInterfaceConfig.GetWarningUnderlineHighlighting()
        || UserInterfaceConfig.GetErrorUnderlineHighlighting();
    }

    public static bool IsEnabled(TextSyntaxDiagnostic diagnostic)
    {
      ArgumentNullException.ThrowIfNull(diagnostic);

      return diagnostic.Severity == TextSyntaxSeverity.Warning
        ? UserInterfaceConfig.GetWarningUnderlineHighlighting()
        : UserInterfaceConfig.GetErrorUnderlineHighlighting();
    }
  }
}
