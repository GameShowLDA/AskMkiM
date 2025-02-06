using System.Globalization;
using NewCore.Communication;
using static Utilities.LoggerUtility;

namespace NewCore.Function.DeviceBusCommutation
{
  public class RelayManager
  {
    /// <summary>
    /// Устройство коммутации шин.
    /// </summary>
    private readonly Device.DeviceBusCommutation _deviceBusCommutation;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="RelayManager"/>.
    /// </summary>
    /// <param name="deviceBusCommutation">Экземпляр устройства коммутации шин.</param>
    public RelayManager(Device.DeviceBusCommutation deviceBusCommutation) => _deviceBusCommutation = deviceBusCommutation;

    /// <summary>
    /// Initializes a new instance of the <see cref="UKSH"/> class.
    /// </summary>
    private readonly ConstructUKSH ConstructUKSH = new ConstructUKSH();

    /// <summary>
    /// Запись подключения реле в программе.
    /// </summary>
    /// <param name="numberRelay">Номер реле, которое необходимо замкнуть.</param>
    /// <returns>Результат проверки и выполнения команды.</returns>
    public bool ConnectRelayIdleMode(int numberRelay)
    {
      if (numberRelay < 0)
      {
        return false;
      }

      CreateCommandUKSH(numberRelay.ToString(CultureInfo.InvariantCulture), true);
      return true;
    }

    /// <summary>
    /// Подключения реле.
    /// </summary>
    /// <param name="_deviceBusCommutation.IPAddress">IP адрес УКШ.</param>
    /// <param name="numberRelay">Номер реле, которое необходимо замкнуть.</param>
    /// <returns>Результат проверки и выполнения команды.</returns>
    public async Task<bool> ConnectRelay(int numberRelay)
    {
      if (_deviceBusCommutation.IPAddress == null)
      {
        return false;
      }

      if (numberRelay < 0)
      {
        return false;
      }

      string cmd = CreateCommandUKSH(numberRelay.ToString(CultureInfo.InvariantCulture), true);
      if (!string.IsNullOrEmpty(cmd))
      {
        DeviceCommand command = new DeviceCommand(cmd);
        await DeviceCommandSender.SendCommandAsync(_deviceBusCommutation.IPAddress, command).ConfigureAwait(true);
        await Task.Delay(10).ConfigureAwait(true);
        return true;
      }

      return false;
    }

    /// <summary>
    /// Запись отключения реле в программе.
    /// </summary>
    /// <param name="numberRelay">Номер реле, которое необходимо замкнуть.</param>
    /// <returns>Результат проверки и выполнения команды.</returns>
    public bool DisconnectRelayIdleMode(int numberRelay)
    {
      if (numberRelay < 0)
      {
        return false;
      }

      CreateCommandUKSH(numberRelay.ToString(CultureInfo.InvariantCulture), false);
      return true;
    }

    /// <summary>
    /// Подключение реле.
    /// </summary>
    /// <param name="_deviceBusCommutation.IPAddress">IP адрес УКШ.</param>
    /// <param name="numberRelay">Номер реле, которое необходимо замкнуть.</param>
    /// <returns>Результат проверки и выполнения команды.</returns>
    public async Task<bool> DisconnectRelay(int numberRelay)
    {
      if (_deviceBusCommutation.IPAddress == null)
      {
        return false;
      }

      if (numberRelay < 0)
      {
        return false;
      }

      string cmd = CreateCommandUKSH(numberRelay.ToString(CultureInfo.InvariantCulture), false);
      if (!string.IsNullOrEmpty(cmd))
      {
        DeviceCommand command = new DeviceCommand(cmd);
        await DeviceCommandSender.SendCommandAsync(_deviceBusCommutation.IPAddress, command).ConfigureAwait(true);
        await Task.Delay(10).ConfigureAwait(true);
        return true;
      }

      return false;
    }

    /// <summary>
    /// Создает команду для управления реле системы UKSH.
    /// </summary>
    /// <param name="numberRelay">Номер реле, которое необходимо замкнуть или разомкнуть.</param>
    /// <param name="operation">Логическое значение: true для замыкания реле, false для размыкания.</param>
    /// <returns>
    /// Возвращает команду в формате строки для управления реле.
    /// Если реле уже находится в нужном состоянии или произошла ошибка, возвращается null.
    /// Формат команды: "17.[значение_LE].[значение_StatePort].[номер реле].[операция]".
    /// Операция: 0 - замкнуть, 1 - разомкнуть.
    /// </returns>
    public string CreateCommandUKSH(string numberRelay, bool operation)
    {
      int tmp_tmpM74HCT573;
      string command;
      try
      {
        bool flag_DoingCommand = false;
        if (!ConstructUKSH.ValueLE.TryGetValue(numberRelay, out string tmpValueLE))
        {
          return null;
        }

        if (!ConstructUKSH.ValueM74HCT573.TryGetValue(numberRelay, out int tmpM74HCT573))
        {
          return null;
        }

        if (!ConstructUKSH.ValueStatePort.TryGetValue(tmpValueLE, out int tmpStatePort))
        {
          return null;
        }

        if (!ConstructUKSH.ValuePointState.TryGetValue(numberRelay, out bool tmpPointState))
        {
          return null;
        }

        try
        {
          tmp_tmpM74HCT573 = Convert.ToInt32(Math.Pow(2, Convert.ToDouble(tmpM74HCT573)));
        }
        catch (OverflowException ex)
        {
          LogError($"Произошло переполнение при вычислении: {ex.Message}");
          return null;
        }
        catch (Exception ex)
        {
          LogError($"Произошла неожиданная ошибка: {ex.Message}");
          throw;
        }

        string operations = string.Empty;

        switch (operation)
        {
          case true:
            {
              try
              {
                tmpStatePort += tmp_tmpM74HCT573;
              }
              catch (OverflowException ex)
              {
                Console.WriteLine($"Произошло переполнение при сложении: {ex.Message}");
                return null;
              }
              catch (Exception ex)
              {
                Console.WriteLine($"Произошла неожиданная ошибка: {ex.Message}");
                throw;
              }

              ConstructUKSH.ValuePointState[numberRelay] = true;
              flag_DoingCommand = true;
              if (int.TryParse(numberRelay, out int result))
              {
                if ((result >= 105) && (result <= 118))
                {
                  operations = "0";
                }
                else
                {
                  operations = "1";
                }
              }
              else
              {
                operations = "0";
              }

              break;
            }

          case false: // разомкнуть точку
            {
              try
              {
                tmpStatePort -= tmp_tmpM74HCT573;
              }
              catch (OverflowException ex)
              {
                LogError($"Произошло переполнение при вычитании: {ex.Message}");
                return null;
              }
              catch (Exception ex)
              {
                LogError($"Произошла неожиданная ошибка: {ex.Message}");
                throw;
              }

              ConstructUKSH.ValuePointState[numberRelay] = false;
              flag_DoingCommand = true;
              if (int.TryParse(numberRelay, out int result))
              {
                if ((result >= 105) && (result <= 118))
                {
                  operations = "0";
                }
                else
                {
                  operations = "1";
                }
              }

              break;
            }
        }

        if (flag_DoingCommand)
        {
          ConstructUKSH.ValueStatePort[tmpValueLE] = tmpStatePort;
          command = "8." + tmpValueLE + "." + tmpStatePort + "." + numberRelay + ".";
        }
        else
        {
          command = null;
        }
      }
      catch (KeyNotFoundException ex)
      {
        LogError($"Ошибка: Ключ не найден. {ex.Message}");
        return null;
      }
      catch (Exception ex)
      {
        // Логирование исключения
        LogError($"Произошла непредвиденная ошибка: {ex.Message}");
        throw;
      }

      return command;
    }
  }
}
