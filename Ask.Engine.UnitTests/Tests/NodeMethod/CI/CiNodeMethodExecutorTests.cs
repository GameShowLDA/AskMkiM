using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Engine.Tests.NodeMethod.CI;
using System.Globalization;
using static Ask.Engine.Tests.Base.UIValidationHelper;

namespace Ask.Engine.UnitTests.Tests.NodeMethod.CI;

public sealed class CiNodeMethodExecutorTests
{
  [Fact(DisplayName = "Ошибка СИ содержит точку, напряжение, норму и результат")]
  public void BuildExecutionErrorMessage_ReturnsCompleteDescription()
  {
    var previousCulture = CultureInfo.CurrentCulture;
    CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ru-RU");

    try
    {
      var point = new PointModel
      {
        DeviceNumber = 1,
        ModuleNumber = 2,
        PointNumber = 20,
      };
      var dataModel = new DataModel
      {
        Voltage = 100,
        Param = 10,
      };

      var result = CiNodeMethodExecutor.BuildExecutionErrorMessage(point, dataModel, 7.35);

      Assert.Equal(
        "Точка[1.2.20](10<R МОм). Rизм = 7,35 МОм",
        result);
    }
    finally
    {
      CultureInfo.CurrentCulture = previousCulture;
    }
  }
}
