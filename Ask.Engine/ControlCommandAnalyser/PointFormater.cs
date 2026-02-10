using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Engine.ControlCommandAnalyser.Model.Chains;

namespace Ask.Engine.ControlCommandAnalyser
{
  public static class PointFormater
  {
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
