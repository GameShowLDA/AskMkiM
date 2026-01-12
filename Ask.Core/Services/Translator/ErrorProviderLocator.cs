using Ask.Core.Shared.Interfaces.ExecutionInterfaces;

namespace Ask.Core.Services.Translator
{
  public static class ErrorProviderLocator
  {
    public static IMeasurementErrorProvider? Provider { get; set; }
  }
}
