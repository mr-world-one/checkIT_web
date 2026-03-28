using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace CheckIT.Tests.TestDoubles;

internal sealed class FakeTempDataDictionary : Dictionary<string, object?>, ITempDataDictionary
{
    public new object? this[string key]
    {
        get => TryGetValue(key, out var v) ? v : null;
        set => base[key] = value;
    }

    public void Keep() { }
    public void Keep(string key) { }
    public void Load() { }
    public object? Peek(string key) => this[key];
    public void Save() { }
    public void Remove(string key) => base.Remove(key);

    public ICollection<string> KeysCollection => Keys;
    public new ICollection<object?> ValuesCollection => Values;

    public void Add(KeyValuePair<string, object?> item) => ((IDictionary<string, object?>)this).Add(item);
    public bool Contains(KeyValuePair<string, object?> item) => ((IDictionary<string, object?>)this).Contains(item);
    public void CopyTo(KeyValuePair<string, object?>[] array, int arrayIndex) => ((IDictionary<string, object?>)this).CopyTo(array, arrayIndex);
    public bool Remove(KeyValuePair<string, object?> item) => ((IDictionary<string, object?>)this).Remove(item);
}
