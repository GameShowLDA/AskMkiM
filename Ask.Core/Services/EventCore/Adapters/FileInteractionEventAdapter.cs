using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using Ask.Core.Shared.DTO.Protocol;
using System.Windows.Controls;

namespace Ask.Core.Services.EventCore.Adapters
{
  /// <summary>
  /// Адаптер для генерации событий <see cref="FileInteractionEvents"/>. 
  /// Предоставляет методы для вызова событий, связанных с файловыми операциями,
  /// такими как открытие OPK-файлов, сравнение файлов и работа с протоколами.
  /// </summary>
  public static class FileInteractionEventAdapter
  {
    /// <summary>
    /// Генерирует событие открытия OPK-файла.
    /// </summary>
    /// <param name="control">UI-элемент, в который будет загружен файл.</param>
    /// <param name="fileName">Имя открываемого OPK-файла.</param>
    /// <example>
    /// <code>
    /// FileInteractionEventAdapter.RaiseOpenOpk(editorControl, "project.opk");
    /// </code>
    /// </example>
    public static void RaiseOpenOpk(UserControl control, string fileName)
      => EventAggregator.Publish(new FileInteractionEvents.OpenOpk(control, fileName));

    /// <summary>
    /// Генерирует событие сравнения двух файлов.
    /// </summary>
    /// <param name="firstFilePath">Путь к первому файлу.</param>
    /// <param name="secondFilePath">Путь ко второму файлу.</param>
    /// <example>
    /// <code>
    /// FileInteractionEventAdapter.RaiseCompareFiles("old.txt", "new.txt");
    /// </code>
    /// </example>
    public static void RaiseCompareFiles(string firstFilePath, string secondFilePath)
      => EventAggregator.Publish(new FileInteractionEvents.CompareFiles(firstFilePath, secondFilePath));

    /// <summary>
    /// Генерирует событие повторного открытия файла в текстовом редакторе.
    /// </summary>
    /// <param name="filePath">Путь к файлу, который нужно открыть повторно.</param>
    /// <example>
    /// <code>
    /// FileInteractionEventAdapter.RaiseOpenFileInEditorAgain("report.txt");
    /// </code>
    /// </example>
    public static void RaiseOpenFileInEditorAgain(string filePath)
      => EventAggregator.Publish(new FileInteractionEvents.OpenFileInEditorAgain(filePath));

    /// <summary>
    /// Генерирует событие просмотра протокола испытаний в новом редакторе.
    /// </summary>
    /// <param name="protocol">Модель протокола, который необходимо открыть.</param>
    /// <example>
    /// <code>
    /// FileInteractionEventAdapter.RaiseViewProtocol(protocolModel);
    /// </code>
    /// </example>
    public static void RaiseViewProtocol(ProtocolModel protocol)
      => EventAggregator.Publish(new FileInteractionEvents.ViewProtocol(protocol));

    /// <summary>
    /// Генерирует событие запроса информации о протоколе.
    /// </summary>
    /// <param name="protocol">Модель протокола, информацию о котором требуется получить.</param>
    /// <example>
    /// <code>
    /// FileInteractionEventAdapter.RaiseGetProtocolInfo(protocolModel);
    /// </code>
    /// </example>
    public static void RaiseGetProtocolInfo(ProtocolModel protocol)
      => EventAggregator.Publish(new FileInteractionEvents.GetProtocolInfo(protocol));

    /// <summary>
    /// Генерирует событие закрытия окна информации о протоколе.
    /// </summary>
    /// <param name="number">Номер протокола.</param>
    /// <param name="executor">Исполнитель испытаний.</param>
    /// <param name="agent">Агент (организация, проводившая испытания).</param>
    /// <param name="customer">Заказчик испытаний.</param>
    /// <param name="protocol">Модель закрываемого протокола.</param>
    /// <example>
    /// <code>
    /// FileInteractionEventAdapter.RaiseProtocolInfoClose("№123", "Иванов", "НИИ Электрон", "АО Аксион", protocolModel);
    /// </code>
    /// </example>
    public static void RaiseProtocolInfoClose(string number, string executor, string agent, string customer, ProtocolModel protocol)
      => EventAggregator.Publish(new FileInteractionEvents.ProtocolInfoClose(number, executor, agent, customer, protocol));
  }
}
