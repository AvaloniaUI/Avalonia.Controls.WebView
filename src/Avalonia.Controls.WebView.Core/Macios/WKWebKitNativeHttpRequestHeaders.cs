using System.Collections.Generic;
using Avalonia.Controls.Macios.Interop;
using Avalonia.Controls.Utils;

namespace Avalonia.Controls.Macios;

internal class WKWebKitNativeHttpRequestHeaders(NSURLRequest request) : INativeHttpRequestHeaders
{
    public bool WasMutated { get; private set; }

    public bool TryClear() => false;

    public bool TryGetCount(out int count)
    {
        count = (int)request.AllHTTPHeaderFields.Count;
        return true;
    }

    public string? GetHeader(string name) => request[name];

    public bool Contains(string name)
    {
        return !string.IsNullOrEmpty(request[name]);
    }

    public void SetHeader(string name, string value)
    {
        if (request is NSMutableURLRequest mutable)
        {
            mutable[name] = value;
            WasMutated = true;
        }
    }

    public bool RemoveHeader(string name)
    {
        if (request is NSMutableURLRequest mutable
            && !string.IsNullOrEmpty(request[name]))
        {
            mutable[name] = "";
            WasMutated = true;
            return true;
        }
        return false;
    }

    public INativeHttpHeadersCollectionIterator GetIterator()
    {
        var dictionary = NSDictionary.AsStringDictionary(request.AllHTTPHeaderFields.Handle);
        return new Iterator(dictionary);
    }

    private class Iterator(Dictionary<string, object?> dictionary) : INativeHttpHeadersCollectionIterator
    {
        private readonly IEnumerator<KeyValuePair<string, object?>> _enumerator = dictionary.GetEnumerator();
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
