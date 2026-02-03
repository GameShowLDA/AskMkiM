using Ask.Core.Services.Extensions;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandExecutor.Execution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ask.Engine.ControlCommandExecutor.Executors.Interface
{
  internal class PtCommandExecutor : ICommandExecutor
  {
    public string Mnemonic => EnumExtensions.GetDisplayOrganizationalInfo(OrganizationalComands.PT).DisplayName;

    public async Task ExecuteAsync(CommandExecutionContext context, ProtocolModel protocolModel)
    {
      var command = context.Command as PtCommandModel;
      context.TranslationControl.SetActiveLine(command.FormattedStartLineNumber);

      var messgaeService = context.Console;

      foreach (var key in command.BusPointsDictionary.Keys)
      {
        await messgaeService.ShowMessageAsync(new ShowMessageModel($"Шина {key}"));

        foreach (var item in command.BusPointsDictionary[key])
        {
          await messgaeService.ShowMessageAsync(new ShowMessageModel($"Точка {item}"));
        }
      }

    }
  }
}
