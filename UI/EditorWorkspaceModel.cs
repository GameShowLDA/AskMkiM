using System.Collections.ObjectModel;
using System.Windows.Controls;
using UI.Components;
using UI.Components.Invoke;

namespace UI
{
  public class EditorWorkspaceModel
  {
    /// <summary>
    /// Получает или задает список объектов кнопки, представляющей открытую страницу. 
    /// </summary>
    public ObservableCollection<OpenFileButton> OpenPages { get; set; } = new ObservableCollection<OpenFileButton>();

    /// <summary>
    /// Получает или задает список пользовательских контролов, которые отображаются в приложении.
    /// </summary>
    public ObservableCollection<UserControl> UserControls { get; set; } = new ObservableCollection<UserControl> { };

    /// <summary>
    /// Получает или задает словарь, где имя файла - ключ, а путь к файлу - значение.
    /// </summary>
    public Dictionary<string, string> FilePaths { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Интерфейс для создания связи между файловым мененджером и Multi Editor Control.
    /// </summary>
    public MultiEditorControl MultiEditorControl;

    public EditorWorkspaceModel(MultiEditorControl multiEditorControl)
    {
      this.MultiEditorControl = multiEditorControl;
    }
  }
}
