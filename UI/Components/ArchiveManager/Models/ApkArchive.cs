namespace UI.Components.ArchiveManager.Models
{
  public class ApkArchive
  {
    /// <summary>
    /// Название архива.
    /// </summary>
    public string ArchiveName { get; set; }

    /// <summary>
    /// Примечание к архиву.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Принадлежит к главному архиву или нет.
    /// </summary>
    public bool IsMain { get; set; }
  }
}
