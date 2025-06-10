namespace Mode.ServicesTest.MESH
{
  /// <summary>
  /// Управляет состоянием устройства МеШ, включая его инициализацию, сброс параметров и обновление пользовательского интерфейса.
  /// </summary>
  public partial class MeshControl
  {
    /// <summary>
    /// Выполняет начальную настройку пользовательского интерфейса для устройства MESH.
    /// Устанавливает, что устройство не инициализировано, ComboBox выбран в состоянии "<пусто>".
    /// Кнопка питания может быть отключена по умолчанию (если требуется).
    /// </summary>
    private void InitializeMeshUI()
    {
      isMeshInitialized = false;
      currentDeviceName = string.Empty;

      CmbMeshDevice.SelectedItem = "<пусто>";
      // По умолчанию устройство не выбрано, поэтому можно отключить кнопку питания.
      // BtnMeshPower.IsEnabled = false;
      // BtnMeshPower.Content = "Включение питания";
    }

    /// <summary>
    /// Обновляет состояние пользовательского интерфейса в зависимости от выбранного состояния устройства.
    /// Если <paramref name="enable"/> равно true, устройство считается инициализированным и активируются необходимые элементы управления;
    /// иначе, часть элементов блокируется.
    /// Если <paramref name="skipLog"/> равно false, выводится сообщение о смене состояния устройства.
    /// </summary>
    /// <param name="enable">Если true, устройство выбрано и инициализировано.</param>
    /// <param name="skipLog">Если true, логирование состояния не производится.</param>
    public async Task UpdateMeshUI(bool enable, bool skipLog)
    {
      // Основной флаг инициализации устройства.
      isMeshInitialized = enable;

      // Кнопка питания становится доступной только при инициализации устройства.
      BtnMeshPower.IsEnabled = enable;

      // ComboBox оставляем активным для выбора устройства, если оно не инициализировано.
      // (При необходимости можно добавить блокировку, как реализовано в других контролах.)

      if (!skipLog)
      {
        if (enable)
        {
          await ShowMessageAsync($"Инициализация устройства: {currentDeviceName}");
        }
        else if (!string.IsNullOrEmpty(currentDeviceName))
        {
          await ShowMessageAsync($"Отключение устройства: {currentDeviceName}");
        }
      }
    }
  }
}