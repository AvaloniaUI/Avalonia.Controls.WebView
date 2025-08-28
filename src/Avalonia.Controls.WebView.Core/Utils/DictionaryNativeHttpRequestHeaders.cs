using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Avalonia.Controls.Utils;

internal class DictionaryNativeHttpRequestHeaders(IEnumerable<KeyValuePair<string, string>> headers, bool immutable)
    : INativeHttpRequestHeaders
{
    public bool Immutable => !TryGetMutable(out _);

    public bool TryClear()
    {
        if (TryGetMutable(out var dictionary))
        {
            dictionary.Clear();
            return true;
        }

        return false;
    }

    public bool TryGetCount(out int count)
    {
        count = headers switch
        {
            ICollection<KeyValuePair<string, string>> collection => collection.Count,
            IReadOnlyCollection<KeyValuePair<string, string>> readOnlyCollection => readOnlyCollection.Count,
            _ => headers.Count()
        };
        return true;
    }

    public string? GetHeader(string name) => headers switch
    {
        IDictionary<string, string> dict => dict.TryGetValue(name, out var value) ? value : null,
        IReadOnlyDictionary<string, string> readOnlyDict => readOnlyDict.TryGetValue(name, out var value) ? value : null,
        _ => headers.FirstOrDefault(h => h.Key == name).Value
    };

    public bool Contains(string name) => headers switch
    {
        IDictionary<string, string> dict => dict.ContainsKey(name),
        IReadOnlyDictionary<string, string> readOnlyDict => readOnlyDict.ContainsKey(name),
        _ => headers.Any(h => h.Key == name)
    };

    public bool TrySetHeader(string name, string value)
    {
        if (TryGetMutable(out var dictionary))
        {
            dictionary[name] = value;
            return true;
        }

        return false;
    }

    public bool TryRemoveHeader(string name) => TryGetMutable(out var dictionary) && dictionary.Remove(name);

    public INativeHttpHeadersCollectionIterator GetIterator() => new Iterator(headers);

    private bool TryGetMutable([NotNullWhen(true)] out IDictionary<string, string>? dictionary)
    {
        dictionary = null;

        if (immutable)
        {
            return false;
        }

        if (headers is IDictionary<string, string> { IsReadOnly: false } dict)
        {
            dictionary = dict;
            return true;
        }

        return false;
    } 

    public class Iterator(IEnumerable<KeyValuePair<string, string>> dictionary) : INativeHttpHeadersCollectionIterator
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
