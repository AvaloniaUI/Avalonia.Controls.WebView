#pragma once
#include "com.h"
#include "stddef.h"
struct IWebViewFactory;
struct INativeWebView;
struct INativeWebViewHandlers;
struct IAvnString;
COMINTERFACE(IWebViewFactory, 809c652e, 7396, 11d2, 97, 71, 00, a0, cf, b4, d5, 0c) : IUnknown
{
    virtual INativeWebView* CreateWebView (
        INativeWebViewHandlers* handlers
    ) = 0;
};
COMINTERFACE(INativeWebView, e5aca67b, 02b7, 4129, aa, 79, d6, e4, 17, 21, 0b, da) : IUnknown
{
    virtual void* AsNsView () = 0;
    virtual bool GetCanGoBack () = 0;
    virtual bool GoBack () = 0;
    virtual bool GetCanGoForward () = 0;
    virtual bool GoForward () = 0;
    virtual HRESULT GetSource (
        IAvnString** ppv
    ) = 0;
    virtual HRESULT Navigate (
        IAvnString* url
    ) = 0;
    virtual HRESULT NavigateToString (
        IAvnString* text
    ) = 0;
    virtual bool Refresh () = 0;
    virtual bool Stop () = 0;
    virtual HRESULT InvokeScript (
        IAvnString* script, 
        int id
    ) = 0;
};
COMINTERFACE(INativeWebViewHandlers, e5aca67b, 02b7, 4129, aa, 79, d6, e4, 17, 21, 0b, ba) : IUnknown
{
    virtual HRESULT OnScriptResult (
        int id, 
        bool isError, 
        IAvnString* result
    ) = 0;
    virtual HRESULT OnNavigationCompleted (
        IAvnString* url, 
        bool success
    ) = 0;
    virtual HRESULT OnNavigationStarted (
        IAvnString* url, 
        bool* cancel
    ) = 0;
};
COMINTERFACE(IAvnString, 233e094f, 9b9f, 44a3, 9a, 6e, 69, 48, bb, dd, 9f, bb) : IUnknown
{
    virtual HRESULT Pointer (
        void** retOut
    ) = 0;
    virtual HRESULT Length (
        int* ret
    ) = 0;
};
