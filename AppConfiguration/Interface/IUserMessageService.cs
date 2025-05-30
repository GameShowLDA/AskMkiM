using System.Threading.Tasks;
using Utilities.Models;

namespace AppConfiguration.Interface
{
  /// <summary>
  /// Интерфейс для отображения сообщений пользователю.
  /// </summary>
  public interface IUserMessageService
  {
    /// <summary>
    /// Асинхронное отображение сообщения пользователю.
    /// </summary>
    /// <param name="model">Модель отображаемого сообщения.</param>
    /// <returns>True, если пользователь подтвердил действие.</returns>
    Task ShowMessageAsync(ShowMessageModel model, bool IsBlockStart = false, bool SkipStepModeCheck = false);

    Task AppendEmptyLineAsync(int indentLevel = 0);

    string Header { get; set; }
  }
}