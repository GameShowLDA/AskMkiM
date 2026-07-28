using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;

namespace Ask.UI.Features.ServiceTools.Gpt
{
  /// <summary>
  /// Хранит пробойную установку, выбранную для конкретной вкладки управления GPT.
  /// </summary>
  internal sealed class GptDeviceContext
  {
    /// <summary>
    /// Пробойная установка текущей вкладки.
    /// </summary>
    internal IBreakdownTester? Device { get; set; }
  }
}
