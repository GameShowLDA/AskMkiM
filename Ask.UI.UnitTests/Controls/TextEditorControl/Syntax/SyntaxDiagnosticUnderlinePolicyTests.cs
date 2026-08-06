using Ask.Core.Services.Config.Base;
using UI.Controls.TextEditorControl.Syntax;

namespace Ask.UI.UnitTests.Controls.TextEditorControl.Syntax;

public sealed class SyntaxDiagnosticUnderlinePolicyTests
{
  [Fact]
  public void DisabledUnderlineSettings_DisableWarningsAndErrors()
  {
    bool previousWarningSetting = UserInterfaceConfig.GetWarningUnderlineHighlighting();
    bool previousErrorSetting = UserInterfaceConfig.GetErrorUnderlineHighlighting();

    try
    {
      UserInterfaceConfig.SetWarningUnderlineHighlighting(false);
      UserInterfaceConfig.SetErrorUnderlineHighlighting(false);

      Assert.False(SyntaxDiagnosticUnderlinePolicy.HasEnabledUnderlines());
      Assert.False(SyntaxDiagnosticUnderlinePolicy.IsEnabled(CreateDiagnostic(TextSyntaxSeverity.Warning)));
      Assert.False(SyntaxDiagnosticUnderlinePolicy.IsEnabled(CreateDiagnostic(TextSyntaxSeverity.Error)));
    }
    finally
    {
      UserInterfaceConfig.SetWarningUnderlineHighlighting(previousWarningSetting);
      UserInterfaceConfig.SetErrorUnderlineHighlighting(previousErrorSetting);
    }
  }

  private static TextSyntaxDiagnostic CreateDiagnostic(TextSyntaxSeverity severity)
  {
    return new TextSyntaxDiagnostic
    {
      Severity = severity
    };
  }
}
