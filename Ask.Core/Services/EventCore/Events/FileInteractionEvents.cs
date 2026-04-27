using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.EventInterfaces;
using System.Windows.Controls;

namespace Ask.Core.Services.EventCore.Events
{
  /// <summary>
  /// События, связанные с файловыми операциями, включая открытие файлов,
  /// повторное открытие в редакторе и работу с протоколами.
  /// </summary>
  /// <remarks>
  /// Эти события используются для взаимодействия между пользовательским интерфейсом
  /// и внутренней логикой при работе с файлами, такими как OPK-документы и протоколы испытаний.
  /// </remarks>
  public static class FileInteractionEvents
  {
    /// <summary>
    /// Событие, генерируемое при необходимости открыть новый OPK-файл.
    /// </summary>
    public class OpenOpk : IEvent
    {
      public UserControl Control { get; }
      public string FileName { get; }

      public OpenOpk(UserControl control, string fileName)
      {
        Control = control;
        FileName = fileName;
      }
    }

    /// <summary>
    /// Событие, генерируемое при необходимости повторного открытия файла в текстовом редакторе.
    /// </summary>
    public class OpenFileInEditorAgain : IEvent
    {
      public string FilePath { get; }

      public OpenFileInEditorAgain(string filePath)
      {
        FilePath = filePath;
      }
    }

    /// <summary>
    /// Событие, генерируемое при просмотре протокола испытаний в новом редакторе.
    /// </summary>
    public class ViewProtocol : IEvent
    {
      public ProtocolModel Protocol { get; }

      public ViewProtocol(ProtocolModel protocol)
      {
        Protocol = protocol;
      }
    }

    /// <summary>
    /// Событие, генерируемое при запросе информации о протоколе.
    /// </summary>
    public class GetProtocolInfo : IEvent
    {
      public ProtocolModel Protocol { get; }

      public GetProtocolInfo(ProtocolModel protocol)
      {
        Protocol = protocol;
      }
    }

    /// <summary>
    /// Событие, генерируемое при закрытии окна с информацией о протоколе.
    /// </summary>
    public class ProtocolInfoClose : IEvent
    {
      public string Number { get; }
      public string Executor { get; }
      public string Agent { get; }
      public string Customer { get; }
      public ProtocolModel Protocol { get; }

      public ProtocolInfoClose(string number, string executor, string agent, string customer, ProtocolModel protocol)
      {
        Number = number;
        Executor = executor;
        Agent = agent;
        Customer = customer;
        Protocol = protocol;
      }
    }
  }
}
