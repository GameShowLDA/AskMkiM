using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mode.Device.DBC;
using Mode.ServicesTest.MESH;
using Mode.ServicesTest.MINT;
using Mode.ServicesTest.MKR;
using Mode.ServicesTest.UKSH;
using static UI.Components.Invoke.OpenFileButton;

namespace MainWindowProgram.Services
{
  /// <summary>
  /// Реализация сервиса для добавления элементов управления сервисного режима.
  /// </summary>
  public class ServiceMode
  {
    /// <summary>
    /// Сервис управления многооконным интерфейсом.
    /// </summary>
    private readonly MultiWindowService _multiWindow;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="TestService"/>.
    /// </summary>
    /// <param name="multiWindow">Сервис управления многооконным интерфейсом.</param>
    public ServiceMode(MultiWindowService multiWindow)
    {
      _multiWindow = multiWindow;
    }

    /// <summary>
    /// Добавляет элемент управления для сервисного обслуживания модуля типа МеШ в multiEditors.
    /// </summary>
    public async Task AddServicesTestMeshControlAsync() =>
      await _multiWindow.AddControlAsync("Сервисный режим: модуль MESH", new MeshControl(), TypeWindow.DeviceControl);

    /// <summary>
    /// Добавляет элемент управления для сервисного обслуживания модуля типа МИНТ в multiEditors.
    /// </summary>
    public async Task AddServicesTestMintControlAsync() =>
      await _multiWindow.AddControlAsync("Сервисный режим: модуль MINT", new MintControl(), TypeWindow.DeviceControl);

    /// <summary>
    /// Добавляет элемент управления для сервисного обслуживания модуля типа МКР в multiEditors.
    /// </summary>
    public async Task AddServicesTestMkrControlAsync() =>
      await _multiWindow.AddControlAsync("Сервисный режим: модуль MKR", new MkrControl(), TypeWindow.DeviceControl);

    /// <summary>
    /// Добавляет элемент управления для сервисного обслуживания модуля типа УКШ в multiEditors.
    /// </summary>
    public async Task AddServicesTestUkshControlAsync() =>
      await _multiWindow.AddControlAsync("Сервисный режим: модуль UKSH", new DBCManager(), TypeWindow.DeviceControl);
  }
}
