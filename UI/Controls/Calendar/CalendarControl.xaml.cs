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
  private CalendarNote? _openedNote;
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
      if (NoteEditor.Visibility != Visibility.Visible) CloseDetails();
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
    control.NotesPanel.Visibility = (bool)args.NewValue ? Visibility.Visible : Visibility.Collapsed;
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

  private void OpenNote_Click(object sender, RoutedEventArgs e)
  {
    if (NoteEditor.Visibility == Visibility.Visible) return;
    _openedNote = (CalendarNote)((Button)sender).Tag;
    DetailTitle.Text = _openedNote.Caption;
    DetailText.Text = _openedNote.Text;
    NoteDetails.Visibility = Visibility.Visible;
    DeleteConfirmation.Visibility = Visibility.Collapsed;
  }

  private void EditNote_Click(object sender, RoutedEventArgs e)
  {
    if (_openedNote is null) return;
    _editingId = _openedNote.Id;
    BeginEdit(_openedNote);
  }

  private void BeginEdit(CalendarNote? note)
  {
    NoteDetails.Visibility = DeleteConfirmation.Visibility = Visibility.Collapsed;
    NoteEditor.Visibility = Visibility.Visible;
    NotesListScroll.Visibility = Visibility.Collapsed;
    EmptyNotesText.Visibility = Visibility.Collapsed;
    AddNoteButton.IsEnabled = false;
    EditorHeading.Text = note is null ? "Новая заметка" : "Редактировать заметку";
    NoteDate.SelectedDate = note?.Date ?? SelectedDate;
    NoteTitle.Text = note?.Title ?? string.Empty;
    NoteText.Text = note?.Text ?? string.Empty;
    EditorError.Text = string.Empty;
    NoteText.Focus();
    NoteEditor.BringIntoView();
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
    CloseDetails();
    UpdateNotesList();
  }

  private void CloseNote_Click(object sender, RoutedEventArgs e) => CloseDetails();

  private void CloseDetails()
  {
    _openedNote = null;
    NoteDetails.Visibility = DeleteConfirmation.Visibility = Visibility.Collapsed;
  }

  private void DeleteNote_Click(object sender, RoutedEventArgs e)
  {
    if (_openedNote is null) return;
    DeleteConfirmation.Visibility = Visibility.Visible;
    DeleteConfirmation.BringIntoView();
  }

  private void CancelDelete_Click(object sender, RoutedEventArgs e) => DeleteConfirmation.Visibility = Visibility.Collapsed;

  private async void ConfirmDelete_Click(object sender, RoutedEventArgs e)
  {
    if (_openedNote is null) return;
    var id = _openedNote.Id;
    await RunStorageAsync(() =>
    {
      _notesService.Delete(id);
      return _notesService.Load();
    }, notes =>
    {
      _notes = notes;
      CloseDetails();
      RefreshNotes();
      NotesStatus.Text = "Заметка удалена.";
    });
  }
}
