using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ControlCommandAnalyser.Model;
using ControlCommandAnalyser.Model.Ok;
using ControlCommandExecutor.Execution;
using Utilities.Models;

namespace ControlCommandExecutor.Executors
{
  internal class RmCommandExecutor : ICommandExecutor
  {
    public string Mnemonic => "РМ";

    public async Task ExecuteAsync(CommandExecutionContext context)
    {

      var command = context.Command as RmCommandModel;
      context.TranslationControl.SetActiveLine(command.FormattedStartLineNumber);

      var points = command.GetAllDestinationPoints();

      List<PointModel> pointsModel = PointModel.ConvertToPointModels(points);
      await EquipmentService.AnalyzePoints(pointsModel, command.PointsMap, context.Console);

      var breakDown = await EquipmentService.GetBreakdownTesterOrThrow(context.Console);
      await breakDown.ConnectableManager.ConnectAsync();
    }
  }
}
