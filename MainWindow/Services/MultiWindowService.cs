using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;
using UI.Components;
using static UI.Components.Invoke.OpenFileButton;

namespace MainWindowProgram.Services
{
  /// <summary>
  /// Реализация сервиса для управления компонентом <see cref="MultiWindowControl"/>.
  /// Позволяет добавлять пользовательские элементы управления в различные окна интерфейса.
  /// </summary>
  public class MultiWindowService
  {
    /// <summary>
    /// Компонент, управляющий отображением множества окон и вкладок.
    /// </summary>
    private readonly MultiWindowControl _multiWindowControl;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="MultiWindowService"/>.
    /// </summary>
    /// <param name="multiWindowControl">Контейнер окон и вкладок, с которым работает сервис.</param>
    public MultiWindowService(MultiWindowControl multiWindowControl)
    {
      _multiWindowControl = multiWindowControl;
    }

    /// <summary>
    /// Асинхронно добавляет элемент управления в редактор.
    /// </summary>
    /// <param name="name">Название вкладки или окна.</param>
    /// <param name="control">Элемент управления, который необходимо отобразить.</param>
    /// <param name="type">Тип окна, в котором должен отображаться элемент управления.</param>
    /// <returns>Задача, представляющая операцию добавления.</returns>
    public Task AddControlAsync(string name, UserControl control, TypeWindow type)
    {
      _multiWindowControl.AddControl(name, control, type);
      return Task.CompletedTask;
    }

    /// <summary>
    /// Добавляет новый MultiEditorControl в контейнер.
    /// </summary>
    /// <param name="filePath">Путь к файлу.</param>
    public Task OpenFileInEditor(string filePath)
    {
      _multiWindowControl.OpenFileInEditor(filePath);
      return Task.CompletedTask;
    }

    /// <summary>
    /// Создает новый файл в редакторе.
    /// </summary>
    /// <remarks>
    /// Этот метод вызывает создание нового файла в редакторе, если редактор был инициализирован.
    /// Если редактор не инициализирован, выводится сообщение об ошибке.
    /// </remarks>
    public Task CreateNewFile()
    {
      _multiWindowControl.CreateNewFile();
      return Task.CompletedTask;
    }

    public Task SaveFile()
    {
      _multiWindowControl.SaveFile();
      return Task.CompletedTask;
    }

    public Task SaveFileAs()
    {
      _multiWindowControl.SaveFileAs();
      return Task.CompletedTask;
    }

    public Task PrintFile()
    {
      _multiWindowControl.PrintFile();
      return Task.CompletedTask;
    }
  }
}
