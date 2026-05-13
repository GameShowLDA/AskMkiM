using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Ask.UI.Features.Archive.ViewModels
{
  /// <summary>
  /// ObservableCollection с поддержкой пакетной замены элементов.
  /// </summary>
  /// <typeparam name="T">Тип элементов коллекции.</typeparam>
  public sealed class RangeObservableCollection<T> : ObservableCollection<T>
  {

    /// <summary>
    /// Полностью заменяет содержимое коллекции новым набором элементов.
    /// </summary>
    /// <param name="items">Новая коллекция элементов.</param>
    public void ReplaceRange(IEnumerable<T> items)
    {
      CheckReentrancy();
      Items.Clear();

      foreach (var item in items)
      {
        Items.Add(item);
      }

      OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
      OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
      OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }
  }
}
