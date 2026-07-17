namespace Ask.Core.Services.Validation.Devices;

public sealed class RelaySwitchModuleConfigurationValidator : IRelaySwitchModuleConfigurationValidator
{
  public const int MinimumPointCount = 1;
  public const int MaximumPointCount = 4096;

  public bool TryValidatePointCount(int pointCount, out string errorMessage)
  {
    if (pointCount is >= MinimumPointCount and <= MaximumPointCount)
    {
      errorMessage = string.Empty;
      return true;
    }

    errorMessage = $"Количество точек должно быть от {MinimumPointCount} до {MaximumPointCount}.";
    return false;
  }
}
