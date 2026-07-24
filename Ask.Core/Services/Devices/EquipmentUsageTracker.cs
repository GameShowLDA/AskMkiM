using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using System.Threading;

namespace Ask.Core.Services.Devices;

/// <summary>
/// Отслеживает оборудование, использованное текущим выполнением.
/// </summary>
public static class EquipmentUsageTracker
{
  /// <summary>
  /// Текущий сеанс учёта оборудования.
  /// </summary>
  private static readonly AsyncLocal<EquipmentUsageSession?> CurrentSession = new();

  /// <summary>
  /// Открывает область учёта оборудования для нового выполнения.
  /// </summary>
  /// <returns>Сеанс учёта оборудования.</returns>
  public static EquipmentUsageSession BeginSession()
  {
    var session = new EquipmentUsageSession(CurrentSession.Value);
    CurrentSession.Value = session;
    return session;
  }

  /// <summary>
  /// Регистрирует обращение к устройству в текущем выполнении.
  /// </summary>
  /// <param name="device">Использованное устройство.</param>
  public static void Register(IDevice? device)
  {
    if (device == null)
    {
      return;
    }

    CurrentSession.Value?.Register(device);
  }

  /// <summary>
  /// Восстанавливает предыдущий сеанс учёта оборудования.
  /// </summary>
  /// <param name="session">Закрываемый сеанс.</param>
  /// <param name="previousSession">Предыдущий сеанс.</param>
  internal static void CloseSession(EquipmentUsageSession session, EquipmentUsageSession? previousSession)
  {
    if (ReferenceEquals(CurrentSession.Value, session))
    {
      CurrentSession.Value = previousSession;
    }
  }
}

/// <summary>
/// Хранит оборудование, использованное одним выполнением.
/// </summary>
public sealed class EquipmentUsageSession : IDisposable
{
  /// <summary>
  /// Объект синхронизации доступа к коллекции устройств.
  /// </summary>
  private readonly object _sync = new();

  /// <summary>
  /// Набор уникальных экземпляров использованного оборудования.
  /// </summary>
  private readonly HashSet<IDevice> _deviceSet = new(ReferenceEqualityComparer.Instance);

  /// <summary>
  /// Оборудование в порядке первого обращения.
  /// </summary>
  private readonly List<IDevice> _devices = new();

  /// <summary>
  /// Предыдущий сеанс учёта оборудования.
  /// </summary>
  private readonly EquipmentUsageSession? _previousSession;

  /// <summary>
  /// Признак завершённого сеанса.
  /// </summary>
  private bool _disposed;

  /// <summary>
  /// Создаёт сеанс учёта оборудования.
  /// </summary>
  /// <param name="previousSession">Предыдущий сеанс.</param>
  internal EquipmentUsageSession(EquipmentUsageSession? previousSession)
  {
    _previousSession = previousSession;
  }

  /// <summary>
  /// Возвращает снимок использованного оборудования.
  /// </summary>
  /// <returns>Устройства в порядке первого обращения.</returns>
  public IReadOnlyList<IDevice> GetUsedDevices()
  {
    lock (_sync)
    {
      return _devices.ToArray();
    }
  }

  /// <summary>
  /// Добавляет устройство в набор использованного оборудования.
  /// </summary>
  /// <param name="device">Использованное устройство.</param>
  internal void Register(IDevice device)
  {
    lock (_sync)
    {
      if (!_disposed)
      {
        if (_deviceSet.Add(device))
        {
          _devices.Add(device);
        }
      }
    }
  }

  /// <summary>
  /// Закрывает область учёта оборудования.
  /// </summary>
  public void Dispose()
  {
    lock (_sync)
    {
      if (_disposed)
      {
        return;
      }

      _disposed = true;
    }

    EquipmentUsageTracker.CloseSession(this, _previousSession);
  }
}
