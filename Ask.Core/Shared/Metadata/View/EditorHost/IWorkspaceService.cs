using Ask.Core.Shared.Metadata.Enums.UiEnums;
using Ask.Core.Shared.Metadata.Static;
using System.Windows.Controls;

namespace Ask.Core.Shared.Metadata.View.EditorHost
{
  /// <summary>
  /// Управляет размещением пользовательских контролов во внутренних окнах приложения.
  /// Отвечает только за layout (dock, вкладки, панели), не за содержимое документов.
  /// </summary>
  public interface IWorkspaceService
  {
    /// <summary>
    /// Добавляет пользовательский контрол в рабочее пространство.
    /// </summary>
    /// <param name="name">Отображаемое имя вкладки или окна.</param>
    /// <param name="control">UI-контрол, который необходимо показать.</param>
    /// <param name="type">Тип окна/области размещения.</param>
    void AddControl(string name, UserControl control, TypeWindow type, string description = null);

    /// <summary>
    /// Удаляет контрол по типу редактора.
    /// </summary>
    /// <param name="editorType">Тип вкладки.</param>
    void RemoveControl(EditorType editorType);
  }
}
