using System.Resources;

namespace TestConsole
{
  internal class ThemeManager
  {
    public static void RunAsync()
    {
      var assembly = typeof(UI.Resources.Theme.Colors).Assembly;

      var darkManager = new ResourceManager("UI.Resources.Theme.Colors", assembly);
      var lightManager = new ResourceManager("UI.Resources.Theme.Colors.Light", assembly);


      string[] keys = { "PrimaryColor", "SecondaryColor", "ForegroundColor", "ActiveColor", "RedColor", "GreenColor", "YellowColor" };

      Console.WriteLine("{0,-25} {1,-15} {2,-15}", "Key", "DarkValue", "LightValue");
      Console.WriteLine(new string('-', 60));

      foreach (var key in keys)
      {
        string darkValue = darkManager.GetString(key) ?? "—";
        string lightValue = lightManager.GetString(key) ?? "—";

        Console.WriteLine("{0,-25} {1,-15} {2,-15}", key, darkValue, lightValue);
      }
    }
  }
}
