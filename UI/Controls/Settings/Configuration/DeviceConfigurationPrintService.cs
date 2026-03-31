using Ask.Core.Shared.DTO.Devices.Base;
using Ask.Core.Shared.DTO.Devices.ChassisManager;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Device.Communication.Com.Configuration;
using System.Globalization;
using System.IO.Ports;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace UI.Controls.Settings.Configuration;

/// <summary>
/// Сервис формирования и печати конфигурации устройств.
/// </summary>
public static class DeviceConfigurationPrintService
{
  /// <summary>
  /// Формирует печатное представление текущей конфигурации устройств.
  /// </summary>
  public static async Task<string> BuildPrintableConfigurationAsync(CancellationToken cancellationToken = default)
  {
    var configuration = await DeviceConfigurationService.BuildConfigurationFileAsync(cancellationToken);
    return BuildPrintableConfiguration(configuration);
  }

  /// <summary>
  /// Печатает текст через стандартный диалог принтера.
  /// </summary>
  public static void Print(string text)
  {
    var paragraph = new Paragraph(new Run(text))
    {
      FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
      FontSize = 12,
      Margin = new Thickness(30, 20, 30, 20),
      TextAlignment = TextAlignment.Left
    };

    var document = new FlowDocument(paragraph)
    {
      PagePadding = new Thickness(50),
      FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
      FontSize = 12,
      ColumnWidth = double.PositiveInfinity,
      ColumnGap = 0,
      IsColumnWidthFlexible = false,
      TextAlignment = TextAlignment.Left
    };

    var printDialog = new PrintDialog();
    if (printDialog.ShowDialog() == true)
    {
      IDocumentPaginatorSource idpSource = document;
      printDialog.PrintDocument(idpSource.DocumentPaginator, "Конфигурация устройств");
    }
  }

  private static string BuildPrintableConfiguration(DeviceConfigurationFileModel configuration)
  {
    var chassisList = configuration.Chassis
      .OrderBy(chassis => chassis.Number)
      .ToList();

    if (chassisList.Count == 0)
    {
      return "Конфигурация устройств не заполнена.";
    }

    var sb = new StringBuilder();
    int chassisIndex = 1;

    foreach (var chassis in chassisList)
    {
      if (sb.Length > 0)
      {
        sb.AppendLine(new string('=', 70));
      }

      sb.AppendLine($"Шасси #{chassisIndex}");
      AppendField(sb, "Модель устройства", chassis.Name, 2);
      AppendField(sb, "Номер шасси", chassis.Number, 2);
      AppendConnectionDetails(sb, chassis.ConnectionDetails, 2);

      sb.AppendLine();
      sb.AppendLine("  Устройства:");

      int devicesPrinted = 0;

      devicesPrinted += AppendDeviceSection(
        sb,
        "Модуль коммутации релейный",
        configuration.RelaySwitchModules.Where(x => x.NumberChassis == chassis.Number),
        insertSectionSeparator: devicesPrinted > 0,
        (builder, device) =>
        {
          AppendField(builder, "Тип структурной шины", device.BusType.ToString(), 4);
          AppendField(builder, "Сопротивление коммутатора, Ом", device.SwitchResistance, 4);
          AppendField(builder, "Ёмкость коммутатора, нФ", device.SwitchCapacitance, 4);
        });

      devicesPrinted += AppendDeviceSection(
        sb,
        "Устройство коммутации шин",
        configuration.SwitchingDevices.Where(x => x.NumberChassis == chassis.Number),
        insertSectionSeparator: devicesPrinted > 0);

      devicesPrinted += AppendDeviceSection(
        sb,
        "Модуль ист. напряжения и тока",
        configuration.PowerSourceModules.Where(x => x.NumberChassis == chassis.Number),
        insertSectionSeparator: devicesPrinted > 0);

      devicesPrinted += AppendDeviceSection(
        sb,
        "Измеритель (быстрый)",
        configuration.FastMeters.Where(x => x.NumberChassis == chassis.Number),
        insertSectionSeparator: devicesPrinted > 0);

      devicesPrinted += AppendDeviceSection(
        sb,
        "Пробойная установка",
        configuration.BreakdownTesters.Where(x => x.NumberChassis == chassis.Number),
        insertSectionSeparator: devicesPrinted > 0);

      if (devicesPrinted == 0)
      {
        sb.AppendLine("    Не добавлено ни одного устройства.");
      }

      chassisIndex++;
    }

    return sb.ToString().TrimEnd();
  }

  private static int AppendDeviceSection<TDevice>(
    StringBuilder sb,
    string title,
    IEnumerable<TDevice> devices,
    bool insertSectionSeparator = false,
    Action<StringBuilder, TDevice>? appendAdditional = null)
    where TDevice : DeviceDto
  {
    var deviceList = devices
      .OrderBy(device => device.Number)
      .ToList();

    if (deviceList.Count == 0)
    {
      return 0;
    }

    if (insertSectionSeparator)
    {
      sb.AppendLine();
    }

    int currentIndex = 1;
    foreach (var device in deviceList)
    {
      string suffix = deviceList.Count > 1 ? $" #{currentIndex}" : string.Empty;
      sb.AppendLine($"    {title}{suffix}");

      AppendField(sb, "Модель устройства", device.Name, 4);
      AppendField(sb, "Номер устройства", device.Number, 4);
      AppendConnectionDetails(sb, device.ConnectionDetails, 4);

      appendAdditional?.Invoke(sb, device);

      currentIndex++;
      if (currentIndex <= deviceList.Count)
      {
        sb.AppendLine();
      }
    }

    return deviceList.Count;
  }

  private static void AppendConnectionDetails(StringBuilder sb, string? connectionDetails, int indent)
  {
    if (string.IsNullOrWhiteSpace(connectionDetails))
    {
      return;
    }

    if (TryGetIp(connectionDetails, out var ipAddress))
    {
      AppendField(sb, "Тип подключения устройства", "IP", indent);
      AppendField(sb, "IP Address", ipAddress, indent);
      return;
    }

    if (TryGetCom(connectionDetails, out var comSettings) && comSettings != null)
    {
      AppendField(sb, "Тип подключения устройства", "COM", indent);
      AppendField(sb, "COM-порт", comSettings.PortName, indent);
      AppendField(sb, "Бит в секунду", comSettings.BaudRate, indent);
      AppendField(sb, "Стоповые биты", GetStopBitsText(comSettings.StopBits), indent);
      AppendField(sb, "Биты данных", comSettings.DataBits, indent);
      AppendField(sb, "Чётность", GetParityText(comSettings.Parity), indent);
      return;
    }

    AppendField(sb, "Адрес подключения", connectionDetails, indent);
  }

  private static bool TryGetIp(string connectionDetails, out string ipAddress)
  {
    ipAddress = string.Empty;

    if (string.IsNullOrWhiteSpace(connectionDetails))
    {
      return false;
    }

    if (connectionDetails.Contains("{"))
    {
      return false;
    }

    string token = connectionDetails.Trim().Split(':')[0];
    if (!IPAddress.TryParse(token, out var parsed))
    {
      return false;
    }

    ipAddress = parsed.ToString();
    return true;
  }

  private static bool TryGetCom(string connectionDetails, out SerialPortCustom? comSettings)
  {
    comSettings = null;

    if (string.IsNullOrWhiteSpace(connectionDetails))
    {
      return false;
    }

    try
    {
      comSettings = SerialPortCustom.ToObject(connectionDetails);
      return comSettings != null;
    }
    catch
    {
      return false;
    }
  }

  private static string GetStopBitsText(StopBits stopBits)
  {
    return stopBits switch
    {
      StopBits.One => "1",
      StopBits.OnePointFive => "1.5",
      StopBits.Two => "2",
      _ => stopBits.ToString()
    };
  }

  private static string GetParityText(Parity parity)
  {
    return parity switch
    {
      Parity.Even => "Чет",
      Parity.Odd => "Нечет",
      Parity.Mark => "Маркер",
      Parity.Space => "Пробел",
      _ => "Нет"
    };
  }

  private static void AppendField(StringBuilder sb, string label, string? value, int indent)
  {
    if (string.IsNullOrWhiteSpace(value))
    {
      return;
    }

    sb.Append(' ', indent);
    sb.Append(label);
    sb.Append(": ");
    sb.AppendLine(value);
  }

  private static void AppendField(StringBuilder sb, string label, int value, int indent)
  {
    if (value <= 0)
    {
      return;
    }

    AppendField(sb, label, value.ToString(CultureInfo.InvariantCulture), indent);
  }

  private static void AppendField(StringBuilder sb, string label, double value, int indent)
  {
    if (value <= 0)
    {
      return;
    }

    AppendField(sb, label, value.ToString("0.###", CultureInfo.InvariantCulture), indent);
  }
}
