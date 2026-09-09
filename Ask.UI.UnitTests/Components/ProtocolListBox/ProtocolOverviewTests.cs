using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.DTO.Protocol;
using Ask.UI.Components.ProtocolListBox;
using Ask.UI.Controls.TextEditorControl;
using System.Windows.Controls;
using System.Windows.Media;

namespace Ask.UI.UnitTests.Components.ProtocolListBox;

public sealed class ProtocolOverviewTests
{
  [Theory]
  [InlineData(ShowMessageModel.MessageType.Info, false, "Количество брака: 0", false)]
  [InlineData(ShowMessageModel.MessageType.Info, false, "[БРАК] R = 20", true)]
  [InlineData(ShowMessageModel.MessageType.Error, false, "Нет связи", true)]
  [InlineData(ShowMessageModel.MessageType.Info, true, "Превышен предел", true)]
  [InlineData(ShowMessageModel.MessageType.Success, false, "[БРАК] отсутствует", false)]
  public void ClassifiesStructuredErrorsAndLegacyMarkers(
    ShowMessageModel.MessageType status, bool executionError, string text, bool expected)
  {
    var message = new ShowMessageModel { MessageColor = Colors.Red, Status = status,
      Header = text, ExecutionError = executionError };
    Assert.Equal(expected, ProtocolListBoxUI.IsOverviewError(message));
  }

  [Fact]
  public void NavigationRevealsErrorInsideCollapsedCommand()
  {
    RunInSta(() =>
    {
      bool original = ProtocolConfig.GetCommandHeadersInProtocol();
      try
      {
        ProtocolConfig.SetCommandHeadersInProtocol(true);
        var command = new ShowMessageModel { Header = "1 ПР", Status = ShowMessageModel.MessageType.Command };
        var error = new ShowMessageModel { Header = "Ошибка", ExecutionError = true };
        var control = new ProtocolListBoxUI();
        control.LoadMessages(new[] { command, error });
        var header = control.DisplayItems[0];
        var group = header.Group!;
        foreach (var body in group.BodyItems) control.DisplayItems.Remove(body);
        group.VisibleBodyCount = 0;
        group.SetExpanded(false);
        ((ListBox)control.FindName("ProtocolListBox")).ItemsSource = control.DisplayItems;

        control.NavigateToErrorOverviewLine(2);

        Assert.True(group.IsExpanded);
        Assert.Contains(control.DisplayItems, item => ReferenceEquals(item.Message, error));
        Assert.Same(error, ((ProtocolDisplayItem)((ListBox)control.FindName("ProtocolListBox")).SelectedItem).Message);
      }
      finally { ProtocolConfig.SetCommandHeadersInProtocol(original); }
    });
  }

  [Fact]
  public void NavigationToAttachedLogSelectsItsOwnerAndExpandsLogs()
  {
    RunInSta(() =>
    {
      var message = new ShowMessageModel { Header = "Измерение", Debug = "[ОТЛАДКА ROOT] источник" };
      var log = new ShowMessageModel { Debug = "[ЛОГ ROOT] ответ прибора" };
      var control = new ProtocolListBoxUI();
      control.LoadMessages(new[] { message, log });
      control.NavigateToErrorOverviewLine(2);
      var item = Assert.Single(control.DisplayItems);
      Assert.Same(message, item.Message);
      Assert.True(item.AreServiceLogsExpanded);
    });
  }

  [Fact]
  public void DenseMarkersStayNearTheirPositionAndCycleWithinCluster()
  {
    RunInSta(() =>
    {
      var bar = new ErrorOverviewBar();
      bar.Measure(new System.Windows.Size(24, 100));
      bar.Arrange(new System.Windows.Rect(0, 0, 24, 100));
      int selected = 0;
      bar.SetLineDiagnostics(100, Enumerable.Range(1, 100)
        .Select(i => (i, ErrorOverviewSeverity.Error, $"Ошибка {i}")), line => selected = line);
      bar.NavigateAt(50, false);
      int first = selected;
      Assert.InRange(first, 45, 55);
      bar.NavigateAt(50, false);
      Assert.Equal(first + 1, selected);
      bar.NavigateAt(50, true);
      Assert.Equal(first, selected);
    });
  }

  [Fact]
  public void EmptyTrackNavigatesToDocumentPosition()
  {
    RunInSta(() =>
    {
      var bar = new ErrorOverviewBar();
      bar.Measure(new System.Windows.Size(24, 100));
      bar.Arrange(new System.Windows.Rect(0, 0, 24, 100));
      int selected = 0;
      bar.SetLineDiagnostics(100, null, line => selected = line);
      bar.NavigateAt(0, false);
      Assert.Equal(1, selected);
      bar.NavigateAt(100, false);
      Assert.Equal(100, selected);
    });
  }

  private static void RunInSta(Action action)
  {
    Exception? failure = null;
    var thread = new Thread(() =>
    {
      try { action(); }
      catch (Exception ex) { failure = ex; }
    });
    thread.SetApartmentState(ApartmentState.STA);
    thread.Start();
    thread.Join();
    if (failure != null) throw new InvalidOperationException(failure.ToString(), failure);
  }
}
