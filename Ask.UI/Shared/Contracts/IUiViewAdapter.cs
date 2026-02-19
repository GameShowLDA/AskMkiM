using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ask.UI.Shared.Contracts
{
  namespace Ask.UI.Shared.Contracts
  {
    /// <summary>
    /// Универсальный адаптер визуального представления.
    ///
    /// Позволяет передавать UI-элемент между слоями приложения без зависимости
    /// от конкретной UI-технологии (WPF, Avalonia, WinUI и др.).
    ///
    /// Хост интерфейса интерпретирует возвращаемый объект в соответствии
    /// с используемым UI-фреймворком.
    /// Логика приложения не должна выполнять приведение типов напрямую.
    /// </summary>
    public interface IUiViewAdapter
    {
      /// <summary>
      /// Нативный визуальный объект конкретного UI-фреймворка.
      /// Тип определяется реализацией адаптера.
      /// </summary>
      object NativeView { get; }
    }
  }

}
