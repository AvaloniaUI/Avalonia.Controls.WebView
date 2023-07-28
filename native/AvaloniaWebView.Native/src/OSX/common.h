#ifndef common_h
#define common_h
#include "comimpl.h"
#include "webview-native.h"
#include <stdio.h>
#import <Foundation/Foundation.h>
#import <WebKit/WebKit.h>
#import <WebKit/WKNavigationDelegate.h>

template<typename T> inline T* objc_cast(id from) {
    if(from == nil)
        return nil;
    if ([from isKindOfClass:[T class]]) {
        return static_cast<T*>(from);
    }
    return nil;
}

template<typename T> class ObjCWrapper {
public:
    T* Value;
    ObjCWrapper(T* value)
    {
        Value = value;
    }
    operator T*() const
    {
        return Value;
    }
    T* operator->() const
    {
        return Value;
    }
    ~ObjCWrapper()
    {
        Value = nil;
    }
};



#define START_COM_ARP_CALL START_ARP_CALL; START_COM_CALL

#endif
