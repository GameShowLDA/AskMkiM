using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;

namespace Ask.Core.Shared.ParserContext
{
  /// <summary>
  /// Контекст выполнения команды парсера, содержащий информацию
  /// о текущей строке программы и связанных устройствах.
  /// </summary>
  /// <param name="CommandNumber">Номер команды.</param>
  /// <param name="Mnemonic">Мнемоника команды.</param>
  /// <param name="LineNumber">Номер строки программы.</param>
  /// <param name="Breakdown">Пробойная установка, используемая командой.</param>
  /// <param name="Fastmeter">Мультиметр, используемый командой.</param>
  public record ParameterContext(
    string CommandNumber,
    string Mnemonic,
    int LineNumber,
    IBreakdownTester? Breakdown = null,
    IMultimeter? Fastmeter = null)
  {

    /// <summary>
    /// Создаёт новый контекст выполнения команды.
    /// </summary>
    /// <param name="number">Номер команды.</param>
    /// <param name="mnemonic">Мнемоника команды.</param>
    /// <param name="line">Номер строки программы.</param>
    /// <returns>Новый экземпляр <see cref="ParameterContext"/>.</returns>
    public static ParameterContext Create(
        string number,
        string mnemonic,
        int line)
        => new(number, mnemonic, line);
  }
}
