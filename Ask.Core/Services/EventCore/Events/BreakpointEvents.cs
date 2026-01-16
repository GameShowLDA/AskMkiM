using Ask.Core.Shared.Interfaces.EventInterfaces;

namespace Ask.Core.Services.EventCore.Events
{
  /// <summary>
  /// Содержит события, связанные с установкой и снятием точек останова (красных маркеров) в редакторе.
  /// </summary>
  public static class BreakpointEvents
  {
    /// <summary>
    /// Событие, обозначающее установку точки останова на строку.
    /// </summary>
    public class BreakpointSet : IEvent
    {
      /// <summary>
      /// Номер строки, на которую установили точку.
      /// </summary>
      public int LineNumber { get; }

      public BreakpointSet(int lineNumber)
      {
        LineNumber = lineNumber;
      }
    }

    /// <summary>
    /// Событие, обозначающее снятие точки останова со строки.
    /// </summary>
    public class BreakpointRemoved : IEvent
    {
      /// <summary>
      /// Номер строки, с которой убрали точку.
      /// </summary>
      public int LineNumber { get; }

      public BreakpointRemoved(int lineNumber)
      {
        LineNumber = lineNumber;
      }
    }
  }
}