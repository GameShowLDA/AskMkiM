using Ask.Core.Shared.DTO.Devices.Base;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Static;

namespace Ask.Engine.UnitTests.Tests.Base;

public class MeasurementTestDataTests
{
  [Fact]
  public void SeparateSaveMethods_PreserveValuesOfOtherInputMode()
  {
    MeasurementTestData.SaveMeasurementData(new DataModel(
      new PointModel { DeviceNumber = 1, ModuleNumber = 2, PointNumber = 3 },
      new PointModel { DeviceNumber = 4, ModuleNumber = 5, PointNumber = 6 },
      7.5));

    MeasurementTestData.SaveModuleTestData(new DataModel
    {
      TestedNumber = "7.8",
      TesterNumber = "9.10",
      TestRange = "1-3,5"
    });

    DataModel result = MeasurementTestData.GetData();

    Assert.Equal("1.2.3", result.FirstPoint.ToString());
    Assert.Equal("4.5.6", result.SecondPoint.ToString());
    Assert.Equal(7.5, result.Param);
    Assert.Equal("7.8", result.TestedNumber);
    Assert.Equal("9.10", result.TesterNumber);
    Assert.Equal("1-3,5", result.TestRange);
  }

  [Fact]
  public void SaveMeasurementData_UpdatesOnlyIncludedOptionalFields()
  {
    MeasurementTestData.SaveMeasurementData(
      new DataModel(
        new PointModel { DeviceNumber = 10, ModuleNumber = 11, PointNumber = 12 },
        new PointModel { DeviceNumber = 13, ModuleNumber = 14, PointNumber = 15 },
        16)
      {
        ActiveBus = BusPoint.B,
        ActivePairBus = SwitchingBusNew.AB3
      },
      includeBus: true,
      includeBusGroup: true);

    MeasurementTestData.SaveMeasurementData(
      new DataModel(
        new PointModel { DeviceNumber = 17, ModuleNumber = 18, PointNumber = 19 },
        new PointModel { DeviceNumber = 20, ModuleNumber = 21, PointNumber = 22 },
        23)
      {
        ActiveBus = BusPoint.A,
        ActivePairBus = SwitchingBusNew.AB1
      });

    DataModel result = MeasurementTestData.GetData();

    Assert.Equal(BusPoint.B, result.ActiveBus);
    Assert.Equal(SwitchingBusNew.AB3, result.ActivePairBus);

    MeasurementTestData.SaveMeasurementData(
      new DataModel(
        new PointModel { DeviceNumber = 24, ModuleNumber = 25, PointNumber = 26 },
        new PointModel { DeviceNumber = 27, ModuleNumber = 28, PointNumber = 29 },
        30)
      {
        ActiveBus = BusPoint.A,
        ActivePairBus = SwitchingBusNew.AB1
      },
      includeBus: true,
      includeBusGroup: true);

    result = MeasurementTestData.GetData();

    Assert.Equal(BusPoint.A, result.ActiveBus);
    Assert.Equal(SwitchingBusNew.AB1, result.ActivePairBus);
  }

  [Fact]
  public void GetData_ReturnsPointCopies()
  {
    MeasurementTestData.SaveMeasurementData(new DataModel(
      new PointModel { DeviceNumber = 20, ModuleNumber = 21, PointNumber = 22 },
      new PointModel { DeviceNumber = 23, ModuleNumber = 24, PointNumber = 25 },
      26));

    DataModel firstRead = MeasurementTestData.GetData();
    firstRead.FirstPoint.PointNumber = 999;

    DataModel secondRead = MeasurementTestData.GetData();

    Assert.Equal(22, secondRead.FirstPoint.PointNumber);
  }
}
