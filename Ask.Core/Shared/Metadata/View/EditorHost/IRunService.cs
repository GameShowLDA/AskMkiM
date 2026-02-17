using Ask.Core.Shared.Metadata.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ask.Core.Shared.Metadata.View.EditorHost
{
  /// <summary>
  /// Управляет вкладками выполнения и результатами запуска.
  /// </summary>
  public interface IRunService
  {
    /// <summary>
    /// Добавляет вкладку выполнения.
    /// </summary>
    Task AddRunItem(IRunView runControl, EditorType type);

    /// <summary>
    /// Закрывает вкладку выполнения.
    /// </summary>
    Task CloseRunItem(IRunView runControl, EditorType type);
  }
}
