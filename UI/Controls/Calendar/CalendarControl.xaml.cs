using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace UI.Controls.Calendar
{
  /// <summary>
  /// Логика взаимодействия для CalendarControl.xaml
  /// </summary>
  public partial class CalendarControl : UserControl
  {
    private bool _isSelecting;

    public CalendarControl()
    {
      InitializeComponent();
      DataContext = new CalendarViewModel();
      PreviewMouseMove += CalendarControl_PreviewMouseMove;
      PreviewMouseLeftButtonUp += CalendarControl_PreviewMouseLeftButtonUp;
      LostMouseCapture += (_, _) => _isSelecting = false;
    }

    private CalendarViewModel ViewModel => (CalendarViewModel)DataContext;

    private void DayButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      if (TryGetCalendarDay(sender, out var day) == false)
      {
        return;
      }

      _isSelecting = true;
      CaptureMouse();
      ViewModel.BeginSelection(day);
      e.Handled = true;
    }

    private void DayButton_PreviewMouseEnter(object sender, MouseEventArgs e)
    {
      if (_isSelecting == false || e.LeftButton != MouseButtonState.Pressed)
      {
        return;
      }

      if (TryGetCalendarDay(sender, out var day) == false)
      {
        return;
      }

      ViewModel.UpdateSelection(day);
    }

    private void CalendarControl_PreviewMouseMove(object sender, MouseEventArgs e)
    {
      if (_isSelecting == false || e.LeftButton != MouseButtonState.Pressed)
      {
        return;
      }

      if (TryGetCalendarDayAtMousePosition(e, out var day) == false)
      {
        return;
      }

      ViewModel.UpdateSelection(day);
    }

    private void DayButton_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
      if (_isSelecting == false)
      {
        return;
      }

      if (TryGetCalendarDay(sender, out var day))
      {
        ViewModel.CompleteSelection(day);
      }

      ReleaseSelectionCapture();
      e.Handled = true;
    }

    private void CalendarControl_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
      if (_isSelecting == false)
      {
        return;
      }

      ReleaseSelectionCapture();
    }

    private void DayButton_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
      if (TryGetCalendarDay(sender, out var day) == false || sender is not Button button)
      {
        return;
      }

      ReleaseSelectionCapture();
      ViewModel.PrepareContextSelection(day);

      var contextMenu = BuildDayContextMenu();
      button.ContextMenu = contextMenu;
      contextMenu.PlacementTarget = button;
      contextMenu.IsOpen = true;
      e.Handled = true;
    }

    private ContextMenu BuildDayContextMenu()
    {
      var existingNote = ViewModel.GetSelectionNoteSummary();
      var hasNotes = ViewModel.SelectionHasNotes();

      var contextMenu = new ContextMenu
      {
        MinWidth = 210,
      };
      contextMenu.SetResourceReference(StyleProperty, "AppContextMenuStyle");

      var editNoteItem = new MenuItem
      {
        Header = string.IsNullOrWhiteSpace(existingNote) ? "Создать заметку" : "Изменить заметку",
      };
      editNoteItem.Click += (_, _) => OpenNoteDialog(existingNote);
      contextMenu.Items.Add(editNoteItem);

      if (hasNotes)
      {
        var deleteNoteItem = new MenuItem
        {
          Header = "Удалить заметку",
        };
        deleteNoteItem.Click += (_, _) => ViewModel.DeleteNoteForSelection();
        contextMenu.Items.Add(deleteNoteItem);
      }

      return contextMenu;
    }

    private void OpenNoteDialog(string initialText)
    {
      if (CalendarNoteDialog.TryShow(Window.GetWindow(this), initialText, out var noteText) == false)
      {
        return;
      }

      if (string.IsNullOrWhiteSpace(noteText))
      {
        ViewModel.DeleteNoteForSelection();
        return;
      }

      ViewModel.SaveNoteForSelection(noteText);
    }

    private void ReleaseSelectionCapture()
    {
      _isSelecting = false;
      if (IsMouseCaptured)
      {
        ReleaseMouseCapture();
      }
    }

    private static bool TryGetCalendarDay(object sender, out CalendarDay day)
    {
      if (sender is FrameworkElement element && element.DataContext is CalendarDay calendarDay)
      {
        day = calendarDay;
        return true;
      }

      day = null!;
      return false;
    }

    private bool TryGetCalendarDayAtMousePosition(MouseEventArgs e, out CalendarDay day)
    {
      var hit = InputHitTest(e.GetPosition(this)) as DependencyObject;

      while (hit != null)
      {
        if (hit is FrameworkElement element && element.DataContext is CalendarDay calendarDay)
        {
          day = calendarDay;
          return true;
        }

        hit = VisualTreeHelper.GetParent(hit);
      }

      day = null!;
      return false;
    }
  }
}
