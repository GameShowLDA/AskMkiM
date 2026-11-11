using Ask.Core.Shared.DTO.Devices.PowerSupply;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Ask.UI.Features.EquipmentConfiguration.PowerSupply;

/// <summary>
/// Редактирует параметры подключения и рабочие пределы внешнего источника питания.
/// </summary>
public partial class PowerSupplySettingsControl : UserControl
{
  /// <summary>
  /// Создаёт форму параметров внешнего источника питания.
  /// </summary>
  public PowerSupplySettingsControl() => InitializeComponent();

  /// <summary>
  /// Поля, изменение которых снимает подсветку ошибки секции.
  /// </summary>
  public IReadOnlyList<FrameworkElement> ValidationSources =>
  [
    PortTextBox,
    MinVoltageTextBox,
    MaxVoltageTextBox,
    VoltageStepTextBox,
    MinCurrentTextBox,
    MaxCurrentTextBox,
    CurrentStepTextBox,
  ];

  /// <summary>
  /// Секция, подсвечиваемая при ошибке параметров.
  /// </summary>
  public FrameworkElement ValidationSection => RootCard;

  /// <summary>
  /// Поле TCP-порта, на которое переводится фокус при ошибке.
  /// </summary>
  public FrameworkElement PortField => PortTextBox;

  /// <summary>
  /// Первое поле рабочих пределов, на которое переводится фокус при ошибке.
  /// </summary>
  public FrameworkElement OperatingLimitsField => MinVoltageTextBox;

  /// <summary>
  /// TCP-порт из формы или ноль, если значение некорректно.
  /// </summary>
  public int Port
    => int.TryParse(PortTextBox.Text, out int port) && port is >= 1 and <= 65535 ? port : 0;

  /// <summary>
  /// Загружает параметры источника питания в форму.
  /// </summary>
  /// <param name="dto">Конфигурация внешнего источника питания.</param>
  public void LoadFrom(PowerSupplyDto dto)
  {
    ArgumentNullException.ThrowIfNull(dto);
    SetText(PortTextBox, dto.Port);
    SetText(MinVoltageTextBox, dto.MinVoltage);
    SetText(MaxVoltageTextBox, dto.MaxVoltage);
    SetText(VoltageStepTextBox, dto.VoltageStep);
    SetText(MinCurrentTextBox, dto.MinCurrent * 1000m);
    SetText(MaxCurrentTextBox, dto.MaxCurrent * 1000m);
    SetText(CurrentStepTextBox, dto.CurrentStep * 1000m);
  }

  /// <summary>
  /// Записывает параметры формы в конфигурацию источника питания.
  /// </summary>
  /// <param name="dto">Конфигурация внешнего источника питания.</param>
  public void ApplyTo(PowerSupplyDto dto)
  {
    ArgumentNullException.ThrowIfNull(dto);
    if (Port == 0)
      throw new InvalidOperationException("Проверьте TCP-порт источника питания.");
    if (!TryGetOperatingLimits(out OperatingLimits limits))
      throw new InvalidOperationException("Проверьте диапазоны и шаги напряжения и тока.");

    dto.Port = Port;
    dto.MinVoltage = limits.MinVoltage;
    dto.MaxVoltage = limits.MaxVoltage;
    dto.VoltageStep = limits.VoltageStep;
    dto.MinCurrent = limits.MinCurrent;
    dto.MaxCurrent = limits.MaxCurrent;
    dto.CurrentStep = limits.CurrentStep;
  }

  /// <summary>
  /// Проверяет диапазоны и шаги напряжения и тока.
  /// </summary>
  /// <returns><see langword="true"/>, если значения корректны; иначе — <see langword="false"/>.</returns>
  public bool HasValidOperatingLimits() => TryGetOperatingLimits(out _);

  private bool TryGetOperatingLimits(out OperatingLimits limits)
  {
    limits = default;
    if (!TryParseNonNegativeDecimal(MinVoltageTextBox, out decimal minVoltage) ||
        !TryParseNonNegativeDecimal(MaxVoltageTextBox, out decimal maxVoltage) ||
        !TryParseNonNegativeDecimal(VoltageStepTextBox, out decimal voltageStep) ||
        !TryParseNonNegativeDecimal(MinCurrentTextBox, out decimal minCurrentMilliampere) ||
        !TryParseNonNegativeDecimal(MaxCurrentTextBox, out decimal maxCurrentMilliampere) ||
        !TryParseNonNegativeDecimal(CurrentStepTextBox, out decimal currentStepMilliampere))
      return false;

    decimal minCurrent = minCurrentMilliampere / 1000m;
    decimal maxCurrent = maxCurrentMilliampere / 1000m;
    decimal currentStep = currentStepMilliampere / 1000m;
    if (!IsValidRange(minVoltage, maxVoltage, voltageStep) ||
        !IsValidRange(minCurrent, maxCurrent, currentStep))
      return false;

    limits = new OperatingLimits(
      minVoltage, maxVoltage, voltageStep,
      minCurrent, maxCurrent, currentStep);
    return true;
  }

  private static bool TryParseNonNegativeDecimal(TextBox textBox, out decimal value)
    => decimal.TryParse(textBox.Text.Replace(',', '.'), NumberStyles.Number,
      CultureInfo.InvariantCulture, out value) && value >= 0;

  private static bool IsValidRange(decimal min, decimal max, decimal step)
    => min >= 0 && max > min && step > 0 && step <= max - min;

  private static void SetText(TextBox textBox, decimal value)
    => textBox.Text = value.ToString(CultureInfo.InvariantCulture);

  private void Integer_PreviewTextInput(object sender, TextCompositionEventArgs e)
    => e.Handled = !e.Text.All(char.IsDigit);

  private void Decimal_PreviewTextInput(object sender, TextCompositionEventArgs e)
    => e.Handled = !e.Text.All(character => char.IsDigit(character) || character is '.' or ',');

  private readonly record struct OperatingLimits(
    decimal MinVoltage,
    decimal MaxVoltage,
    decimal VoltageStep,
    decimal MinCurrent,
    decimal MaxCurrent,
    decimal CurrentStep);
}
