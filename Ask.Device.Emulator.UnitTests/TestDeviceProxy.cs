using System.Reflection;

namespace Ask.Device.Emulator.UnitTests;

internal class TestDeviceProxy : DispatchProxy
{
  private readonly Dictionary<string, object?> _values = new();

  public void Set<T>(string propertyName, T value)
  {
    _values[$"get_{propertyName}"] = value;
  }

  protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
  {
    ArgumentNullException.ThrowIfNull(targetMethod);

    if (_values.TryGetValue(targetMethod.Name, out object? value))
    {
      return value;
    }

    Type returnType = targetMethod.ReturnType;
    return returnType == typeof(void) || !returnType.IsValueType
      ? null
      : Activator.CreateInstance(returnType);
  }
}
