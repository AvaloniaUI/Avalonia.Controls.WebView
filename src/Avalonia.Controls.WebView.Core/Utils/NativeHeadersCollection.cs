using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

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

internal sealed class NativeHeadersCollection(
    INativeHttpRequestHeaders nativeHeaders) :
    WebViewWebRequestHeaders, IDictionary<string, string>
{
    private INativeHttpRequestHeaders? _nativeHeaders = nativeHeaders;
    public bool HasChanges { get; private set; }

    public void Dispose()
    {
        _nativeHeaders = null;
    }

    public override bool TrySet(string name, string value)
    {
        var wasSet = _nativeHeaders?.TrySetHeader(name, value) ?? false;
        HasChanges |= wasSet;
        return wasSet;
    }

    public override bool TryRemove(string name)
    {
        var wasRemoved = _nativeHeaders?.TryRemoveHeader(name) ?? false;
        HasChanges |= wasRemoved;
        return wasRemoved;
    }

    public override IEnumerable<string> Values => ((IDictionary<string, string>)this).Values;
    public override IEnumerable<string> Keys => ((IDictionary<string, string>)this).Keys;

    public override IEnumerator<KeyValuePair<string, string>> GetEnumerator()
    {
        if (_nativeHeaders is null)
            throw new ObjectDisposedException(nameof(WebViewWebRequestHeaders));

        var iterator = _nativeHeaders.GetIterator();
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
        if (_nativeHeaders is null)
            throw new ObjectDisposedException(nameof(WebViewWebRequestHeaders));

        var wasSet = _nativeHeaders.TrySetHeader(item.Key, item.Value);
        HasChanges |= wasSet;
    }

    public void Clear()
    {
        if (_nativeHeaders is null)
            throw new ObjectDisposedException(nameof(WebViewWebRequestHeaders));

        if (_nativeHeaders.Immutable)
            return;

        if (!_nativeHeaders.TryClear())
        {
            var keys = new List<string>();
            var iterator = _nativeHeaders.GetIterator();
            while (iterator.GetHasCurrentHeader())
            {
                iterator.GetCurrentHeader(out var name, out _);
                keys.Add(name);
                if (!iterator.MoveNext())
                    break;
            }

            foreach (var key in keys)
                _nativeHeaders.TryRemoveHeader(key);
        }

        HasChanges = true;
    }

    public bool Contains(KeyValuePair<string, string> item)
    {
        if (_nativeHeaders is null)
            throw new ObjectDisposedException(nameof(WebViewWebRequestHeaders));

        var value = _nativeHeaders.GetHeader(item.Key);
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
        if (_nativeHeaders is null)
            throw new ObjectDisposedException(nameof(WebViewWebRequestHeaders));

        if (Contains(item))
        {
            var wasRemoved = _nativeHeaders.TryRemoveHeader(item.Key);
            HasChanges |= wasRemoved;
            return wasRemoved;
        }
        return false;
    }

    public override int Count
    {
        get
        {
            if (_nativeHeaders is null)
                throw new ObjectDisposedException(nameof(WebViewWebRequestHeaders));

            if (!_nativeHeaders.TryGetCount(out var count))
            {
                var iterator = _nativeHeaders.GetIterator();
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

    public bool IsReadOnly => _nativeHeaders?.Immutable ?? true;

    public void Add(string key, string value)
    {
        if (_nativeHeaders is null)
            throw new ObjectDisposedException(nameof(WebViewWebRequestHeaders));

        var wasSet = _nativeHeaders.TrySetHeader(key, value);
        HasChanges |= wasSet;
    }

    public override bool ContainsKey(string key)
    {
        if (_nativeHeaders is null)
            throw new ObjectDisposedException(nameof(WebViewWebRequestHeaders));

        return _nativeHeaders.Contains(key);
    }

    public bool Remove(string key)
    {
        if (_nativeHeaders is null)
            throw new ObjectDisposedException(nameof(WebViewWebRequestHeaders));

        if (ContainsKey(key))
        {
            var wasRemoved = _nativeHeaders.TryRemoveHeader(key);
            HasChanges |= wasRemoved;
            return wasRemoved;
        }
        return false;
    }

#nullable disable // netstandard2.0 ...
    public override bool TryGetValue(string key, [MaybeNullWhen(false)] out string value)
#nullable restore
    {
        if (_nativeHeaders?.Contains(key) == true)
        {
            value = _nativeHeaders.GetHeader(key);
            return true;
        }

        value = null;
        return false;
    }

    string IDictionary<string, string>.this[string key]
    {
        get
        {
            if (_nativeHeaders is null)
                throw new ObjectDisposedException(nameof(WebViewWebRequestHeaders));

            return _nativeHeaders.GetHeader(key) ?? throw new KeyNotFoundException(key);
        }
        set
        {
            if (_nativeHeaders is null)
                throw new ObjectDisposedException(nameof(WebViewWebRequestHeaders));

            var wasSet = _nativeHeaders.TrySetHeader(key, value);
            HasChanges |= wasSet;
        }
    }

    ICollection<string> IDictionary<string, string>.Keys
    {
        get
        {
            if (_nativeHeaders is null)
                throw new ObjectDisposedException(nameof(WebViewWebRequestHeaders));

            var keys = new List<string>();
            var iterator = _nativeHeaders.GetIterator();
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
            if (_nativeHeaders is null)
                throw new ObjectDisposedException(nameof(WebViewWebRequestHeaders));

            var values = new List<string>();
            var iterator = _nativeHeaders.GetIterator();
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
