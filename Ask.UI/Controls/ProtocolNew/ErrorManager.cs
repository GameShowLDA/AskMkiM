using Ask.Core.Services.Errors.Models;
using Ask.Core.Services.EventCore.Adapters;
using System.Windows;

namespace Ask.UI.Controls.ProtocolNew
{
  public class ErrorManager
  {
    private int ErrorCount { get; set; } = 0;
    private Ask.UI.Controls.ErrorList.ErrorListControl ErrorListBoxVertical;

    public void AddError(ErrorItem errorItem)
    {
      Application.Current.Dispatcher?.Invoke(() =>
      {
        ErrorListBoxVertical.Items.Add(errorItem);
        ErrorCount++;

        if (ErrorCount > 0)
        {
          MessageEventAdapter.RaiseInfoMessage($"Общее кол-во ошибок: {ErrorCount}");
        }
      });
    }

    internal void ErrorClear()
    {
      Application.Current.Dispatcher?.Invoke(() =>
      {
        ErrorListBoxVertical.Items.Clear();
        ErrorCount = 0;
      });
    }

    public ErrorManager(Ask.UI.Controls.ErrorList.ErrorListControl errorListBoxVertical)
    {
      ErrorCount = 0;
      ErrorListBoxVertical = errorListBoxVertical;
    }
  }
}

