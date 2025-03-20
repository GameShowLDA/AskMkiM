using System.Windows;
using System.Windows.Input;
using AppConfig;
using AppConfig.DataBase;
using AppConfig.DataBase.Models;
using AppConfig.DataBase.Repositories;
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
      var data = new ChassisManagerServices().GetAll();
      foreach (var item in data)
      {
        Test.AddSystem((ChassisManagerEntity)item);
      }

      var Racks = new RackServices().GetAll();
      foreach (var item in Racks)
      {
        Test.AddRack((RackEntity)item);
      }
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
}