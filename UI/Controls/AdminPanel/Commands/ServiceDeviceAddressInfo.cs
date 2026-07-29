namespace UI.Controls.AdminPanel.Commands
{
  /// <summary>
  /// Содержит адрес и идентификационные сведения сервисного устройства.
  /// </summary>
  public sealed record ServiceDeviceAddressInfo(
    string Name,
    int ChassisNumber,
    int ModuleNumber,
    string Address)
  {
    /// <summary>
    /// Текст устройства для списка автодополнения.
    /// </summary>
    public string DisplayText =>
      $"{Name} · шасси №{ChassisNumber} · модуль №{ModuleNumber} · {Address}";
  }
}
