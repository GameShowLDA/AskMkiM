using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.UI.Infrastructure.Localization;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace UI.Controls.Settings
{
  /// <summary>
  /// Логика взаимодействия для SettingsProgrammControl.xaml
  /// </summary>
  public partial class SettingsProgrammControl : UserControl
  {
    public SettingsProgrammControl()
    {
      InitializeComponent();
      Loaded += SettingsProgrammControl_Loaded;
    }

    private void SettingsProgrammControl_Loaded(object sender, RoutedEventArgs e)
    {
      LocalizationService.RefreshCurrentLanguage();
    }

    private void PrintConfig(object sender, MouseButtonEventArgs e)
    {
      try
      {
        var chassisList = new DataBaseConfiguration.Services.Device.ChassisManagerServices().GetAllEntities();
        var result = new List<object>();

        foreach (var chassis in chassisList)
        {
          var chassisDict = new Dictionary<string, object?>
          {
            ["ChassisNumber"] = chassis.Number,
            ["ChassisName"] = chassis.Name,
            ["IP"] = chassis.ConnectionDetails,
            ["Modules"] = new List<object>()
          };

          var modules = (List<object>)chassisDict["Modules"]!;

          var moduleServices = new (string Name, IEnumerable<object> Items)[]
          {
            ("RelaySwitchModules", new DataBaseConfiguration.Services.Device.RelaySwitchModuleServices().GetEntitiesByNumberChassis(chassis.Number)),
            ("SwitchingDevices", new DataBaseConfiguration.Services.Device.SwitchingDeviceServices().GetEntitiesByNumberChassis(chassis.Number)),
            ("FastMeters", new DataBaseConfiguration.Services.Device.FastMeterServices().GetEntitiesByNumberChassis(chassis.Number)),
            ("BreakdownTesters", new DataBaseConfiguration.Services.Device.BreakdownTesterServices().GetEntitiesByNumberChassis(chassis.Number)),
            ("PowerSources", new DataBaseConfiguration.Services.Device.PowerSourceModuleServices().GetEntitiesByNumberChassis(chassis.Number))
          };

          foreach (var (moduleType, items) in moduleServices)
          {
            foreach (var item in items)
            {
              var dict = ExtractDeviceData(item);
              modules.Add(dict);
            }
          }

          result.Add(chassisDict);
        }

        string json = JsonSerializer.Serialize(result, new JsonSerializerOptions
        {
          WriteIndented = true,
          Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

        PrintJson(json);
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка при формировании конфигурации: {ex.Message}", "Ошибка",
          MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    /// <summary>
    /// Извлекает только основные свойства устройства:
    /// Имя, Номер, Тип подключения (IP или COM) и детали подключения.
    /// </summary>
    private static Dictionary<string, object?> ExtractDeviceData(object item)
    {
      var result = new Dictionary<string, object?>();

      if (item is IDevice device)
      {
        result["Имя устрйоства"] = device.Name;
        result["Номер устройства"] = device.Number;
        result["Тип подлючения"] = DetectConnectionType(device.ConnectionDetails);
        result["Адрес подлючения"] = device.ConnectionDetails;
      }
      else
      {
        var props = item.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        string? connection = null;
        string? name = null;
        string? type = null;
        int? number = null;

        foreach (var prop in props)
        {
          string pname = prop.Name.ToLowerInvariant();
          var val = prop.GetValue(item)?.ToString();
          if (pname.Contains("connection"))
            connection = val;
          else if (pname.Contains("name"))
            name = val;
          else if (pname.Contains("type"))
            type = val;
          else if (pname.Contains("number") && int.TryParse(val, out int n))
            number = n;
        }

        result["Имя устрйоства"] = name;
        result["Номер устройства"] = number;
        result["Тип подлючения"] = DetectConnectionType(connection);
        result["Адрес подлючения"] = connection;
      }

      return result;
    }

    /// <summary>
    /// Определяет тип подключения (IP или COM) по содержимому строки.
    /// </summary>
    private static string DetectConnectionType(string? connection)
    {
      if (string.IsNullOrWhiteSpace(connection))
        return "Unknown";

      if (connection.Trim().ToUpper().StartsWith("COM"))
        return "COM";

      if (System.Net.IPAddress.TryParse(connection.Split(':')[0], out _))
        return "IP";

      if (connection.Contains(":") && System.Net.IPAddress.TryParse(connection, out _))
        return "IP";

      return "Unknown";
    }

    /// <summary>
    /// Печатает JSON через стандартное диалоговое окно принтера.
    /// </summary>
    private void PrintJson(string json)
    {
      var paragraph = new Paragraph(new Run(json))
      {
        FontFamily = new System.Windows.Media.FontFamily("Consolas"),
        FontSize = 11,
        Margin = new Thickness(30, 20, 30, 20),
        TextAlignment = TextAlignment.Left
      };

      var document = new FlowDocument(paragraph)
      {
        PagePadding = new Thickness(50),
        FontFamily = new System.Windows.Media.FontFamily("Consolas"),
        FontSize = 11,
        ColumnWidth = double.PositiveInfinity,
        ColumnGap = 0,
        IsColumnWidthFlexible = false,
        TextAlignment = TextAlignment.Left
      };

      PrintDialog printDialog = new PrintDialog();
      if (printDialog.ShowDialog() == true)
      {
        IDocumentPaginatorSource idpSource = document;
        printDialog.PrintDocument(idpSource.DocumentPaginator, "Конфигурация устройств");
      }
    }
  }
}


