using System.Globalization;
using Ask.Engine.Tests.MethodExecutor.CI;

namespace Ask.Engine.UnitTests.Tests.MethodExecutor.CI;

public class CiGroupMethodExecutorTests
{
  [Fact]
  public void BuildExecutionErrorMessage_ReturnsDescriptionForDischarge()
  {
    var previousCulture = CultureInfo.CurrentCulture;

    try
    {
      CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ru-RU");

      var result = CiGroupMethodExecutor.BuildExecutionErrorMessage(0, "0000001", 10, 7.35);

      Assert.Equal("Разряд-0[0000001] (10<R МОм). Rизм = 7,35 МОм", result);
    }
    finally
    {
      CultureInfo.CurrentCulture = previousCulture;
    }
  }
}
