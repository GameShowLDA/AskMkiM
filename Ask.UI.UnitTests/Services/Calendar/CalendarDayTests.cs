using UI.Controls.Calendar;

namespace Ask.UI.UnitTests.Services.Calendar;

public sealed class CalendarDayTests
{
  [Theory]
  [InlineData(0, "", false)]
  [InlineData(1, "1", true)]
  [InlineData(9, "9", true)]
  [InlineData(10, "9+", true)]
  [InlineData(100, "9+", true)]
  public void NoteCountBadge_ShowsBoundedCount(int count, string caption, bool visible)
  {
    var day = new CalendarDay { NoteCount = count };
    Assert.Equal(caption, day.NoteCountCaption);
    Assert.Equal(visible, day.HasNotes);
  }

  [Fact]
  public void Refresh_UpdatesCountsWithoutChangingSelectedDate()
  {
    var viewModel = new CalendarViewModel();
    var selected = viewModel.SelectedDate;
    int count = 3;
    viewModel.NotesProvider = date => date == selected ? count : 0;
    viewModel.Refresh();
    Assert.Equal(3, viewModel.Days.Single(day => day.Date == selected).NoteCount);
    count = 12;
    viewModel.Refresh();
    Assert.Equal("9+", viewModel.Days.Single(day => day.Date == selected).NoteCountCaption);
    Assert.Equal(selected, viewModel.SelectedDate);
  }
}
