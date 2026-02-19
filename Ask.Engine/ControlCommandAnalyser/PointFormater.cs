using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Engine.ControlCommandAnalyser.Model.Chains;

namespace Ask.Engine.ControlCommandAnalyser
{
  /// <summary>
  /// Утилитарный класс для форматирования строк представления точек цепей
  /// и вывода сообщений результатов проверки.
  /// </summary>
  /// <remarks>
  /// Используется для формирования текстовых представлений цепей в протоколах
  /// и интерфейсе пользователя.
  /// </remarks>
  public static class PointFormater
  {
    /// <summary>
    /// Формирует строковое представление точек для режима «разомкнуто».
    /// </summary>
    /// <param name="chainModels">Список цепей с точками.</param>
    /// <returns>
    /// Асинхронно возвращает строку с форматированным списком точек,
    /// разделённых специальными маркерами:
    /// <list type="bullet">
    /// <item><description><c>*</c> — границы цепи</description></item>
    /// <item><description><c>#</c> — разделитель точек внутри цепи</description></item>
    /// <item><description><c>##</c> или <c>,,</c> — разделитель цепей</description></item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// При включённой настройке отображения машинного адреса
    /// добавляет адрес точки в квадратных скобках.
    /// </remarks>
    public static async Task<string> GetFormatDisconnectPoint(List<ChainModel> chainModels)
    {
      var formatPoint = new List<string>();

      for (int itemIndex = 0; itemIndex < chainModels.Count; itemIndex++)
      {
        var item = chainModels[itemIndex];
        var count = item.PointModels.Count;
        var chainStr = string.Empty;
        for (int i = 0; i < count; i++)
        {
          string point = item.PointModels[i].Mnemonic;
          point += DeviceDisplayConfig.GetMachineAddressVisibility() ? $" [{item.PointModels[i].ToString()}]" : string.Empty;

          if (i == 0 && i + 1 == count)
          {
            chainStr += $"*{point}*";
          }
          else if (i == 0)
          {
            chainStr += $"*{point}";
          }
          else if (i + 1 == count)
          {
            chainStr += $"#{point}*";
          }
          else
          {
            chainStr += $"#{point}";
          }

        }
        formatPoint.Add(chainStr);
      }

      var result = string.Empty;
      for (int i = 0; i < formatPoint.Count; i++)
      {
        if (i + 1 == formatPoint.Count)
        {
          result += formatPoint[i];
          continue;
        }

        var firstStr = formatPoint[i];
        var secondStr = formatPoint[i + 1];

        var firstCountHash = firstStr.IndexOf("#");
        var secondCountHash = secondStr.IndexOf("#");

        if (firstCountHash == -1 && secondCountHash == -1)
        {
          result += firstStr + " ,, ";
        }
        else
        {
          result += firstStr + " ## ";
        }
      }

      result = result.Replace("* ## *", "##");
      result = result.Replace("* ,, *", ",,");
      return result;
    }

    /// <summary>
    /// Формирует строковое представление точек для режима «замкнуто».
    /// </summary>
    /// <param name="chainModels">Модель цепи с набором точек.</param>
    /// <returns>
    /// Строку, где каждая точка обрамлена символами <c>*</c>.
    /// </returns>
    /// <remarks>
    /// Если включено отображение машинного адреса,
    /// к мнемонике точки добавляется её адрес в квадратных скобках.
    /// </remarks>
    public static string GetFormatConnectPoint(ChainModel chainModels)
    {
      var result = string.Empty;
      var count = chainModels.PointModels.Count;


      for (int i = 0; i < count; i++)
      {
        var point = chainModels.PointModels[i];

        var machineAddress = DeviceDisplayConfig.GetMachineAddressVisibility() ? $" [{point.ToString()}]" : string.Empty;
        result += $"*{point.Mnemonic}{machineAddress}*";
      }

      return result;
    }

    /// <summary>
    /// Выводит список сообщений результатов проверки через сервис сообщений.
    /// </summary>
    /// <param name="showMessageModels">Список сообщений для отображения.</param>
    /// <param name="messageService">Сервис вывода сообщений в интерфейс.</param>
    /// <returns>Асинхронная задача завершения операции.</returns>
    /// <remarks>
    /// Если список сообщений не пустой, сначала выводится заголовок
    /// «Результаты проверки», затем все сообщения по порядку.
    /// </remarks>
    public static async Task MessageResult(List<ShowMessageModel> showMessageModels, IMessageOutputService messageService)
    {
      if (showMessageModels.Count > 0)
      {

        await messageService.ShowMessageAsync(new ShowMessageModel($"Результаты проверки") { IndentLevel = 1 });
        foreach (var item in showMessageModels)
        {
          await messageService.ShowMessageAsync(item);
        }
      }
    }
  }
}
