using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AppConfiguration.Interface;
using Utilities.Interface;
using Utilities.Models;
using static Utilities.LoggerUtility;

namespace UI.Components.ProtocolListBox
{
  /// <summary>
  /// Логика взаимодействия для ProtocolListBoxUI.xaml
  /// </summary>
  public partial class ProtocolListBoxUI : UserControl, IUserMessageService
  {
    public ObservableCollection<ShowMessageModel> Messages { get; } = new();
    public string Header { get; set; }

    public bool HasRetryAction => throw new NotImplementedException();

    public bool ClickRetry { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public IButtonService ButtonService { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public ProtocolListBoxUI()
    {
      InitializeComponent();
    }

    /// <summary>
    /// Удаляет указанное количество последних строк из списка протокола.
    /// </summary>
    /// <param name="count">Количество строк для удаления. По умолчанию 1.</param>
    /// <returns>Фактическое количество удалённых строк.</returns>
    public Task<int> RemoveLastLinesAsync(int count = 1)
    {
      int removed = 0;

      Application.Current.Dispatcher.Invoke(() =>
      {
        int linesToRemove = Math.Min(count, Messages.Count);
        for (int i = 0; i < linesToRemove; i++)
        {
          Messages.RemoveAt(Messages.Count - 1);
          removed++;
        }
      });

      return Task.FromResult(removed);
    }

    public async Task ClearAsync()
    {
      await Application.Current.Dispatcher.InvokeAsync(() =>
      {
        Messages.Clear();
        LogInformation("Протокол полностью очищен.");
      });
    }

    public async Task<bool> RemoveLineContainingTextAsync(string textToRemove)
    {
      return await Application.Current.Dispatcher.InvokeAsync(() =>
      {
        try
        {
          var target = Messages.FirstOrDefault(m =>
              (!string.IsNullOrEmpty(m.Header) && m.Header.Contains(textToRemove, StringComparison.OrdinalIgnoreCase)) ||
              (!string.IsNullOrEmpty(m.Message) && m.Message.Contains(textToRemove, StringComparison.OrdinalIgnoreCase)));

          if (target != null)
          {
            Messages.Remove(target);
            LogInformation($"Строка '{textToRemove}' найдена и удалена.");
            return true;
          }

          LogWarning($"Строка '{textToRemove}' не найдена.");
          return false;
        }
        catch (Exception ex)
        {
          LogException("Ошибка при удалении строки", ex);
          return false;
        }
      });
    }

    public async Task AppendLineAsync(ShowMessageModel showMessageModel)
    {
      await Application.Current.Dispatcher.InvokeAsync(() =>
      {
        Messages.Add(showMessageModel);
        ProtocolListBox.ScrollIntoView(showMessageModel);
      });
    }

    public Task AppendEmptyLineAsync(int indentLevel = 0)
    {
      var emptyLine = new ShowMessageModel
      {
        Header = string.Empty,
        Message = string.Empty,
        Time = string.Empty,
        HeaderColor = Colors.Transparent,
        MessageColor = Colors.Transparent,
        IndentLevel = indentLevel
      };

      return AppendLineAsync(emptyLine);
    }

    public async Task ShowMessageAsync(ShowMessageModel model, bool IsBlockStart = false, bool SkipStepModeCheck = false, bool skipPause = false)
    {
      await AppendLineAsync(model);
    }

    /// <summary>
    /// Возвращает весь текст протокола в виде строки с учётом табуляции.
    /// </summary>
    /// <returns>Общий текст протокола.</returns>
    public Task<string> GetPlainTextAsync()
    {
      return Task.FromResult(string.Join(Environment.NewLine, Messages.Select(m =>
      {
        string indent = new string(' ', m.IndentLevel * 2);
        string header = string.IsNullOrWhiteSpace(m.Header) ? "" : $"{m.Header}: ";
        return $"{indent}{header}{m.Message} | {m.Time}";
      })));
    }

    public Task<bool> AwaitAdminDecisionAsync(string message)
    {
      throw new NotImplementedException();
    }

    public Task<bool> WaitAdminButtonAsync()
    {
      throw new NotImplementedException();
    }

    public void RegisterRetryAction(Func<Task> retryAction)
    {
      throw new NotImplementedException();
    }

    public Task TryInvokeRetryAsync()
    {
      throw new NotImplementedException();
    }

    public void ClearRetryAction()
    {
      throw new NotImplementedException();
    }

    public Task<IUserMessageService.UserAction> WaitUserActionAsync()
    {
      return null;
    }

    public CancellationToken GetCancellationToken()
    {
      throw new NotImplementedException();
    }
  }
}
