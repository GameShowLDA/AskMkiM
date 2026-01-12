using Ask.Core.Shared.Interfaces.ErrorInterfaces;

namespace Ask.Engine.ControlCommandAnalyser.Model.Interface
{
  public interface IError
  {
    IPointError PointErrors { get; }
  }
}
