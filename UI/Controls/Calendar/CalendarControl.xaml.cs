using Ask.Core.Services.Calendar;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace UI.Controls.Calendar;

/// <summary>Выбирает дату и управляет общими заметками календаря.</summary>
public partial class CalendarControl : UserControl
{
  private readonly CalendarViewModel _viewModel;
  private readonly CalendarNoteService _notesService = new();
  private IReadOnlyList<CalendarNote> _notes = Array.Empty<CalendarNote>();
  private Guid? _pendingDeleteId;
  private StackPanel? _deleteActions;
  private StackPanel? _deleteConfirmation;
  private Guid? _editingId;
  private bool _busy;
  private bool _loadedNotes;

  public static readonly DependencyProperty NotesEnabledProperty = DependencyProperty.Register(
    nameof(NotesEnabled), typeof(bool), typeof(CalendarControl),
    new PropertyMetadata(false, OnNotesEnabledChanged));

  /// <summary>Признак доступности общих заметок в этом экземпляре календаря.</summary>
  public bool NotesEnabled
  {
    get => (bool)GetValue(NotesEnabledProperty);
    set => SetValue(NotesEnabledProperty, value);
  }

  public event EventHandler? SelectedDateChanged;
  public DateTime SelectedDate => _viewModel.SelectedDate;

  public CalendarControl()
  {
    InitializeComponent();
    _viewModel = new CalendarViewModel();
    _viewModel.SelectedDateChanged += (_, _) =>
    {
      UpdateNotesList();

      SelectedDateChanged?.Invoke(this, EventArgs.Empty);
    };
    DataContext = _viewModel;
    AddNoteButton.IsEnabled = false;
    EmptyNotesText.Visibility = Visibility.Collapsed;
    IsVisibleChanged += async (_, _) =>
    {
      if (IsVisible && NotesEnabled) await ReloadAsync();
    };
  }

  public void SetAvailabilityProvider(Func<DateTime, CalendarDayAvailability> provider)
  {
    _viewModel.AvailabilityProvider = provider;
    _viewModel.Refresh();
  }

  private static void OnNotesEnabledChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
  {
    var control = (CalendarControl)sender;
    bool enabled = (bool)args.NewValue;
    control.NotesPanel.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
    control.Width = enabled ? 680 : 352;
    control.CalendarLayout.Height = enabled ? 500 : double.NaN;
    if (!(bool)args.NewValue)
    {
      control._viewModel.NotesProvider = null;
      control._viewModel.Refresh();
    }
  }

  private async Task ReloadAsync()
  {
    if (_busy) return;
    await RunStorageAsync(() => _notesService.Load(), notes =>
    {
      _notes = notes;
      _loadedNotes = true;
      ReloadNotesButton.Visibility = Visibility.Collapsed;
      NotesStatus.Text = string.Empty;
      RefreshNotes();
    });
  }

  private async Task RunStorageAsync<T>(Func<T> operation, Action<T> onSuccess)
  {
    if (_busy) return;
    _busy = true;
    IsEnabled = false;
    try
    {
      var result = await Task.Run(operation);
      onSuccess(result);
    }
    catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or JsonException)
    {
      NotesStatus.Text = "Не удалось прочитать или сохранить заметки. " + error.Message;
      ReloadNotesButton.Visibility = Visibility.Visible;
    }
    finally
    {
      _busy = false;
      IsEnabled = true;
      AddNoteButton.IsEnabled = _loadedNotes && NoteEditor.Visibility != Visibility.Visible;
    }
  }

  private void RefreshNotes()
  {
    var dates = _notes.Select(note => note.Date.Date).ToHashSet();
    _viewModel.NotesProvider = dates.Contains;
    _viewModel.Refresh();
    UpdateNotesList();
  }

  private void UpdateNotesList()
  {
    ResetDeleteConfirmation();
    var notes = _notes.Where(note => note.Date.Date == SelectedDate.Date).ToArray();
    NotesList.ItemsSource = notes;
    NotesHeading.Text = $"Заметки дня · {notes.Length}";
    EmptyNotesText.Visibility = _loadedNotes && notes.Length == 0 && NoteEditor.Visibility != Visibility.Visible
      ? Visibility.Visible : Visibility.Collapsed;
  }

  private async void ReloadNotes_Click(object sender, RoutedEventArgs e) => await ReloadAsync();

  private void AddNote_Click(object sender, RoutedEventArgs e)
  {
    _editingId = null;
    BeginEdit(null);
  }

  private void EditListNote_Click(object sender, RoutedEventArgs e)
  {
    var note = (CalendarNote)((Button)sender).Tag;
    _editingId = note.Id;
    BeginEdit(note);
  }

  private void DeleteListNote_Click(object sender, RoutedEventArgs e)
  {
    ResetDeleteConfirmation();
    var button = (Button)sender;
    _pendingDeleteId = ((CalendarNote)button.Tag).Id;
    _deleteActions = (StackPanel)button.Parent;
    var row = (Grid)_deleteActions.Parent;
    _deleteConfirmation = row.Children.OfType<StackPanel>().Single(panel => panel.Name == "RowConfirmation");
    _deleteActions.Visibility = Visibility.Collapsed;
    _deleteConfirmation.Visibility = Visibility.Visible;
    ((Button)_deleteConfirmation.Children[1]).Focus();
  }

  private void BeginEdit(CalendarNote? note)
  {
    ResetDeleteConfirmation();
    NoteEditor.Visibility = Visibility.Visible;
    NotesListScroll.Visibility = Visibility.Collapsed;
    EmptyNotesText.Visibility = Visibility.Collapsed;
    AddNoteButton.IsEnabled = false;
    EditorHeading.Text = note is null ? "Новая заметка" : "Редактировать заметку";
    NoteDate.SelectedDate = note?.Date ?? SelectedDate;
    NoteTitle.Text = note?.Title ?? string.Empty;
    NoteText.Text = note?.Text ?? string.Empty;
    EditorError.Text = string.Empty;
    NotesStatus.Text = string.Empty;
    NoteText.Focus();

  }

  private async void SaveNote_Click(object sender, RoutedEventArgs e)
  {
    if (NoteDate.SelectedDate is not { } date || string.IsNullOrWhiteSpace(NoteText.Text))
    {
      EditorError.Text = "Укажите дату и непустой текст заметки.";
      return;
    }
    var note = new CalendarNote
    {
      Id = _editingId ?? Guid.NewGuid(), Date = date.Date,
      Title = NoteTitle.Text, Text = NoteText.Text
    };
    _editingId = note.Id;
    await RunStorageAsync(() =>
    {
      _notesService.Save(note);
      return _notesService.Load();
    }, notes =>
    {
      _notes = notes;
      EndEdit();
      RefreshNotes();
      NotesStatus.Text = $"Заметка сохранена на {date:dd.MM.yyyy}.";
    });
  }

  private void CancelEdit_Click(object sender, RoutedEventArgs e) => EndEdit();

  private void EndEdit()
  {
    NoteEditor.Visibility = Visibility.Collapsed;
    NotesListScroll.Visibility = Visibility.Visible;
    _editingId = null;
    AddNoteButton.IsEnabled = _loadedNotes;
    ResetDeleteConfirmation();
    UpdateNotesList();
  }

  private void ResetDeleteConfirmation()
  {
    if (_deleteActions is not null) _deleteActions.Visibility = Visibility.Visible;
    if (_deleteConfirmation is not null) _deleteConfirmation.Visibility = Visibility.Collapsed;
    _deleteActions = _deleteConfirmation = null;
    _pendingDeleteId = null;
  }

  private void CancelDelete_Click(object sender, RoutedEventArgs e) => ResetDeleteConfirmation();

  private async void ConfirmDelete_Click(object sender, RoutedEventArgs e)
  {
    if (_pendingDeleteId is not { } id) return;
    await RunStorageAsync(() =>
    {
      _notesService.Delete(id);
      return _notesService.Load();
    }, notes =>
    {
      _notes = notes;
      ResetDeleteConfirmation();
      RefreshNotes();
      NotesStatus.Text = "Заметка удалена.";
    });
  }
}
