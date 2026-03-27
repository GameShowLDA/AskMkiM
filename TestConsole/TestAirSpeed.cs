using Ask.DataBase.Engine.Static.Devices;
using Ask.Device.Communication.Ethernet.Udp.Protocols;

namespace TestConsole
{
  internal class TestAirSpeed
  {
    public static async Task RunAsync()
    {
      var manager = ChassisManagers.GetByNumberAsync(1).GetAwaiter().GetResult();
      await manager.PowerManager.StartPowerAsync();

      await Task.Delay(5000);
      var udp = new UdpProtocol(manager);

      for (int speed = 0; speed <= 255; speed += 50)
      {
        Console.WriteLine($"Скорость: {speed}");
        await udp.QueryAsync($"3.1.3.{speed}.");
        await udp.QueryAsync($"3.2.3.{speed}.");
        await udp.QueryAsync($"3.3.3.{speed}.");
        await Task.Delay(5000);
      }
    }
  }
}
