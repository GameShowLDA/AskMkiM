using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Interfaces.ProtocolInterfaces;
using System.Text;

namespace Ask.UI.Features.ProtocolNew.Protocol;

/// <summary>
/// Создаёт итоговый текст проверки, сохраняя действующий формат заголовка, времени и заключения.
/// </summary>
internal sealed class InspectionProtocolBuilder : IInspectionProtocolBuilder
{
  /// <inheritdoc />
  public string Build(ActionSettings settings)
  {
    ArgumentNullException.ThrowIfNull(settings);

    var message = new StringBuilder();
    message.AppendLine($"Проверка \"{settings.Name}\" от {DateTime.Now:dd.MM.yyyy} завершена.");
    message.AppendLine($"\tНачало проверки: {settings.StartTime:HH:mm:ss}");
    message.AppendLine($"\tВремя выполнения: {settings.ExecutionDuration:hh\\:mm\\:ss\\:fff}");
    message.AppendLine();

    if (settings.ExecutionErrors.Count == 0)
    {
      message.AppendLine("\tЗаключение: ошибок не обнаружено");
      return message.ToString();
    }

    message.AppendLine("Заключение:");
    for (var index = 0; index < settings.ExecutionErrors.Count; index++)
    {
      message.AppendLine($"\t{index + 1}. {settings.ExecutionErrors[index]}[БРАК]");
    }

    return message.ToString();
  }
}
