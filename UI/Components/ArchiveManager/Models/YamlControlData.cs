using YamlDotNet.Serialization;

namespace UI.Components.ArchiveManager.Models
{
  public class YamlControlData
  {
    [YamlMember(Alias = "TotalControlSum")]
    public string TotalControlSum { get; set; }
    public YamlControlData() { }

    public YamlControlData(string totalControlSum)
    {
      this.TotalControlSum = totalControlSum;
    }
  }
}
