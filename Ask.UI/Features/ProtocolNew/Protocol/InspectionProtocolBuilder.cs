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
    message.AppendLine($"Проверка \"{settings.Name}\" от {DateTime.Now:dd.MM.yyyy} завершена ({settings.Mode}).");
    message.AppendLine($"\tНачало проверки: {settings.StartTime:HH:mm:ss}");
    message.AppendLine($"\tВремя выполнения: {settings.ExecutionDuration:hh\\:mm\\:ss\\:fff}");
    message.AppendLine();

    if (settings.InputParameters.Count > 0)
    {
      message.AppendLine("Введённые данные:");
      foreach (var parameter in settings.InputParameters)
        message.AppendLine($"\t{parameter}");

      message.AppendLine();
    }

    if (settings.DeviceResults.Count > 1)
    {

      int i = 1;
      foreach (var deviceResult in settings.DeviceResults)
      {
        message.AppendLine($"\t{i}. {deviceResult.DeviceName}");
        int j = 1;
        foreach (var testResult in deviceResult.Tests)
        {
          if (testResult.Errors.Count > 0)
          {
            message.AppendLine($"\t\t{testResult.TestName} {testResult.Errors.Count} ошибок:");
            for (var index = 0; index < testResult.Errors.Count; index++)
            {
              message.AppendLine($"\t\t\t{index + 1}. {testResult.Errors[index].Message} [БРАК]");
            }
          }
          else
          {
            message.AppendLine($"\t\t{i}.{j}. {testResult.TestName} [НОРМА]");
            j++;
          }
        }
        i++;
      }
    }


    if (settings.ExecutionErrors.Count == 0)
    {
      message.AppendLine("\tЗаключение: ошибок не обнаружено");
      return message.ToString();
    }

    message.AppendLine("Заключение:");
    for (var index = 0; index < settings.ExecutionErrors.Count; index++)
    {
      message.AppendLine($"\t{index + 1}. {settings.ExecutionErrors[index]} [БРАК]");
    }

    return message.ToString();
  }
}
