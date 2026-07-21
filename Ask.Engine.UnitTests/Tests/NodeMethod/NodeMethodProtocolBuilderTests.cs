using System.Globalization;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.Metadata.Enums.UnitEnums;
using Ask.Engine.Tests.NodeMethod;

namespace Ask.Engine.UnitTests.Tests.NodeMethod;

public class NodeMethodProtocolBuilderTests
{
  [Fact]
  public void BuildFailure_UsesMinimumResistanceFromUnitMetadata()
  {
    RunWithRussianCulture(() =>
    {
      var point = new PointModel
      {
        DeviceNumber = 1,
        ModuleNumber = 2,
        PointNumber = 20,
      };

      var result = NodeMethodProtocolBuilder.BuildFailure(
        point,
        10,
        7.35,
        ResistanceUnit.MegaOhm,
        MeasurementLimitKind.Minimum);

      Assert.Equal("Точка[1.2.20](10<R МОм). Rизм = 7,35 МОм", result);
    });
  }

  [Fact]
  public void BuildFailure_UsesMaximumCurrentFromUnitMetadata()
  {
    RunWithRussianCulture(() =>
    {
      var point = new PointModel
      {
        DeviceNumber = 1,
        ModuleNumber = 2,
        PointNumber = 20,
      };

      var result = NodeMethodProtocolBuilder.BuildFailure(
        point,
        10,
        12.35,
        CurrentUnit.MilliAmpere,
        MeasurementLimitKind.Maximum);

      Assert.Equal("Точка[1.2.20](I<10 мА). Iизм = 12,35 мА", result);
    });
  }

  private static void RunWithRussianCulture(Action action)
  {
    var previousCulture = CultureInfo.CurrentCulture;

    try
    {
      CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ru-RU");
      action();
    }
    finally
    {
      CultureInfo.CurrentCulture = previousCulture;
    }
  }
}
