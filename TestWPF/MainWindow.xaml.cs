using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AppConfig;
using AppConfig.DataBase;
using AppConfig.DataBase.Models;
using AppConfig.DataBase.Services;
using Microsoft.EntityFrameworkCore;

namespace TestWPF
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    public MainWindow()
    {
      SettingsFileReader.ReadAllSettingsAsync().ConfigureAwait(true);
      InitializeComponent();

      DbContextOptionsBuilder<AppDbContext> optionsBuilder = new DbContextOptionsBuilder<AppDbContext>().UseSqlite($"Data Source={FileLocations.ConfigFilePath}");
      using var dbContext = new AppDbContext(optionsBuilder.Options);
      var service = new ChassisManagerRepository(dbContext);
      var data = service.GetAll();
      foreach (var item in data)
      {
        Test.AddSystem(item);
      }

      //var data = GenerateTestData();

      //foreach (var item in data)
      //{
      //  service.Create(item);
      //}
    }

    private void Button_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      new UI.Controls.Search.SearchWindow().ShowDialog();
    }

    public static List<ChassisManagerEntity> GenerateTestData()
    {
      return new List<ChassisManagerEntity>
        {
            new ChassisManagerEntity { Id = 1, Name = "Chassis Alpha", Number = 101, Description = "Основное шасси", ConnectionDetails = "192.168.1.1" },
            new ChassisManagerEntity { Id = 2, Name = "Chassis Beta", Number = 102, Description = "Резервное шасси", ConnectionDetails = "192.168.1.2" },
            new ChassisManagerEntity { Id = 3, Name = "Chassis Gamma", Number = 103, Description = "Шасси для тестирования", ConnectionDetails = "192.168.1.3" },
            new ChassisManagerEntity { Id = 4, Name = "Chassis Delta", Number = 104, Description = "Шасси с повышенной отказоустойчивостью", ConnectionDetails = "192.168.1.4" },
            new ChassisManagerEntity { Id = 5, Name = "Chassis Epsilon", Number = 105, Description = "Шасси с поддержкой удаленного управления", ConnectionDetails = "192.168.1.5" },
            new ChassisManagerEntity { Id = 6, Name = "Chassis Zeta", Number = 106, Description = "Шасси для лабораторных исследований", ConnectionDetails = "192.168.1.6" },
            new ChassisManagerEntity { Id = 7, Name = "Chassis Eta", Number = 107, Description = "Шасси с низким энергопотреблением", ConnectionDetails = "192.168.1.7" },
            new ChassisManagerEntity { Id = 8, Name = "Chassis Theta", Number = 108, Description = "Шасси с высокой пропускной способностью", ConnectionDetails = "192.168.1.8" },
            new ChassisManagerEntity { Id = 9, Name = "Chassis Iota", Number = 109, Description = "Шасси с поддержкой резервного копирования", ConnectionDetails = "192.168.1.9" },
            new ChassisManagerEntity { Id = 10, Name = "Chassis Kappa", Number = 110, Description = "Экспериментальное шасси", ConnectionDetails = "192.168.1.10" }
        };
    }
  }
}