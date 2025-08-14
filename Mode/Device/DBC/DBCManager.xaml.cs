using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DataBaseConfiguration.Services.Device;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using NewCore.Base.Interface.Main;
using UI.Controls.ProtocolNew;
using Utilities.Interface;
using Utilities.Models;

namespace Mode.Device.DBC
{
  /// <summary>
  /// Логика взаимодействия для DBCManager.xaml
  /// </summary>
  public partial class DBCManager : UserControl
  {
    private const int ButtonSize = 40;
    private const int ButtonMargin = 5;
    public DBCManager()
    {
      InitializeComponent();
      LoadDBC();
      ChassisData.DeviceSelected += OnDBCSelected;
      Loaded += DBCManager_Loaded;
    }

    private void DBCManager_Loaded(object sender, RoutedEventArgs e)
    {
      int totalButtons = 130;

      for (int i = 0; i < totalButtons; i++)
      {
        Button button = new Button
        {
          Content = $"K_{i + 1}",
          Width = ButtonSize,
          Height = ButtonSize,
          Margin = new Thickness(ButtonMargin),
          Background = Brushes.Red
        };
        button.Click += Button_ClickAsync;

        buttonPanel.Children.Add(button);
      }

      buttonPanel.IsEnabled = false;
    }

    private async void Button_ClickAsync(object sender, RoutedEventArgs e)
    {
      if (sender is Button clickedButton)
      {
        if (int.TryParse(clickedButton.Content.ToString().Split('_')[1], out int parseNumber))
        {
          if (clickedButton.Background == Brushes.Red)
          {
            var result = await dbc.RelayManager.ConnectRelay(parseNumber);
            await Protocol.ShowMessageAsync(new ShowMessageModel($"Реле {parseNumber}", message: $"Подключение {(result ? "НОМРА" : "БРАК")}", type: result ? ShowMessageModel.MessageType.Success : ShowMessageModel.MessageType.Error));
            if (result)
            {
              clickedButton.Background = Brushes.Green;
            }
          }
          else
          {
            var result = await dbc.RelayManager.DisconnectRelay(parseNumber);
            await Protocol.ShowMessageAsync(new ShowMessageModel($"Реле {parseNumber}", message: $"Отключение {(result ? "НОМРА" : "БРАК")}", type: result ? ShowMessageModel.MessageType.Success : ShowMessageModel.MessageType.Error));
            if (result)
            {
              clickedButton.Background = Brushes.Red;
            }
          }
        }
      }
    }

    private ISwitchingDevice dbc;

    public IButtonService ButtonService { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public string Header { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    /// <summary>
    /// Обрабатывает выбор управляющего шасси. После выбора делает видимыми блоки выбора устройства и измерителя.
    /// </summary>
    /// <param name="obj">Выбранное устройство (ожидается IChassisManager).</param>
    private void OnDBCSelected(object obj)
    {
      if (obj is ISwitchingDevice)
      {
        dbc = obj as ISwitchingDevice;
        Connect.Visibility = Visibility.Visible;
      }
      else
      {
        Connect.Visibility = Visibility.Collapsed;
        Disconnect.Visibility = Visibility.Collapsed;
        Reset.Visibility = Visibility.Collapsed;
      }
    }

    /// <summary>
    /// Загружает список всех доступных управляющих шасси и отображает их в поле выбора.
    /// </summary>
    private void LoadDBC()
    {
      var devices = new SwitchingDeviceServices().GetAll();
      var names = new List<string>();

      foreach (var chassisManager in devices)
      {
        names.Add(chassisManager.Name + " " + chassisManager.Number);
      }

      ChassisData.ItemsSource = devices;
      ChassisData.DisplayFields = names;
    }

    private async void Connect_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      var connect = await dbc.ConnectableManager.InitializeAsync();
      if (connect.Connect)
      {
        Connect.Visibility = Visibility.Collapsed;
        ManagerChassisSelectionControl.Visibility = Visibility.Collapsed;
        Disconnect.Visibility = Visibility.Visible;
        Reset.Visibility = Visibility.Visible;
        buttonPanel.IsEnabled = true;
      }
      else
      {
        await Protocol.ShowMessageAsync(new ShowMessageModel(connect.Answer));
      }
    }

    private async void Disconnect_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      await dbc.ConnectableManager.DisconnectAsync();
      Connect.Visibility = Visibility.Visible;
      ManagerChassisSelectionControl.Visibility = Visibility.Visible;
      Disconnect.Visibility = Visibility.Collapsed;
      Reset.Visibility = Visibility.Collapsed;
      buttonPanel.IsEnabled = false;
    }

    private async void Reset_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      await dbc.ConnectableManager.ResetAsync();
    }
  }
}
