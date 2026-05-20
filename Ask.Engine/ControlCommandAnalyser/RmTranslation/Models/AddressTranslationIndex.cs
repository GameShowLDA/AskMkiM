namespace Ask.Engine.ControlCommandAnalyser.RmTranslation.Models;

public sealed class AddressTranslationIndex
{
  private readonly Dictionary<string, AddressMapping> byObject;
  private readonly Dictionary<string, AddressMapping> bySynonym;
  private readonly Dictionary<MachineAddress, AddressMapping> byMachine;

  public AddressTranslationIndex(IEnumerable<AddressMapping> entries)
  {
    byObject = new Dictionary<string, AddressMapping>(StringComparer.OrdinalIgnoreCase);
    bySynonym = new Dictionary<string, AddressMapping>(StringComparer.OrdinalIgnoreCase);
    byMachine = new Dictionary<MachineAddress, AddressMapping>();

    foreach (var entry in entries)
    {
      byObject.TryAdd(entry.ObjectAddress.Value, entry);
      byMachine.TryAdd(entry.MachineAddress, entry);
      if (entry.Synonym is not null)
        bySynonym.TryAdd(entry.Synonym.Value, entry);
    }
  }

  public bool TryGetByObjectAddress(string objectAddress, out AddressMapping mapping)
    => byObject.TryGetValue(objectAddress, out mapping!);

  public bool TryGetBySynonym(string synonym, out AddressMapping mapping)
    => bySynonym.TryGetValue(synonym, out mapping!);

  public bool TryGetByMachineAddress(MachineAddress machineAddress, out AddressMapping mapping)
    => byMachine.TryGetValue(machineAddress, out mapping!);

  public IReadOnlyList<AddressMapping> Search(string text)
  {
    if (string.IsNullOrWhiteSpace(text))
      return Array.Empty<AddressMapping>();

    return byObject.Values
      .Where(entry =>
        entry.ObjectAddress.Value.Contains(text, StringComparison.OrdinalIgnoreCase)
        || entry.Synonym?.Value.Contains(text, StringComparison.OrdinalIgnoreCase) == true
        || entry.MachineAddress.ToString().Contains(text, StringComparison.OrdinalIgnoreCase))
      .Distinct()
      .ToArray();
  }
}
