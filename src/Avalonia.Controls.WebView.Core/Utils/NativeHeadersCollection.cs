using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Avalonia.Controls.Utils;

internal interface INativeHttpRequestHeaders
{
    bool Immutable { get; }
    bool TryClear();
    bool TryGetCount(out int count);
    string? GetHeader(string name);
    bool Contains(string name);
    bool TrySetHeader(string name, string value);
    bool TryRemoveHeader(string name);
    INativeHttpHeadersCollectionIterator GetIterator();
}
internal interface INativeHttpHeadersCollectionIterator
{
    void GetCurrentHeader(out string name, out string value);
    bool GetHasCurrentHeader();
    bool MoveNext();
}

internal class DictionaryNativeHttpRequestHeaders(IReadOnlyDictionary<string, string> headers)
    : INativeHttpRequestHeaders
{
    public bool Immutable => true;

    public bool TryClear() => false;

    public bool TryGetCount(out int count)
    {
        count = headers.Count;
        return true;
    }

    public string? GetHeader(string name) => headers.TryGetValue(name, out var value) ? value : null;

    public bool Contains(string name) => headers.ContainsKey(name);

    public bool TrySetHeader(string name, string value) => false;

    public bool TryRemoveHeader(string name) => false;

    public INativeHttpHeadersCollectionIterator GetIterator() => new Iterator(headers);

    private class Iterator(IReadOnlyDictionary<string, string> dictionary) : INativeHttpHeadersCollectionIterator
    {
        private readonly IEnumerator<KeyValuePair<string, string>> _enumerator = dictionary.GetEnumerator();
        private bool _initial = true;

        public void GetCurrentHeader(out string name, out string value)
        {
            var c = _enumerator.Current;
            name = c.Key;
            value = c.Value as string ?? ""; // should always be a string
        }

        public bool GetHasCurrentHeader()
        {
            if (_initial)
            {
                _initial = false;
                return MoveNext();
            }
            else
            {
                return !string.IsNullOrEmpty(_enumerator.Current.Key);
            }
        }

        public bool MoveNext() => _enumerator.MoveNext();
    }
}

internal sealed class NativeHeadersCollection(
    INativeHttpRequestHeaders nativeHeaders) :
    WebViewWebRequestHeaders, IDictionary<string, string>
{
    public override bool TrySet(string name, string value)
    {
        return nativeHeaders.TrySetHeader(name, value);
    }

    public override bool TryRemove(string name)
    {
        return nativeHeaders.TryRemoveHeader(name);
    }

    public override IEnumerable<string> Values => ((IDictionary<string, string>)this).Values;
    public override IEnumerable<string> Keys => ((IDictionary<string, string>)this).Keys;

    public override IEnumerator<KeyValuePair<string, string>> GetEnumerator()
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
        nativeHeaders.TrySetHeader(item.Key, item.Value);
    }

    public void Clear()
    {
        if (nativeHeaders.Immutable)
            return;
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
                nativeHeaders.TryRemoveHeader(key);
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
            return nativeHeaders.TryRemoveHeader(item.Key);
        }
        return false;
    }

    public override int Count
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

    public bool IsReadOnly => nativeHeaders.Immutable;

    public void Add(string key, string value)
    {
        nativeHeaders.TrySetHeader(key, value);
    }

    public override bool ContainsKey(string key)
    {
        return nativeHeaders.Contains(key);
    }

    public bool Remove(string key)
    {
        if (ContainsKey(key))
        {
            return nativeHeaders.TryRemoveHeader(key);
        }
        return false;
    }

#nullable disable // netstandard2.0 ...
    public override bool TryGetValue(string key, [MaybeNullWhen(false)] out string value)
#nullable restore
    {
        if (nativeHeaders.Contains(key))
        {
            value = nativeHeaders.GetHeader(key);
            return true;
        }

        value = null;
        return false;
    }

    string IDictionary<string, string>.this[string key]
    {
        get => nativeHeaders.GetHeader(key) ?? throw new KeyNotFoundException(key);
        set => nativeHeaders.TrySetHeader(key, value);
    }

    ICollection<string> IDictionary<string, string>.Keys
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

    ICollection<string> IDictionary<string, string>.Values
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
