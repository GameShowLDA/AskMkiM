using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mode.ServicesTest.MESH
{
  public partial class MeshControl
  {
    /// <summary>
    /// Начальная настройка: устройство не инициализ., ComboBox на "<пусто>", 
    /// кнопка питания отключена и т.д.
    /// </summary>
    private void InitializeMeshUI()
    {
      isMeshInitialized = false;
      currentDeviceName = string.Empty;

      CmbMeshDevice.SelectedItem = "<пусто>";
      // Устройство не выбрано => кнопка питания неактивна
      //BtnMeshPower.IsEnabled = false;
      //BtnMeshPower.Content = "Включение питания";
    }

    /// <summary>
    /// Обновляет UI: если enable=true, значит устройство выбрано и мы активируем кнопки,
    /// иначе - блокируем часть элементов. 
    /// </summary>
    public async Task UpdateMeshUI(bool enable, bool skipLog)
    {
      // Основной флаг
      isMeshInitialized = enable;

      // Кнопка питания доступна, только если устройство инициализировано
      BtnMeshPower.IsEnabled = enable;

      // ComboBox логично оставить активным, когда устройство не инициализировано, 
      // чтобы пользователь мог что-то выбрать.
      // Но если уже инициализировали, при желании можно и оставить ComboBox активным, 
      // так как не указано, что при включённом питании нужно его блокировать. 
      // Если нужно - добавляем аналитику, как done in Uksh.
      // Пока оставим ComboBox без блокировки, т.к. не сказано иного.

      if (!skipLog)
      {
        if (enable)
        {
          await ShowMessageAsync($"Инициализация устройства: {currentDeviceName}");
        }
        else
        {
          if (!string.IsNullOrEmpty(currentDeviceName))
            await ShowMessageAsync($"Отключение устройства: {currentDeviceName}");
        }
      }
    }
  }
}
