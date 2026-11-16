using Ask.Core.Shared.DTO.Executor;
using Ask.Engine.ControlCommandAnalyser.Model;
using System.Text;

namespace Ask.Engine.ControlCommandAnalyser.ComandBody;

/// <summary>
/// Формирует каноничное тело команды ВИ.
/// </summary>
internal sealed class ViCommandBodyBuilder : ICommandBody
{
  public bool CanCreate(BaseCommandModel model) => model is ViCommandModel;

  public StringBuilder Create(BaseCommandModel model, StringBuilder source)
  {
    if (model is not ViCommandModel vi || !vi.PowerSupplyNumber.HasValue)
      return source;

    if (vi.AlgorithmKey.Contains("Б")
        && source.Length >= 2
        && source[^2] == ',')
    {
      source.Remove(source.Length - 2, 1);
    }

    var parameters = new List<string> { vi.PowerSupplyNumber.Value.ToString() };
    if (!string.IsNullOrWhiteSpace(vi.VoltageSource))
      parameters.Add(vi.VoltageSource);
    if (!string.IsNullOrWhiteSpace(vi.CurrentSource))
      parameters.Add(vi.CurrentSource);
    if (vi.PositiveBus.HasValue)
      parameters.Add($"+{vi.PositiveBus}");
    if (vi.NegativeBus.HasValue)
      parameters.Add($"-{vi.NegativeBus}");

    return source.Append($"*{string.Join(", ", parameters)}*");
  }
}
