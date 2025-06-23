namespace UI.Controls.TextEditor
{
  public sealed class EditorType
  {
    public static readonly EditorType TextEditor = new("Текстовый редактор");
    public static readonly EditorType Translator = new("Трансляторы");

    public string DisplayName { get; }

    private EditorType(string displayName)
    {
      DisplayName = displayName;
    }

    public override string ToString() => DisplayName;
  }
}
