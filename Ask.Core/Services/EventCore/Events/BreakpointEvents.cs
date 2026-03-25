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

      /// <summary>
      /// Метка команды, на которую установили точку.
      /// </summary>
      public int CommandNumber { get; }

      public BreakpointSet(int lineNumber, int commandNumber)
      {
        LineNumber = lineNumber;
        CommandNumber = commandNumber;
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

      /// <summary>
      /// Метка команды, с которой убрали точку.
      /// </summary>
      public int CommandNumber { get; }

      public BreakpointRemoved(int lineNumber, int commandNumber)
      {
        LineNumber = lineNumber;
        CommandNumber = commandNumber;
      }
    }

    /// <summary>
    /// Событие, обозначающее включение точки остановки.
    /// </summary>
    public class BreakpointOn : IEvent
    {
      /// <summary>
      /// Номер строки, где точку включили.
      /// </summary>
      public int LineNumber { get; }

      /// <summary>
      /// Метка команды, где точку включили.
      /// </summary>
      public int CommandNumber { get; }

      public BreakpointOn(int lineNumber, int commandNumber)
      {
        LineNumber = lineNumber;
        CommandNumber = commandNumber;
      }
    }

    /// <summary>
    /// Событие, обозначающее выключение точки остановки.
    /// </summary>
    public class BreakpointOff : IEvent
    {
      /// <summary>
      /// Номер строки, где точку выключили.
      /// </summary>
      public int LineNumber { get; }

      /// <summary>
      /// Метка команды, где точку выключили.
      /// </summary>
      public int CommandNumber { get; }

      public BreakpointOff(int lineNumber, int commandNumber)
      {
        LineNumber = lineNumber;
        CommandNumber = commandNumber;
      }
    }
  }
}