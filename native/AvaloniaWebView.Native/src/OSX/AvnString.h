//
//  AvnString.h
//  Avalonia.Native.OSX
//
//  Created by Dan Walmsley on 07/11/2018.
//  Copyright © 2018 Avalonia. All rights reserved.
//

#ifndef AvnString_h
#define AvnString_h

extern IAvnString* CreateAvnString(NSString* string);
extern IAvnString* CreateByteArray(void* data, int len);
extern NSString* GetNSStringAndRelease(IAvnString* s);
#endif /* AvnString_h */
