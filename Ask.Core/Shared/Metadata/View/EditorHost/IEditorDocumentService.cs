using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ask.Core.Shared.Metadata.View.EditorHost
{
  /// <summary>
  /// Управляет жизненным циклом документов: создание, открытие, сохранение и печать.
  /// </summary>
  public interface IEditorDocumentService
  {
    /// <summary>
    /// Создаёт новый пустой документ.
    /// </summary>
    void CreateNewFile();

    /// <summary>
    /// Открывает файл в редакторе.
    /// </summary>
    /// <param name="filePath">Путь к файлу.</param>
    void OpenFile(string filePath);

    /// <summary>
    /// Сохраняет активный документ.
    /// </summary>
    void SaveFile();

    /// <summary>
    /// Сохраняет активный документ под новым именем.
    /// </summary>
    void SaveFileAs();

    /// <summary>
    /// Отправляет активный документ на печать.
    /// </summary>
    void PrintFile();

    /// <summary>
    /// Открывает папку активного файла в проводнике.
    /// </summary>
    void OpenFolder();
  }
}
