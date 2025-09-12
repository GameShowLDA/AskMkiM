using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using ControlCommandAnalyser.Model.Chains;
using Utilities.Interface;
using Utilities.Models;

namespace ControlCommandExecutor
{
  internal static class PointFormater
  {

    internal static string GetFormatDisconnectPoint(List<ChainModel> chainModels)
    {
      var formatPoint = new List<string>();


      for (int itemIndex = 0; itemIndex < chainModels.Count; itemIndex++)
      {
        var item = chainModels[itemIndex];
        var count = item.PointModels.Count;
        var chainStr = string.Empty;
        for (int i = 0; i < count; i++)
        {
          var point = item.PointModels[i].Mnemonic;

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


      return result;
    }

    internal static string GetFormatConnectPoint(ChainModel chainModels)
    {
      var result = string.Empty;
      var count = chainModels.PointModels.Count;

      for (int i = 0; i < count; i++)
      {
        var point = chainModels.PointModels[i].Mnemonic;

        result += $"*{point}*";
        if (i + 1 != count)
        {
          result += $" ** ";
        }
      }

      return result;
    }

    internal static async Task MessageResult(List<ShowMessageModel> showMessageModels, IUserMessageService messageService)
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
