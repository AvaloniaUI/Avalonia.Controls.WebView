//This file will contain actual IID structures
#define COM_GUIDS_MATERIALIZE
#include "common.h"
#include "AvnString.h"

@interface WebViewHandlers : NSObject<WKNavigationDelegate>
-(id)initWithHandlers: (INativeWebViewHandlers*) arg;
@end

class WebViewNative : public ComSingleObject<INativeWebView, &IID_INativeWebView>
{
private:
    WKWebView* _webView;
    WebViewHandlers* _handlersWrapper;
    INativeWebViewHandlers* _handlers;

public:
    FORWARD_IUNKNOWN()

    WebViewNative(INativeWebViewHandlers* handlers)
    {
        WKWebViewConfiguration* config = [[WKWebViewConfiguration alloc] init];
        CGRect frame = {};
        _webView = [[WKWebView alloc] initWithFrame:frame configuration:config];
        _handlers = handlers;
        _handlersWrapper = [[WebViewHandlers alloc] initWithHandlers: _handlers];
        _webView.navigationDelegate = _handlersWrapper;
    }

    ~WebViewNative()
    {
        _handlersWrapper = nullptr;
        _handlers = nullptr;
        _webView = nullptr;
    }
    
    virtual void* AsNsView () override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            return (__bridge_retained void*)_webView;
        }
    };

    virtual bool GetCanGoBack () override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            return [_webView canGoBack];
        }
    }

    virtual bool GoBack () override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            return [_webView goBack] != nullptr;
        }
    }

    virtual bool GetCanGoForward () override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            return [_webView canGoForward];
        }
    }

    virtual bool GoForward () override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            return [_webView goForward] != nullptr;
        }
    }

    virtual HRESULT GetSource (IAvnString** ppv) override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            auto url = _webView.URL.absoluteString;
            *ppv = CreateAvnString(url);
            return S_OK;
        }
    }

    virtual HRESULT Navigate (IAvnString* url) override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            if (url == nullptr) return E_POINTER;
            NSURL* nsUrl = [NSURL URLWithString: GetNSStringAndRelease(url)];
            NSURLRequest* request = [NSURLRequest requestWithURL: nsUrl];
            [_webView loadRequest:request];
            return S_OK;
        }
    }

    virtual HRESULT NavigateToString (IAvnString* text) override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            if (text == nullptr) return E_POINTER;
            auto navigation = [_webView loadHTMLString: GetNSStringAndRelease(text) baseURL: nullptr];
            return S_OK;
        }
    }

    virtual bool Refresh () override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            return [_webView reload] != nullptr;
        }
    }

    virtual bool Stop () override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            [_webView stopLoading];
            return true;
        }
    }

    virtual HRESULT InvokeScript (IAvnString* script, int index) override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            auto scriptStr = GetNSStringAndRelease(script);
            
            [_webView evaluateJavaScript:scriptStr completionHandler:^(id _Nullable value, NSError * _Nullable error) {
                if (error != nullptr)
                {
                    _handlers->OnScriptResult(index, true, CreateAvnString(error.localizedDescription));
                }
                else
                {
                    _handlers->OnScriptResult(index, false, CreateAvnString((NSString *)value));
                }
            }];
            return S_OK;
        }
    }
};

@implementation WebViewHandlers {
    INativeWebViewHandlers* handler;
}
- (id)initWithHandlers: (INativeWebViewHandlers*) arg
{
    handler = arg;
    return self;
}
- (void)webView:(WKWebView *)webView didFinishNavigation:(WKNavigation *)navigation
{
    @autoreleasepool
    {
        auto url = webView.URL.absoluteString;
        auto str = CreateAvnString(url);
        handler->OnNavigationCompleted(str, true);
    }
}
- (void)webView:(WKWebView *)webView
    decidePolicyForNavigationAction:(WKNavigationAction *)navigationAction
    decisionHandler:(void (^)(WKNavigationActionPolicy))decisionHandler
{
    @autoreleasepool
    {
        auto url = webView.URL.absoluteString;
        auto str = CreateAvnString(url);
        bool cancel = false;
        handler->OnNavigationStarted(str, &cancel);
        if (cancel)
        {
            decisionHandler(WKNavigationActionPolicyCancel);
        }
        else
        {
            decisionHandler(WKNavigationActionPolicyAllow);
        }
    }
}
@end

class WebViewNativeFactory : public ComSingleObject<IWebViewFactory, &IID_IWebViewFactory>
{
public:
    FORWARD_IUNKNOWN()
    
    virtual INativeWebView* CreateWebView (
        INativeWebViewHandlers* handlers
    ) override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            if(handlers == nullptr)
                return nullptr;
            return new WebViewNative(handlers);
        }
    };
};

extern "C" IWebViewFactory* CreateWebViewNativeFactory()
{
    return new WebViewNativeFactory();
};
