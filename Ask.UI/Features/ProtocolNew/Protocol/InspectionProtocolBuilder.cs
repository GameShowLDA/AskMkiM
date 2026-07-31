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
    message.AppendLine($"Проверка \"{settings.Name}\" завершена ({settings.Mode}).");
    message.AppendLine($"\tНачало проверки: {DateTime.Now:dd.MM.yyyy} {settings.StartTime:HH:mm:ss}");
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
        foreach (var testResult in deviceResult.Tests)
        {
          if (testResult.Errors.Count > 0)
          {
            WriteErrorMessage(message, i, testResult);
          }
        }
        i++;
      }

    }
    else if(settings.DeviceResults.Count == 1)
    {
      foreach (var deviceResult in settings.DeviceResults)
      {
        int i = 1;
        foreach (var testResult in deviceResult.Tests)
        {
          if (testResult.Errors.Count > 0)
          {
            WriteErrorMessage(message, i, testResult);
          }
          else
          {
            message.AppendLine($"\t\t{i}. {testResult.TestName} [НОРМА]");
            i++;
          }
        }
      }
    }


    if (settings.ExecutionErrors.Count == 0)
    {
      message.AppendLine("\tЗаключение: ошибок не обнаружено");
      return message.ToString();
    }

    message.AppendLine("\nЗаключение:");
    for (var index = 0; index < settings.ExecutionErrors.Count; index++)
    {
      message.AppendLine($"\t{index + 1}. {settings.ExecutionErrors[index]} [БРАК]");
    }

    return message.ToString();
  }

  private static void WriteErrorMessage(StringBuilder message, int i, TestExecutionResult testResult)
  {
    message.AppendLine($"\t\t{i}. {testResult.TestName} {testResult.Errors.Count} ошибок:");
    for (var index = 0; index < testResult.Errors.Count; index++)
    {
      message.AppendLine($"\t\t\t{i}.{index + 1}. {testResult.Errors[index].Message} [БРАК]");
    }
  }
}
