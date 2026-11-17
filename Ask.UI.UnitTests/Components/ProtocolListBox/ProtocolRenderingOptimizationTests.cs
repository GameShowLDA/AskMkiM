using Ask.Core.Shared.DTO.Protocol;
using Ask.UI.Components.ProtocolListBox;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Windows.Data;

namespace Ask.UI.UnitTests.Components.ProtocolListBox;

public sealed class ProtocolRenderingOptimizationTests
{
  [Fact]
  public void CollapsedLogs_NotifyTextOnceWhenExpandedAndPreserveEveryEntry()
  {
    var item = ProtocolDisplayItem.CreateLine(new ShowMessageModel("Измерение"), false);
    var notifications = new List<string>();
    item.PropertyChanged += (_, e) => notifications.Add(e.PropertyName!);
    var logs = Enumerable.Range(0, 2000).Select(i => $"[INFO] ответ прибора {i}").ToArray();

    foreach (var log in logs)
      item.AddServiceLog(log);

    Assert.Equal(string.Join(Environment.NewLine, logs), item.ServiceLogText);
    Assert.DoesNotContain(nameof(ProtocolDisplayItem.ServiceLogText), notifications);
    Assert.Single(notifications, name => name == nameof(ProtocolDisplayItem.HasServiceLogs));

    item.AreServiceLogsExpanded = true;

    Assert.Single(notifications, name => name == nameof(ProtocolDisplayItem.ServiceLogText));
    Assert.Equal(string.Join(Environment.NewLine, logs), item.ServiceLogText);
    Assert.Equal(nameof(ProtocolDisplayItem.ServiceLogText), notifications[^2]);
    Assert.Equal(nameof(ProtocolDisplayItem.AreServiceLogsExpanded), notifications[^1]);
  }

  [Fact]
  public void ExpandedLogs_NotifyEveryAppendAndRefreshAfterCollapsedUpdates()
  {
    var item = ProtocolDisplayItem.CreateLine(new ShowMessageModel("Измерение"), false);
    item.AreServiceLogsExpanded = true;
    var texts = new List<string>();
    item.PropertyChanged += (_, e) =>
    {
      if (e.PropertyName == nameof(ProtocolDisplayItem.ServiceLogText))
        texts.Add(item.ServiceLogText);
    };

    item.AddServiceLog("Первый ответ");
    item.AddServiceLog("Второй ответ");
    item.AreServiceLogsExpanded = false;
    item.AddServiceLog("Третий ответ");
    Assert.Equal(2, texts.Count);
    item.AreServiceLogsExpanded = true;

    Assert.Equal(new[]
    {
      "Первый ответ",
      string.Join(Environment.NewLine, "Первый ответ", "Второй ответ"),
      string.Join(Environment.NewLine, "Первый ответ", "Второй ответ", "Третий ответ"),
    }, texts);
  }

  [Fact]
  public void BoundLogText_IsCurrentBeforeExpansionVisibilityChanges() => RunInSta(() =>
  {
    var item = ProtocolDisplayItem.CreateLine(new ShowMessageModel("Измерение"), false);
    var box = new ProtocolServiceLogsBox();
    BindingOperations.SetBinding(box, ProtocolServiceLogsBox.LogTextProperty,
      new Binding(nameof(ProtocolDisplayItem.ServiceLogText)) { Source = item });
    item.PropertyChanged += (_, e) =>
    {
      if (e.PropertyName == nameof(ProtocolDisplayItem.AreServiceLogsExpanded) && item.AreServiceLogsExpanded)
        Assert.Equal(item.ServiceLogText, box.LogText);
    };

    item.AddServiceLog("Первый ответ");
    item.AreServiceLogsExpanded = true;
    item.AddServiceLog("Второй ответ");
    Assert.Equal(item.ServiceLogText, box.LogText);
    item.AreServiceLogsExpanded = false;
    item.AddServiceLog("Третий ответ");
    item.AreServiceLogsExpanded = true;
    Assert.Equal(item.ServiceLogText, box.LogText);
  });

  [Fact]
  public void OverviewIndex_AppendsIncrementallyAndRebuildsAfterStructuralChanges() => RunInSta(() =>
  {
    var control = new ProtocolListBoxUI();
    control.LoadMessages(new[] { new ShowMessageModel("Первая строка"), new ShowMessageModel("Вторая строка") });
    var type = typeof(ProtocolListBoxUI);
    var indices = (Dictionary<ProtocolDisplayItem, int>)type.GetField("_visibleIndices", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(control)!;
    var dirty = type.GetField("_visibleIndicesDirty", BindingFlags.Instance | BindingFlags.NonPublic)!;
    var flush = type.GetMethod("FlushOverviewUpdate", BindingFlags.Instance | BindingFlags.NonPublic)!;
    Assert.False((bool)dirty.GetValue(control)!);

    control.DisplayItems.Add(ProtocolDisplayItem.CreateLine(new ShowMessageModel("Добавленная строка"), false));
    Assert.False((bool)dirty.GetValue(control)!);
    AssertIndexMatchesDisplayItems();

    control.DisplayItems.Insert(0, ProtocolDisplayItem.CreateLine(new ShowMessageModel("Вставленная строка"), false));
    RebuildAndCheck();
    control.DisplayItems.RemoveAt(1);
    RebuildAndCheck();
    control.DisplayItems.Move(0, control.DisplayItems.Count - 1);
    RebuildAndCheck();
    control.DisplayItems[0] = ProtocolDisplayItem.CreateLine(new ShowMessageModel("Заменённая строка"), false);
    RebuildAndCheck();
    control.DisplayItems.Clear();
    RebuildAndCheck();

    void RebuildAndCheck()
    {
      Assert.True((bool)dirty.GetValue(control)!);
      flush.Invoke(control, null);
      Assert.False((bool)dirty.GetValue(control)!);
      AssertIndexMatchesDisplayItems();
    }

    void AssertIndexMatchesDisplayItems()
    {
      Assert.Equal(control.DisplayItems.Count, indices.Count);
      for (var i = 0; i < control.DisplayItems.Count; i++)
        Assert.Equal(i, indices[control.DisplayItems[i]]);
    }
  });

  [Fact]
  public void AppendLine_OnUiThreadCompletesSynchronouslyAndPreservesMessage() => RunInSta(() =>
  {
    var control = new ProtocolListBoxUI();
    var message = new ShowMessageModel("Строка протокола");

    var appendTask = control.AppendLineAsync(message);

    Assert.True(appendTask.IsCompletedSuccessfully);
    Assert.Contains("Строка протокола", control.GetText());
    Assert.Same(message, Assert.Single(control.DisplayItems).Message);
  });

  private static void RunInSta(Action action)
  {
    Exception? failure = null;
    var thread = new Thread(() =>
    {
      try { action(); }
      catch (Exception exception) { failure = exception; }
    }) { IsBackground = true };
    thread.SetApartmentState(ApartmentState.STA);
    thread.Start();
    Assert.True(thread.Join(TimeSpan.FromSeconds(20)), "Protocol rendering test timed out");
    if (failure != null)
      ExceptionDispatchInfo.Capture(failure).Throw();
  }
}
