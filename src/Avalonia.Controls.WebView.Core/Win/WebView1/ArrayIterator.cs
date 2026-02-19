using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.Versioning;
using Avalonia.Controls.Win.WebView1.Interop;

namespace Avalonia.Controls.Win.WebView1;

internal abstract class ArrayIterator<TItem>(
    IEnumerable<TItem> items) : InspectableCallbackBase, IIterable, IIterator, IDisposable
    where TItem : class
{
    private IEnumerator<TItem?> m_enumerator = items.GetEnumerator();
    private bool m_firstItem = true;
    private bool m_hasCurrent;

    IntPtr IIterator.get_Current()
    {
        // IEnumerator starts at item -1, while IIterators start at item 0.  Therefore, if this is the
        // first access to the iterator we need to advance to the first item.
        if (m_firstItem)
        {
            m_firstItem = false;
            MoveNext();
        }

        return GetCurrent(m_enumerator.Current);
    }

    protected abstract IntPtr GetCurrent(TItem? item);

    bool IIterator.get_HasCurrent()
    {
        // IEnumerator starts at item -1, while IIterators start at item 0.  Therefore, if this is the
        // first access to the iterator we need to advance to the first item.
        if (m_firstItem)
        {
            m_firstItem = false;
            MoveNext();
        }

        return m_hasCurrent;
    }

    IIterator IIterable.First()
    {
        m_enumerator.Dispose();
        m_enumerator = items.GetEnumerator();
        m_firstItem = true;
        m_hasCurrent = false;
        return this;
    }

    bool IIterator.MoveNext() => MoveNext();

    uint IIterator.GetMany(uint count, uint itemsSize, IntPtr items)
    {
        // giving up on this implementation, OS should use MoveNext/Current
        return 0;
    }

    private bool MoveNext()
    {
        m_hasCurrent = m_enumerator.MoveNext();

        return m_hasCurrent;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            m_enumerator.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~ArrayIterator()
    {
        Dispose(false);
    }
}


[GeneratedComClass]
[SupportedOSPlatform("windows")]
internal partial class HStringIterator(IEnumerable<string> items) : ArrayIterator<string>(items)
{
    // Can OS release it for us? Will see.
    protected override IntPtr GetCurrent(string? item) => new HStringInterop(item).Handle;

    protected override Guid[] GetIids()
    {
        // might be worth finding GUID for IIterator<HString> and IIterable<HString>.
        // But it wors without ever calling this method.
        throw new NotImplementedException();
    }
}
