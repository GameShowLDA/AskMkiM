using System.Windows;
using System.Windows.Input;
using AppConfig;
using AppConfig.DataBase;
using AppConfig.DataBase.Models;
using AppConfig.DataBase.Services;
using Microsoft.EntityFrameworkCore;
using static Utilities.LoggerUtility;

namespace TestWPF
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    public MainWindow()
    {
      Task.Run(async () =>
      {
        try
        {
          await StartConfigAsync();
        }
        catch (InvalidOperationException exception)
        {
          LogError($"Ошибка загрузки темы программы: {exception}");
          return;
        }
        catch (Exception ex)
        {
          LogError($"Ошибка выполнения программы: {ex}");
        }
      }).Wait();

      InitializeComponent();
      DbContextOptionsBuilder<AppDbContext> optionsBuilder = new DbContextOptionsBuilder<AppDbContext>().UseSqlite($"Data Source={FileLocations.ConfigFilePath}");
      using var dbContext = new AppDbContext(optionsBuilder.Options);
      var service = new ChassisManagerRepository(dbContext);
      var data = service.GetAll();
      foreach (var item in data)
      {
        Test.AddSystem(item);
      }

      var Racks = new RackRepository(dbContext).GetAll();
      foreach (var item in Racks)
      {
        Test.AddRack(item);
      }

      // TestDataSeeder.GenerateTestDataAndSaveToDB();
    }

    private async Task StartConfigAsync()
    {
      await SettingsFileReader.ReadAllSettingsAsync();
    }


    private void Button_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      new UI.Controls.Search.SearchWindow().ShowDialog();
    }
  }


  public static class TestDataSeeder
  {
    public static void GenerateTestDataAndSaveToDB()
    {
      DbContextOptionsBuilder<AppDbContext> optionsBuilder = new DbContextOptionsBuilder<AppDbContext>()
          .UseSqlite($"Data Source={FileLocations.ConfigFilePath}");

      using var dbContext = new AppDbContext(optionsBuilder.Options);

      var chassisRepo = new ChassisManagerRepository(dbContext);
      var breakdownRepo = new BreakdownTesterRepository(dbContext);
      var fastMeterRepo = new FastMeterRepository(dbContext);
      var powerSourceRepo = new PowerSourceModuleRepository(dbContext);
      var precisionMeterRepo = new PrecisionMeterRepository(dbContext);
      var relaySwitchRepo = new RelaySwitchModuleRepository(dbContext);
      var switchingDeviceRepo = new SwitchingDeviceRepository(dbContext);

      // Проверяем, есть ли уже данные в БД
      if (chassisRepo.GetAll().Any())
      {
        Console.WriteLine("Тестовые данные уже существуют в базе.");
        return;
      }

      // Создаём 3 менеджера шасси
      var chassisManagers = new List<ChassisManagerEntity>
        {
            new ChassisManagerEntity { Id = 1, Name = "Chassis Alpha", Number = 101, Description = "Основное шасси", ConnectionDetails = "192.168.1.1" },
            new ChassisManagerEntity { Id = 2, Name = "Chassis Beta", Number = 102, Description = "Резервное шасси", ConnectionDetails = "192.168.1.2" },
            new ChassisManagerEntity { Id = 3, Name = "Chassis Gamma", Number = 103, Description = "Шасси для тестирования", ConnectionDetails = "192.168.1.3" }
        };

      foreach (var chassis in chassisManagers)
      {
        chassisRepo.Create(chassis);
      }

      // Генерируем устройства для каждого шасси
      foreach (var chassis in chassisManagers)
      {
        for (int i = 1; i <= 3; i++)
        {
          breakdownRepo.Create(new BreakdownTesterEntity
          {
            Id = chassis.Id * 10 + i,
            Name = $"BreakdownTester {chassis.Number}-{i}",
            NumberChassis = chassis.Number,
            Number = chassis.Id * 10 + i,
            Description = "Пробойная установка",
            ConnectionDetails = $"192.168.2.{i}"
          });

          fastMeterRepo.Create(new FastMeterEntity
          {
            Id = chassis.Id * 20 + i,
            Name = $"FastMeter {chassis.Number}-{i}",
            NumberChassis = chassis.Number,
            Number = chassis.Id * 20 + i,
            Description = "Быстрый измеритель",
            ConnectionDetails = $"192.168.3.{i}"
          });

          powerSourceRepo.Create(new PowerSourceModuleEntity
          {
            Id = chassis.Id * 30 + i,
            Name = $"PowerSource {chassis.Number}-{i}",
            NumberChassis = chassis.Number,
            Number = chassis.Id * 30 + i,
            Description = "Источник питания",
            ConnectionDetails = $"192.168.4.{i}"
          });

          precisionMeterRepo.Create(new PrecisionMeterEntity
          {
            Id = chassis.Id * 40 + i,
            Name = $"PrecisionMeter {chassis.Number}-{i}",
            NumberChassis = chassis.Number,
            Number = chassis.Id * 40 + i,
            Description = "Точный измеритель",
            ConnectionDetails = $"192.168.5.{i}"
          });

          relaySwitchRepo.Create(new RelaySwitchModuleEntity
          {
            Id = chassis.Id * 50 + i,
            Name = $"RelaySwitch {chassis.Number}-{i}",
            NumberChassis = chassis.Number,
            Number = chassis.Id * 50 + i,
            Description = "Релейный коммутатор",
            ConnectionDetails = $"192.168.6.{i}",
            PointCount = 10
          });

          switchingDeviceRepo.Create(new SwitchingDeviceEntity
          {
            Id = chassis.Id * 60 + i,
            Name = $"SwitchingDevice {chassis.Number}-{i}",
            NumberChassis = chassis.Number,
            Number = chassis.Id * 60 + i,
            Description = "Коммутатор устройств",
            ConnectionDetails = $"192.168.7.{i}"
          });
        }
      }

      Console.WriteLine("Тестовые данные успешно добавлены в БД.");
    }
  }


}