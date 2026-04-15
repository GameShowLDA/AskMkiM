using System.Windows;
using System.Windows.Input;

namespace UI.Controls.Calendar
{
  public partial class CalendarNoteDialog : Window
  {
    private CalendarNoteDialog(string initialText)
    {
      InitializeComponent();
      noteTextBox.Text = initialText;
      noteTextBox.Focus();
      noteTextBox.CaretIndex = noteTextBox.Text.Length;
    }

    public string NoteText => noteTextBox.Text;

    public static bool TryShow(Window? owner, string initialText, out string noteText)
    {
      var dialog = new CalendarNoteDialog(initialText);
      if (owner != null)
      {
        dialog.Owner = owner;
      }

      var result = dialog.ShowDialog() == true;
      noteText = dialog.NoteText;
      return result;
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      if (e.LeftButton == MouseButtonState.Pressed)
      {
        DragMove();
      }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
      DialogResult = true;
      Close();
    }
  }
}
