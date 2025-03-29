using System.Windows;
using System.Windows.Input;
using AppManager;
using AppManager.DataBase;
using AppManager.DataBase.Models;
using AppManager.DataBase.Services;
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