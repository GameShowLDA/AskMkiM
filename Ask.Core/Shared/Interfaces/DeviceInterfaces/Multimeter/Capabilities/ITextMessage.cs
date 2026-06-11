using Ask.Core.Shared.Interfaces.UiInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter.Capabilities
{
  public interface ITextMessage
  {
    /// <summary>
    /// Асинхронно устанавливает режим измерения сопротивления.
    /// </summary>
    /// <returns>Задача, завершающаяся после установки режима.</returns>
    Task Message(string text, IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Асинхронно устанавливает режим измерения сопротивления.
    /// </summary>
    /// <returns>Задача, завершающаяся после установки режима.</returns>
    Task ClearMessage(IUserInteractionService? userMessageService = null);
  }
}
