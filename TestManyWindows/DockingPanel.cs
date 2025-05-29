using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TestManyWindows
{
  public class DockingPanel : Grid
  {
    private StackPanel _tabPanel;
    private ContentPresenter _contentPresenter;
    private List<DockingTab> _tabs = new();
    public event Action TabRemoved;
    public int TabCount => _tabs.Count;

    public DockingPanel()
    {
      RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
      RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

      _tabPanel = new StackPanel { Orientation = Orientation.Horizontal, Background = Brushes.DimGray };
      _contentPresenter = new ContentPresenter();

      Children.Add(_tabPanel);
      Children.Add(_contentPresenter);
      Grid.SetRow(_tabPanel, 0);
      Grid.SetRow(_contentPresenter, 1);
      AllowDrop = true;
      Drop += DockingPanel_Drop;
    }

    private void DockingPanel_Drop(object sender, DragEventArgs e)
    {
      if (e.Data.GetDataPresent(typeof(DockingTab)))
      {
        var tab = (DockingTab)e.Data.GetData(typeof(DockingTab));
        var parent = VisualTreeHelper.GetParent(tab) as Panel;
        parent?.Children.Remove(tab);
        _tabs.Remove(tab);

        AddTab(tab);
      }
    }

    public void AddTab(DockingTab tab)
    {
      _tabs.Add(tab);
      _tabPanel.Children.Add(tab);
      SelectTab(tab);
    }

    public void AddTab(string title, UIElement content)
    {
      DockingTab tab = null;

      tab = new DockingTab(
          title,
          () => RemoveTab(tab),
          () => SelectTab(tab)
      )
      {
        Content = content
      };

      _tabs.Add(tab);
      _tabPanel.Children.Add(tab);

      SelectTab(tab);
    }


    private void SelectTab(DockingTab tab)
    {
      foreach (var t in _tabs)
        t.IsSelected = false;

      tab.IsSelected = true;
      _contentPresenter.Content = tab.Content;
    }

    //private void RemoveTab(DockingTab tab)
    //{
    //  int index = _tabs.IndexOf(tab);
    //  _tabs.Remove(tab);
    //  _tabPanel.Children.Remove(tab);

    //  if (tab.IsSelected)
    //  {
    //    if (_tabs.Count > 0)
    //      SelectTab(_tabs[Math.Max(0, index - 1)]);
    //    else
    //      _contentPresenter.Content = null;
    //  }
    //}

    public void RemoveTab(DockingTab tab)
    {
      _tabPanel.Children.Remove(tab);
      _contentPresenter.Content = null;
      _tabs.Remove(tab);
      TabRemoved?.Invoke();
    }

    public bool HasTabs() => _tabs.Count > 0;
  }
}
