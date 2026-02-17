namespace Ask.Core.Shared.Metadata.View
{
  public interface IMetrologyServiceView
  {
    /// <summary>
    /// Открывает пользовательский элемент управления режима КС.
    /// </summary>
    public void OpenKCModeAsync();

    /// <summary>
    /// Открывает пользовательский элемент управления режима ИЕ.
    /// </summary>
    public void OpenIEModeAsync();

    /// <summary>
    /// Открывает пользовательский элемент управления режима СИ.
    /// </summary>
    public void OpenCIModeAsync();

    /// <summary>
    /// Открывает пользовательский элемент управления режима ПР.
    /// </summary>
    public void OpenPRModeAsync();

    /// <summary>
    /// Открывает пользовательский элемент управления режима ПИ (DCW).
    /// </summary>
    public void OpenPIDCWModeAsync();

    /// <summary>
    /// Открывает пользовательский элемент управления режима ПИ (ACW).
    /// </summary>
    public void OpenPIACWModeAsync();

    /// <summary>
    /// Открывает пользовательский элемент управления режима КН (ACW).
    /// </summary>
    public void OpenKNACWModeAsync();

    /// <summary>
    /// Открывает пользовательский элемент управления режима КН (DCW).
    /// </summary>
    public void OpenKNDCWModeAsync();

    /// <summary>
    /// Открывает пользовательский элемент управления режима КН (DCW).
    /// </summary>
    public void OpenEHTModeAsync();
  }
}
