namespace Ask.Core.Services.Validation.Devices;

public interface IRelaySwitchModuleConfigurationValidator
{
  bool TryValidatePointCount(int pointCount, out string errorMessage);
}
