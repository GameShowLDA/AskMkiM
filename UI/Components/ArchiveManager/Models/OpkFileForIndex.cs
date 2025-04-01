using Newtonsoft.Json;
using System;

namespace UI.Components.ArchiveManager.Models
{
  public class OpkFileForIndex : OpkFile
  {
    public string ControlSum { get; set; }
    public OpkFileForIndex(string marking, string name, string order, string opkFilename, DateTime creation, string department, string description)
      : base(marking, name, order, opkFilename, creation, department, description)
    {
    }

    [JsonConstructor]
    public OpkFileForIndex(string marking, string name, string order, string opkFilename, DateTime creation, string department, string description, string controlSum)
      : base(marking, name, order, opkFilename, creation, department, description)
    {
      this.ControlSum = controlSum;
    }

    public OpkFileForIndex() { }
  }
}
