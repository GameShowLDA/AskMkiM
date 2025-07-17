using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ControlCommandAnalyser.Model;
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
      var points = command.GetAllDestinationPoints();

      List<PointModel> pointsModel = PointModel.ConvertToPointModels(points);
      await EquipmentService.AnalyzePoints(pointsModel, context.Console);

    }
  }
}
