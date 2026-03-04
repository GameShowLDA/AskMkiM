using Ask.Core.Shared.Interfaces.ErrorInterfaces;

namespace Ask.Core.Shared.Interfaces.ExecutionInterfaces
{
  public interface IError
  {
    IPointError PointErrors { get; }
  }
}
