using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Ask.UI.Controls.ErrorList
{
  public sealed class BreakpointListItem : INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Номер команды, к которой привязана точка остановки (ключ).
    /// </summary>
    public int CommandNumber { get; }

    private int? _leftLine;
    /// <summary>
    /// Номер строки в левом редакторе (1-based).
    /// </summary>
    /// <remarks><see langword="null"/> - если неизвестно/не применимо.</remarks>
    public int? LeftLine
    {
      get => _leftLine;
      set => SetField(ref _leftLine, value);
    }

    private int? _rightLine;
    /// <summary>
    /// Номер строки в правом редакторе (1-based).
    /// </summary>
    /// <remarks><see langword="null"/> - если неизвестно/не применимо.</remarks>
    public int? RightLine
    {
      get => _rightLine;
      set => SetField(ref _rightLine, value);
    }

    private bool _isEnabled = true;
    /// <summary>
    /// Состояние работы точки остановки.
    /// </summary>
    /// <remarks><see langword="true"/> - если включена.</remarks>
    public bool IsEnabled
    {
      get => _isEnabled;
      set => SetField(ref _isEnabled, value);
    }

    private string _mnemonic;
    /// <summary>
    /// Имя (мнемоника) команды.
    /// </summary>
    public string Mnemonic
    {
      get => _mnemonic;
      set => SetField(ref _mnemonic, value);
    }

    public BreakpointListItem(
      int commandNumber,
      int? leftLine,
      int? rightLine,
      string mnemonic,
      bool isEnabled = true)
    {
      CommandNumber = commandNumber;
      _leftLine = leftLine;
      _rightLine = rightLine;
      _mnemonic = mnemonic;
      _isEnabled = isEnabled;
    }

    /// <summary>
    /// Обновляет позицию (строки) точки остановки.
    /// </summary>
    public void UpdateLocation(int? leftLine, int? rightLine)
    {
      LeftLine = leftLine;
      RightLine = rightLine;
    }

    /// <summary>
    /// Обновляет состояние включена/выключена.
    /// </summary>
    public void UpdateEnabled(bool isEnabled)
    {
      IsEnabled = isEnabled;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
      => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
      if (EqualityComparer<T>.Default.Equals(field, value))
        return;

      field = value;
      OnPropertyChanged(propertyName);
    }

    public override string ToString()
      => $"Cmd={CommandNumber}, Left={LeftLine}, Right={RightLine}, Enabled={IsEnabled}";
  }
}