using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ControlCommandAnalyser
{
  /// <summary>
  /// Глобальное статическое хранилище состояния для исполнения команд контроля.
  /// Используется для хранения результатов диалогов, состояний переходов и т.п.
  /// </summary>
  public static class CommandExecutionState
  {
    static public MessageBoxResult? LastCuResult { get; set; }

  }
}
