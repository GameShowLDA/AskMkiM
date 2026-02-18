using Ask.Core.Shared.Metadata.Enums.FileEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ask.Core.Shared.Metadata.View.EditorHost
{
  /// <summary>
  /// Абстрактное представление текстового редактора.
  /// Предоставляет операции навигации, редактирования, подсветки и работы с точками остановки,
  /// не раскрывая конкретную UI-технологию (WPF, Avalonia, WinUI и т.д.).
  /// Используется внешними подсистемами (транслятор, раннер, анализаторы) как UI-адаптер.
  /// </summary>
  public interface ITextEditorView
  {
    /// <summary>
    /// Проксирует событие изменения текста редактора.
    /// Позволяет внешнему коду подписываться на обновления содержимого,
    /// передавая обработчики напрямую во внутренний AvalonEdit.
    /// </summary>
    public event EventHandler TextChanged;

    /// <summary>
    /// Определяет тип файла, связанный с данным экземпляром редактора.
    /// Используется для выбора схемы подсветки синтаксиса и других
    /// специфичных для формата настроек. Устанавливается при создании
    /// редактора и доступно только для чтения извне.
    /// </summary>
    public FileType FileType { get; }

    /// <summary>
    /// Модель данных, описывающая состояние и параметры текущего
    /// текстового редактора. Может содержать информацию о файле,
    /// настройках отображения и других связанных данных.
    /// Свойство доступно для чтения и записи.
    /// </summary>
    public TextEditorModel TextEditorModel { get; set; }
  }
}
