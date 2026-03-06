using System.Windows;

namespace Ask.Engine.ControlCommandAnalyser
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
