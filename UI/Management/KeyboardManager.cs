using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using UI.Controls.Protocol;
using static AppConfiguration.Base.EventAggregator;

namespace UI.Management
{
  /// <summary>
  /// Класс для управления обработкой нажатий клавиш в приложении.
  /// Позволяет обрабатывать команды для пошагового режима выполнения.
  /// </summary>
  internal class KeyboardManager
  {
    /// <summary>
    /// Ожидает нажатия клавиш для пошагового режима.
    /// </summary>
    /// <param name="commandBindings">Коллекция привязок команд.</param>
    /// <param name="inputBindings">Коллекция привязок клавиш.</param>
    /// <returns>Возвращает:
    /// 1 - выполнение без пошагового режима,
    /// 2 - выполнение пошагового режима без захода,
    /// 3 - выполнение пошагового режима с заходом.</returns>
    static public async Task<byte> WaitForFunctionKeyPress(CommandBindingCollection commandBindings, InputBindingCollection inputBindings)
    {
      var keyPressTaskSource = new TaskCompletionSource<byte>();

      if (Application.Current.Dispatcher.CheckAccess())
      {
        AddKeyBindings(commandBindings, inputBindings, keyPressTaskSource);
      }
      else
      {
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
          AddKeyBindings(commandBindings, inputBindings, keyPressTaskSource);
        }, DispatcherPriority.Normal);
      }

      RaiseInfoMessage("Включен пошаговый режим. Ожидание нажатия клавиш F10 или F11");
      return await keyPressTaskSource.Task.ConfigureAwait(false);
    }

    /// <summary>
    /// Метод для добавления команд и привязок клавиш.
    /// </summary>
    private static void AddKeyBindings(CommandBindingCollection commandBindings, InputBindingCollection inputBindings, TaskCompletionSource<byte> keyPressTaskSource)
    {
      var commandF5 = new RoutedCommand("MyCommandF5", typeof(ProtocolUI));
      commandBindings.Add(new CommandBinding(commandF5, (s, e) =>
      {
        RaiseInfoMessage("Нажата клавиша F5", true);
        keyPressTaskSource.TrySetResult(1);
      }));

      var commandF10 = new RoutedCommand("MyCommandF10", typeof(ProtocolUI));
      commandBindings.Add(new CommandBinding(commandF10, (s, e) =>
      {
        RaiseInfoMessage("Нажата клавиша F10", true);
        keyPressTaskSource.TrySetResult(2);
      }));

      var commandF11 = new RoutedCommand("MyCommandF11", typeof(ProtocolUI));
      commandBindings.Add(new CommandBinding(commandF11, (s, e) =>
      {
        RaiseInfoMessage("Нажата клавиша F11", true);
        keyPressTaskSource.TrySetResult(3);
      }));

      var inputF5 = new KeyBinding(commandF5, Key.F5, ModifierKeys.None);
      var inputF10 = new KeyBinding(commandF10, Key.F10, ModifierKeys.None);
      var inputF11 = new KeyBinding(commandF11, Key.F11, ModifierKeys.None);

      inputBindings.Add(inputF5);
      inputBindings.Add(inputF10);
      inputBindings.Add(inputF11);
    }
  }
}
