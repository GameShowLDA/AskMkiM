namespace UI.Components.ArchiveManager.Models
{
  public class OpkFile
  {
    /// <summary>
    /// Обозначение ОК.
    /// </summary>
    public string Marking { get; set; }
    /// <summary>
    /// Наименование ОК
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// Заказ.
    /// </summary>
    public string Order { get; set; }
    /// <summary>
    /// Файл opk.
    /// </summary>
    public string OpkFilename { get; set; }
    /// <summary>
    /// Дата создания.
    /// </summary>
    public DateTime Creation { get; set; }
    /// <summary>
    /// Цех.
    /// </summary>
    public string Department { get; set; }
    /// <summary>
    /// Примечания.
    /// </summary>
    public string Description { get; set; }

    public OpkFile(string marking, string name, string order, string opkFilename, DateTime creation, string department, string description)
    {
      this.Marking = marking;
      this.Name = name;
      this.Order = order;
      this.OpkFilename = opkFilename;
      this.Creation = creation;
      this.Department = department;
      this.Description = description;
    }

    public OpkFile() { }
  }
}
