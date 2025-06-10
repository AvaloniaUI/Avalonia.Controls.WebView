using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Avalonia.Controls.Utils;

internal interface INativeHttpRequestHeaders
{
    bool TryClear();
    bool TryGetCount(out int count);
    string? GetHeader(string name);
    bool Contains(string name);
    void SetHeader(string name, string value);
    bool RemoveHeader(string name);
    INativeHttpHeadersCollectionIterator GetIterator();
}
internal interface INativeHttpHeadersCollectionIterator
{
    void GetCurrentHeader(out string name, out string value);
    bool GetHasCurrentHeader();
    bool MoveNext();
}

internal sealed class NativeHeadersCollection(INativeHttpRequestHeaders nativeHeaders) : IDictionary<string, string>
{
    public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
    {
        var iterator = nativeHeaders.GetIterator();
        while (iterator.GetHasCurrentHeader())
        {
            iterator.GetCurrentHeader(out var name, out var value);
            yield return new KeyValuePair<string, string>(name, value);
            if (!iterator.MoveNext())
                break;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Add(KeyValuePair<string, string> item)
    {
        nativeHeaders.SetHeader(item.Key, item.Value);
    }

    public void Clear()
    {
        if (!nativeHeaders.TryClear())
        {
            var keys = new List<string>();
            var iterator = nativeHeaders.GetIterator();
            while (iterator.GetHasCurrentHeader())
            {
                iterator.GetCurrentHeader(out var name, out _);
                keys.Add(name);
                if (!iterator.MoveNext())
                    break;
            }

            foreach (var key in keys)
                nativeHeaders.RemoveHeader(key);
        }
    }

    public bool Contains(KeyValuePair<string, string> item)
    {
        var value = nativeHeaders.GetHeader(item.Key);
        return value == item.Value;
    }

    public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
    {
        foreach (var kv in this)
        {
            array[arrayIndex++] = kv;
        }
    }

    public bool Remove(KeyValuePair<string, string> item)
    {
        if (Contains(item))
        {
            nativeHeaders.RemoveHeader(item.Key);
            return true;
        }
        return false;
    }

    public int Count
    {
        get
        {
            if (!nativeHeaders.TryGetCount(out var count))
            {
                var iterator = nativeHeaders.GetIterator();
                while (iterator.GetHasCurrentHeader())
                {
                    count++;
                    if (!iterator.MoveNext())
                        break;
                }
            }

            return count;
        }
    }

    public bool IsReadOnly => false;

    public void Add(string key, string value)
    {
        nativeHeaders.SetHeader(key, value);
    }

    public bool ContainsKey(string key)
    {
        return nativeHeaders.Contains(key);
    }

    public bool Remove(string key)
    {
        if (ContainsKey(key))
        {
            nativeHeaders.RemoveHeader(key);
            return true;
        }
        return false;
    }

#nullable disable // netstandard2.0 ...
    public bool TryGetValue(string key, [MaybeNullWhen(false)] out string value)
#nullable restore
    {
        value = nativeHeaders.GetHeader(key);
        return value != null;
    }

    public string this[string key]
    {
        get => nativeHeaders.GetHeader(key) ?? throw new KeyNotFoundException(key);
        set => nativeHeaders.SetHeader(key, value);
    }

    public ICollection<string> Keys
    {
        get
        {
            var keys = new List<string>();
            var iterator = nativeHeaders.GetIterator();
            while (iterator.GetHasCurrentHeader())
            {
                iterator.GetCurrentHeader(out var name, out _);
                keys.Add(name);
                if (!iterator.MoveNext())
                    break;
            }
            return keys;
        }
    }

    public ICollection<string> Values
    {
        get
        {
            var values = new List<string>();
            var iterator = nativeHeaders.GetIterator();
            while (iterator.GetHasCurrentHeader())
            {
                iterator.GetCurrentHeader(out _, out var value);
                values.Add(value);
                if (!iterator.MoveNext())
                    break;
            }
            return values;
        }
    }
}
