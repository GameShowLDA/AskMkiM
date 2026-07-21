using System.Globalization;
using Ask.Core.Shared.Metadata.Enums.UnitEnums;
using Ask.Engine.Tests.MethodExecutor;
using Ask.Engine.Tests.Protocol;

namespace Ask.Engine.UnitTests.Tests.MethodExecutor;

public class GroupMethodProtocolBuilderTests
{
  [Fact]
  public void BuildFailure_UsesDischargeDataAndUnitMetadata()
  {
    var previousCulture = CultureInfo.CurrentCulture;

    try
    {
      CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ru-RU");

      var result = GroupMethodProtocolBuilder.BuildFailure(
        0,
        "0000001",
        10,
        7.35,
        ResistanceUnit.MegaOhm,
        MeasurementLimitKind.Minimum);

      Assert.Equal("Разряд-0[0000001] (10<R МОм). Rизм = 7,35 МОм", result);
    }
    finally
    {
      CultureInfo.CurrentCulture = previousCulture;
    }
  }

  [Fact]
  public void BuildFailure_UsesMaximumCurrentFromUnitMetadata()
  {
    var previousCulture = CultureInfo.CurrentCulture;

    try
    {
      CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ru-RU");

      var result = GroupMethodProtocolBuilder.BuildFailure(
        2,
        "0000100",
        10,
        12.35,
        CurrentUnit.MilliAmpere,
        MeasurementLimitKind.Maximum);

      Assert.Equal("Разряд-2[0000100] (I<10 мА). Iизм = 12,35 мА", result);
    }
    finally
    {
      CultureInfo.CurrentCulture = previousCulture;
    }
  }
}
