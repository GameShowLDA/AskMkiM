namespace AppManager.Data.Theme
{
  public class ThemeModel
  {
    /// <summary>
    /// Получает или задает основной цвет темы.
    /// </summary>
    public string PrimaryColor { get; set; }

    /// <summary>
    /// Получает или задает вторичный цвет темы.
    /// </summary>
    public string SecondaryColor { get; set; }

    /// <summary>
    /// Получает или задает цвет переднего плана (текста) темы.
    /// </summary>
    public string ForegroundColor { get; set; }

    /// <summary>
    /// Получает или задает цвет активных элементов темы.
    /// </summary>
    public string ActiveColor { get; set; }

    /// <summary>
    /// Получает или задает цвет выбранных элементов темы.
    /// </summary>
    public string IsCheckedColor { get; set; }
  }
}
