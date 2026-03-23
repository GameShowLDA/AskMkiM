using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Runtime.Device;
using Ask.Device.Runtime.Function.GPT.Command;
using static Ask.LogLib.LoggerUtility;
using static Ask.Device.Runtime.Function.GPT.Command.ManualCommandManager;


namespace Ask.Device.Runtime.Function.GPT.Managment
{
  /// <summary>
  /// Универсальный класс управления частотой испытаний
  /// для различных режимов GPT-79904 (например, ACW).
  /// </summary>
  public class FrequencyManagment : IFrequencyConfigurable
  {
    private readonly GPT79904 _gptModel;
    private readonly BreakdownTypeMode _mode;
    private readonly int _delay;
    private readonly Func<int> _getFrequency;
    private readonly Action<int> _setFrequency;
    private int _frequency;

    /// <summary>
    /// Создаёт новый экземпляр класса <see cref="FrequencyManagment"/>.
    /// </summary>
    /// <param name="gptModel">Модель устройства GPT-79904.</param>
    /// <param name="mode">Режим работы (ACW, DCW и т.д.).</param>
    /// <param name="delay">Задержка между командами (мс).</param>
    /// <param name="getFrequency">Функция получения текущего значения частоты из конфигурации.</param>
    /// <param name="setFrequency">Действие для обновления частоты в конфигурации.</param>
    public FrequencyManagment(
      GPT79904 gptModel,
      BreakdownTypeMode mode,
      int delay,
      Func<int> getFrequency,
      Action<int> setFrequency)
    {
      _gptModel = gptModel;
      _mode = mode;
      _delay = delay;
      _getFrequency = getFrequency;
      _setFrequency = setFrequency;
    }

    /// <inheritdoc />
    /// <summary>
    /// Устанавливает частоту испытаний (50 или 60 Гц).
    /// Выполняет повторную попытку при необходимости.
    /// </summary>
    public async Task<(bool Success, string Message)> SetFrequencyAsync(int frequency, IUserInteractionService? userMessageService = null)
    {
      if (frequency != 50 && frequency != 60)
        return (false, "Частота должна быть 50 или 60 Гц.");

      if (_getFrequency() == frequency)
      {
        _frequency = frequency;
        return (true, string.Empty);
      }

      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        _setFrequency(frequency);
        LogInformation($"{nameof(SetFrequencyAsync)}: Устройство в Idle Mode. Пропускаем установку.", isDeviceLog: true);
        _frequency = frequency;
        return (true, string.Empty);
      }


      try
      {
        await Task.Delay(_delay);
        string command = $"{ManualCommandManager.GetCommandSyntax(ManualCommand.MANU_ACW_FREQUENCY)} {frequency}";
        await _gptModel.DeviceProtocol.QueryAsync(command);
        await Task.Delay(_delay);

        var actual = await GetFrequencyAsync();
        if (actual == frequency)
        {
          _setFrequency(frequency);
          _frequency = frequency;
          return (true, string.Empty);
        }

        // повторная попытка
        await _gptModel.DeviceProtocol.QueryAsync(command);
        actual = await GetFrequencyAsync();
        if (actual == frequency)
        {
          _setFrequency(frequency);
          _frequency = frequency;
          return (true, string.Empty);
        }

        string error = $"Не удалось установить частоту {frequency} Гц. Устройство сообщает: {actual} Гц.";
        return (false, error);
      }
      catch (Exception ex)
      {
        return (false, $"Ошибка при установке частоты: {ex.Message}");
      }
      finally
      {
        await Task.Delay(_delay);
      }
    }

    /// <inheritdoc />
    /// <summary>
    /// Считывает установленную частоту испытаний.
    /// </summary>
    public async Task<int> GetFrequencyAsync()
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return _frequency;
      }

      try
      {
        var query = $"{ManualCommandManager.GetCommandSyntax(ManualCommand.MANU_ACW_FREQUENCY)} ?";
        var response = await _gptModel.DeviceProtocol.QueryAsync(query, timeout: 1000);

        if (int.TryParse(response.Replace("Hz", "").Trim(), out var freq))
          return freq;

        return 0;
      }
      catch
      {
        return 0;
      }
    }
  }
}
