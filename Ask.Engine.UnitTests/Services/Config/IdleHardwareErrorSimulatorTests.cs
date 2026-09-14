using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.DTO.Devices.Base;
using Ask.Core.Shared.DTO.Devices.ChassisManager;
using Ask.Core.Shared.DTO.Settings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.DataBase.Provider.Context;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Ask.Engine.UnitTests.Services.Config;

[Collection(nameof(ExecutionConfigCollection))]
public sealed class IdleHardwareErrorSimulatorTests
{
  [Fact]
  public void HardwareSimulationIsOffByDefault()
  {
    Assert.False(new SettingsExecutionDto().IsHardwareErrorSimulationMode);
    Assert.Equal(
      TypeErroneousMeasurement.None,
      new SettingsExecutionDto().ErroneousMeasurementType);
  }

  [Fact]
  public async Task SettingsRemainIndependentDuringRoundTrip()
  {
    SettingsExecutionDto original = await ExecutionConfig.GetExecitonModel();

    try
    {
      await ExecutionConfig.SetExecutionModel(new SettingsExecutionDto
      {
        IdleModeExecution = true,
        ErroneousMeasurementType = TypeErroneousMeasurement.Rnd,
        IsHardwareErrorSimulationMode = false,
      });

      SettingsExecutionDto measurementOnly = await ExecutionConfig.GetExecitonModel();
      Assert.Equal(TypeErroneousMeasurement.Rnd, measurementOnly.ErroneousMeasurementType);
      Assert.False(measurementOnly.IsHardwareErrorSimulationMode);

      await ExecutionConfig.SetExecutionModel(new SettingsExecutionDto
      {
        IdleModeExecution = true,
        ErroneousMeasurementType = TypeErroneousMeasurement.None,
        IsHardwareErrorSimulationMode = true,
      });

      SettingsExecutionDto hardwareOnly = await ExecutionConfig.GetExecitonModel();
      Assert.Equal(TypeErroneousMeasurement.None, hardwareOnly.ErroneousMeasurementType);
      Assert.True(hardwareOnly.IsHardwareErrorSimulationMode);
    }
    finally
    {
      await ExecutionConfig.SetExecutionModel(original);
    }
  }

  [Fact]
  public async Task HardwareSimulationIsDisabledOutsideIdleMode()
  {
    SettingsExecutionDto original = await ExecutionConfig.GetExecitonModel();

    try
    {
      await ExecutionConfig.SetExecutionModel(new SettingsExecutionDto
      {
        IdleModeExecution = false,
        IsHardwareErrorSimulationMode = true,
      });

      Assert.False(IdleHardwareErrorSimulator.ShouldSimulateHardwareError(CreateDevice(true)));
    }
    finally
    {
      await ExecutionConfig.SetExecutionModel(original);
    }
  }

  [Fact]
  public async Task HardwareSimulationIsAlwaysEnabledForSelectedDeviceInIdleMode()
  {
    SettingsExecutionDto original = await ExecutionConfig.GetExecitonModel();

    try
    {
      await ExecutionConfig.SetExecutionModel(new SettingsExecutionDto
      {
        IdleModeExecution = true,
        IsHardwareErrorSimulationMode = false,
      });

      IDevice device = CreateDevice(true);
      for (int attempt = 0; attempt < 100; attempt++)
      {
        Assert.True(IdleHardwareErrorSimulator.ShouldSimulateHardwareError(device));
      }
    }
    finally
    {
      await ExecutionConfig.SetExecutionModel(original);
    }
  }

  [Fact]
  public async Task HardwareSimulationIsDisabledForDeviceWithoutSetting()
  {
    SettingsExecutionDto original = await ExecutionConfig.GetExecitonModel();

    try
    {
      await ExecutionConfig.SetExecutionModel(new SettingsExecutionDto
      {
        IdleModeExecution = true,
        IsHardwareErrorSimulationMode = true,
      });

      Assert.False(IdleHardwareErrorSimulator.ShouldSimulateHardwareError(CreateDevice(false)));
    }
    finally
    {
      await ExecutionConfig.SetExecutionModel(original);
    }
  }

  [Fact]
  public async Task HardwareSimulationFlagRoundTripsWithoutOverwritingDeviceData()
  {
    await using var connection = new SqliteConnection("Data Source=:memory:");
    await connection.OpenAsync();
    var options = new DbContextOptionsBuilder<AppDbContext>()
      .UseSqlite(connection)
      .Options;

    int deviceId;
    await using (var setupContext = new AppDbContext(options))
    {
      await setupContext.Database.EnsureCreatedAsync();
      var device = new ChassisManagerDto
      {
        Name = "Шасси",
        Description = "Исходное описание",
        Number = 7,
        ConnectionDetails = "192.168.1.7",
        DeviceType = DeviceType.ChassisManager,
        DeviceClass = "Test.Chassis",
      };
      setupContext.ChassisManagers.Add(device);
      await setupContext.SaveChangesAsync();
      deviceId = device.Id;
    }

    DeviceDto detachedDevice;
    await using (var readContext = new AppDbContext(options))
    {
      detachedDevice = await readContext.ChassisManagers
        .AsNoTracking()
        .SingleAsync(device => device.Id == deviceId);
    }

    detachedDevice.IsHardwareFailureSimulationEnabled = true;
    await using (var updateContext = new AppDbContext(options))
    {
      updateContext.Attach((object)detachedDevice);
      updateContext.Entry((object)detachedDevice)
        .Property(nameof(DeviceDto.IsHardwareFailureSimulationEnabled))
        .IsModified = true;
      await updateContext.SaveChangesAsync();
    }

    await using var verifyContext = new AppDbContext(options);
    var savedDevice = await verifyContext.ChassisManagers.SingleAsync(device => device.Id == deviceId);
    Assert.True(savedDevice.IsHardwareFailureSimulationEnabled);
    Assert.Equal("Шасси", savedDevice.Name);
    Assert.Equal("Исходное описание", savedDevice.Description);
    Assert.Equal("192.168.1.7", savedDevice.ConnectionDetails);
  }

  private static IDevice CreateDevice(bool simulationEnabled)
  {
    var device = new Mock<IDevice>();
    device.SetupGet(x => x.IsHardwareFailureSimulationEnabled).Returns(simulationEnabled);
    return device.Object;
  }
}

[CollectionDefinition(nameof(ExecutionConfigCollection), DisableParallelization = true)]
public sealed class ExecutionConfigCollection;
