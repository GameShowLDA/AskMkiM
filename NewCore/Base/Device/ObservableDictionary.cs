using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewCore.Base.Device
{
  public class ObservableDictionary<TKey, TValue> : Dictionary<TKey, TValue>
  {
    public event EventHandler? Changed;

    public new TValue this[TKey key]
    {
      get => base[key];
      set
      {
        base[key] = value;
        Changed?.Invoke(this, EventArgs.Empty);
      }
    }

    public new void Add(TKey key, TValue value)
    {
      base.Add(key, value);
      Changed?.Invoke(this, EventArgs.Empty);
    }

    public new bool Remove(TKey key)
    {
      var result = base.Remove(key);
      if (result)
        Changed?.Invoke(this, EventArgs.Empty);
      return result;
    }
  }
}
