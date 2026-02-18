using Ask.Core.Shared.Metadata.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ask.Core.Shared.Metadata.View.EditorHost
{
  /// <summary>
  /// Управляет сессиями выполнения в хосте редактора:
  /// создаёт, отображает и закрывает вкладки с результатами запуска.
  /// </summary>
  public interface IRunService
  {
    /// <summary>
    /// Открывает вкладку выполнения для указанного представления запуска.
    /// </summary>
    /// <param name="runControl">Представление выполнения (output/console/result view).</param>
    /// <param name="type">Тип редакторной области, в которой размещается вкладка.</param>
    Task AddRunItem(IRunView runControl, EditorType type);

    /// <summary>
    /// Закрывает вкладку выполнения.
    /// </summary>
    /// <param name="runControl">Представление выполнения.</param>
    /// <param name="type">Тип редакторной области.</param>
    Task CloseRunItem(IRunView runControl, EditorType type);
  }
}
