//This file will contain actual IID structures
#define COM_GUIDS_MATERIALIZE
#include "common.h"
#include "AvnString.h"
#include "KeyTransform.h"

@interface AvaloniaWKWebView : WKWebView
@property (nonatomic,strong) id localEventHandler;
@end

@interface WebViewDelegate : NSObject<WKNavigationDelegate, WKScriptMessageHandler>
-(id)initWithHandlers: (INativeWebViewHandlers*) arg;
-(void)releaseHandlers;
-(void)onScriptResult:(int)index withResult:(id)result withError:(NSError*)error;
-(void)onBecameFirstResponder;
-(void)onResignedFirstResponder;
-(bool)onKeyDown: (AvnPhysicalKey) key withMods: (AvnInputModifiers) mod;
-(bool)onKeyUp: (AvnPhysicalKey) key withMods: (AvnInputModifiers) mod;
@end

NSMutableArray* _handlersArray = [[NSMutableArray alloc] init];

class WebViewNative : public ComSingleObject<INativeWebView, &IID_INativeWebView>
{
private:
    AvaloniaWKWebView* _webView;
    WebViewDelegate* _handlersWrapper;

public:
    FORWARD_IUNKNOWN()

    WebViewNative(WebViewDelegate* handlers)
    {
        WKWebViewConfiguration* config = [[WKWebViewConfiguration alloc] init];
        if (@available(macOS 11, *)) {
            [[config defaultWebpagePreferences] setAllowsContentJavaScript: true];
        }
        else {
            [[config preferences] setJavaScriptEnabled: true];
        }
        [config.userContentController addScriptMessageHandler:handlers name:@"postWebViewMessage"];
        [config.preferences setValue:@YES forKey:@"developerExtrasEnabled"]; // only for debug

        CGRect frame = {};
        _webView = [[AvaloniaWKWebView alloc] initWithFrame:frame configuration:config];
        _webView.navigationDelegate = handlers;
        _handlersWrapper = handlers;
    }

    virtual HRESULT ReleaseUnmanaged() override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            [_handlersWrapper releaseHandlers];
            [_handlersArray removeObject: _handlersWrapper];
            
            _handlersWrapper = nullptr;
            if (NSThread.isMainThread)
            {
                if (_webView.superview != nullptr)
                {
                    [_webView removeFromSuperview];
                }

                _webView.navigationDelegate = nullptr;
            }
            _webView = nullptr;
            return S_OK;
        }
    }

    virtual void* AsNsView () override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            return (__bridge void*)_webView;
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
            NSURL* nsUrl = [NSURL URLWithString: GetNSStringWithoutRelease(url)];
            NSURLRequest* request = [NSURLRequest requestWithURL: nsUrl];
            [_webView loadRequest:request];
            return S_OK;
        }
    }

    virtual HRESULT NavigateToString (IAvnString* text, IAvnString* baseUrl) override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            if (text == nullptr) return E_POINTER;
            NSURL* baseNsUrl = nullptr;
            if (baseUrl != nullptr)
            {
                baseNsUrl = [NSURL URLWithString: GetNSStringWithoutRelease(baseUrl)];
            }
            
            [_webView loadHTMLString: GetNSStringWithoutRelease(text) baseURL: baseNsUrl];
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
            auto scriptStr = GetNSStringWithoutRelease(script);
            
            [_webView evaluateJavaScript:scriptStr completionHandler:^(id _Nullable value, NSError * _Nullable error) {
                [_handlersWrapper onScriptResult: index withResult:value withError:error];
            }];
            return S_OK;
        }
    }

    virtual bool Focus () override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            auto window = [_webView window];
            if (window)
            {
                return [window makeFirstResponder: _webView];
            }
            return false;
        }
    }

    virtual bool ResignFocus() override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            auto window = [_webView window];
            if (window)
            {
                auto firstResponder = [window firstResponder];
                auto avaloniaView = [[_webView superview] superview];
                if (avaloniaView && firstResponder == _webView)
                {
                    return [window makeFirstResponder: avaloniaView];
                }
            }
            return false;
        }
    }
};

@implementation AvaloniaWKWebView

- (BOOL)acceptsFirstResponder {
    return true;
}
- (BOOL)performKeyEquivalent:(NSEvent *)theEvent {
    auto firstResponder = [[self window] firstResponder];
    if (firstResponder != self)
        return [super performKeyEquivalent: theEvent];

    auto chars = [theEvent charactersIgnoringModifiers];
    auto code = [theEvent keyCode];

    auto modifier = [theEvent modifierFlags];
    if ([theEvent type] == NSEventTypeKeyDown)
    {
        SEL selector = NULL;
        auto isCommandFlag = (modifier & NSEventModifierFlagCommand) != 0;
        
        if (isCommandFlag) {
            if ([chars isEqualToString:@"c"]) {
                selector = @selector(copy:);
            } else if ([chars isEqualToString:@"v"]) {
                selector = @selector(paste:);
            } else if ([chars isEqualToString:@"x"]) {
                selector = @selector(cut:);
            } else if ([chars isEqualToString:@"a"]) {
                selector = @selector(selectAll:);
            }
        }

        if (selector != NULL) {
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Warc-performSelector-leaks"
            [self performSelector:selector];
#pragma clang diagnostic pop
            return true;
        }
    }

    unsigned int modifiers = 0;
    if (modifier & NSEventModifierFlagControl)
        modifiers |= Control;
    if (modifier & NSEventModifierFlagShift)
        modifiers |= Shift;
    if (modifier & NSEventModifierFlagOption)
        modifiers |= Alt;
    if (modifier & NSEventModifierFlagCommand)
        modifiers |= Windows;
    auto physicalKey = getAvnPhysicalKeyForCode(code);

    if (physicalKey != 0
        && (modifiers != 0 || (physicalKey >= AvnPhysicalKeyF1 && physicalKey <= AvnPhysicalKeyF12)))
    {
        auto delegate = [self navigationDelegate];
        auto handler = (WebViewDelegate*)delegate;
        if (handler)
        {
            bool handled = false;

            if (modifiers != 0) {
                if (modifier & NSEventModifierFlagControl)
                    [handler onKeyDown: AvnPhysicalKeyControlLeft withMods:AvnInputModifiersNone];
                if (modifier & NSEventModifierFlagShift)
                    [handler onKeyDown: AvnPhysicalKeyShiftLeft withMods:AvnInputModifiersNone];
                if (modifier & NSEventModifierFlagOption)
                    [handler onKeyDown: AvnPhysicalKeyAltLeft withMods:AvnInputModifiersNone];
                if (modifier & NSEventModifierFlagCommand)
                    [handler onKeyDown: AvnPhysicalKeyMetaLeft withMods:AvnInputModifiersNone];
            }

            handled = [handler onKeyDown: physicalKey withMods:(AvnInputModifiers)modifiers]
                && [handler onKeyUp: physicalKey withMods:(AvnInputModifiers)modifiers];

            if (modifiers != 0) {
                if (modifier & NSEventModifierFlagControl)
                    [handler onKeyUp: AvnPhysicalKeyControlLeft withMods:AvnInputModifiersNone];
                if (modifier & NSEventModifierFlagShift)
                    [handler onKeyUp: AvnPhysicalKeyShiftLeft withMods:AvnInputModifiersNone];
                if (modifier & NSEventModifierFlagOption)
                    [handler onKeyUp: AvnPhysicalKeyAltLeft withMods:AvnInputModifiersNone];
                if (modifier & NSEventModifierFlagCommand)
                    [handler onKeyUp: AvnPhysicalKeyMetaLeft withMods:AvnInputModifiersNone];
            }

            if (handled)
                return true;
        }
    }

    return [super performKeyEquivalent: theEvent];
}
- (void)keyDown:(NSEvent *)event {
    [super keyDown: event];
}
- (void)keyUp:(NSEvent *)event {
    [super keyUp: event];
}
- (void)flagsChanged:(NSEvent *)event {
    [super flagsChanged: event];
}
- (BOOL)becomeFirstResponder {
    if (![super becomeFirstResponder])
        return false;

    [self notifyOnBecameFirstResponder];
    return true;
}
-(void) notifyOnBecameFirstResponder{
    auto delegate = [self navigationDelegate];
    auto handler = (WebViewDelegate*)delegate;
    if (handler) {
        [handler onBecameFirstResponder];
    }
}

- (BOOL)resignFirstResponder {
    if (![super resignFirstResponder])
        return false;

    [self notifyOnResignedFirstResponder];
    return true;
}
-(void) notifyOnResignedFirstResponder{
    auto delegate = [self navigationDelegate];
    auto handler = (WebViewDelegate*)delegate;
    if (handler) {
        [handler onResignedFirstResponder];
    }
}
@end


@implementation WebViewDelegate {
    ComPtr<INativeWebViewHandlers> handler;
}
- (id)initWithHandlers: (INativeWebViewHandlers*) arg
{
    handler = arg;
    return self;
}
- (void)releaseHandlers
{
    handler = nil;
}
- (void)onBecameFirstResponder {
    handler->OnBecameFirstResponder();
}
- (void)onResignedFirstResponder {
    handler->OnResignedFirstResponder();
}
- (BOOL)onKeyDown: (AvnPhysicalKey) key withMods: (AvnInputModifiers) mod {
    return handler->OnKeyDown(mod, key);
}
- (BOOL)onKeyUp: (AvnPhysicalKey) key withMods: (AvnInputModifiers) mod {
    return handler->OnKeyUp(mod, key);
}
- (void)webView:(WKWebView *)webView didFinishNavigation:(WKNavigation *)navigation
{
    if (handler == nullptr) return;

    [self notifyOnNavigationCompleted: webView.URL.absoluteString];
}
- (void) notifyOnNavigationCompleted:(NSString*)url {
    @autoreleasepool
    {
        auto str = CreateAvnString(url);
        handler->OnNavigationCompleted(str, true);
    }
}

- (void)webView:(WKWebView *)webView
    decidePolicyForNavigationAction:(WKNavigationAction *)navigationAction
    decisionHandler:(void (^)(WKNavigationActionPolicy))decisionHandler
{
    if (handler == nullptr) return;

    bool cancel = false;
    [self notifyOnNavigationStarted: webView.URL.absoluteString withCancel: &cancel];
    if (cancel)
    {
        decisionHandler(WKNavigationActionPolicyCancel);
    }
    else
    {
        decisionHandler(WKNavigationActionPolicyAllow);
    }
}
- (void) notifyOnNavigationStarted:(NSString*)url withCancel:(bool*) cancel {
    
    @autoreleasepool
    {
        auto str = CreateAvnString(url);
        handler->OnNavigationStarted(str, cancel);
    }
}

-(void)onScriptResult:(int)index withResult:(id)result withError:(NSError*)error
{
    if (handler == nullptr) return;

    @autoreleasepool
    {
        if (error != nullptr)
        {
            handler->OnScriptResult(index, true, CreateAvnString(error.localizedDescription));
        }
        else
        {
            handler->OnScriptResult(index, false, CreateAvnString((NSString *)result));
        }
    }
}

- (void)userContentController:(WKUserContentController *)userContentController didReceiveScriptMessage:(WKScriptMessage *)message
{
    if ([message.name isEqualToString:@"postWebViewMessage"])
    {
        @autoreleasepool
        {
            auto str = CreateAvnString((NSString *)message.body);
            handler->OnWebMessageReceived(str);
        }
    }
}
@end

class WebViewNativeFactory : public ComSingleObject<IWebViewFactory, &IID_IWebViewFactory>
{
public:
    FORWARD_IUNKNOWN()
    
    virtual INativeWebView* CreateWebView (
        INativeWebViewHandlers* handlers) override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            if(handlers == nullptr)
                return nullptr;
            
            if (_handlersArray == nullptr)
                _handlersArray = [[NSMutableArray alloc] init];
            
            auto handlersWrapper = [[WebViewDelegate alloc] initWithHandlers: handlers];
            [_handlersArray addObject: handlersWrapper];
            return new WebViewNative(handlersWrapper);
        }
    }

    virtual HRESULT InvalidateAllManagedReferences () override
    {
        for (WebViewDelegate * item in _handlersArray)
        {
            [item releaseHandlers];
        }
        [_handlersArray removeAllObjects];
        
        return S_OK;
    }
};

extern "C" IWebViewFactory* CreateWebViewNativeFactory()
{
    return new WebViewNativeFactory();
};
