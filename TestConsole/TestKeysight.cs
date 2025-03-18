using System.Net;
using NewCore.Device;

namespace TestConsole
{
  internal class TestKeysight
  {
    /// <summary>
    /// Главный метод для управления COM-портами.
    /// </summary>
    public static async void RunAsync()
    {
      Console.WriteLine("=== Управление Keysight ===");
      IPAddress iPAddress = IPAddress.Parse("192.168.1.16");

      NewCore.Device.KeysightDevice keysight3466 = new NewCore.Device.KeysightDevice(iPAddress);
      if (await keysight3466.ConnectionManager.InitializeAsync())
      {
        Console.WriteLine($"Подключен к: {keysight3466.Name}");

        await Continuity(keysight3466);
        await Capacitance(keysight3466);
        await Resistance(keysight3466);
        await Voltage(keysight3466);

        keysight3466.ConnectionManager.Disconnect();
      }
      else
      {
        Console.WriteLine("Ошибка подключения.");
      }
    }

    static private async Task Continuity(KeysightDevice keysight3466)
    {
      await keysight3466.ContinuityManager.SetContinuityModeAsync();
      var data = await keysight3466.ContinuityManager.CheckContinuityAsync();
      Console.WriteLine($"Результат прозвонки: {data}");
    }

    static private async Task Resistance(KeysightDevice keysight3466)
    {
      await keysight3466.ResistanceManager.SetResistanceModeAsync();
      var data = await keysight3466.ResistanceManager.MeasureResistanceAsync();
      Console.WriteLine($"Результат сопротивления: {data}");
    }

    static private async Task Capacitance(KeysightDevice keysight3466)
    {
      await keysight3466.CapacitanceManager.SetCapacitanceModeAsync();
      var data = await keysight3466.CapacitanceManager.MeasureCapacitanceAsync();
      Console.WriteLine($"Результат ёмкости: {data}");
    }

    static private async Task Voltage(KeysightDevice keysight3466)
    {
      await keysight3466.DcVoltageManager.SetDCVoltageModeAsync();
      var data = await keysight3466.DcVoltageManager.MeasureDCVoltageAsync();
      Console.WriteLine($"Результат напяжения(постоянное): {data}");
    }
  }
}
