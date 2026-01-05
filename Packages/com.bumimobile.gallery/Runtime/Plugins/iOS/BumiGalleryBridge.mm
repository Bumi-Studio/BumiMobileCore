//
//  BumiGalleryBridge.mm
//  Implementation of the bridge between C# and Swift for gallery operations
//

#include "BumiGalleryBridge.h"
#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>

// Forward declare the Swift class
@class BumiGalleryManager;

static NSString* g_gameObjectName = nil;
static NSString* g_callbackMethod = nil;

// Helper function to send message back to Unity
void SendUnityMessage(const char* gameObject, const char* method, const char* message) {
    if (gameObject && method) {
        UnitySendMessage(gameObject, method, message ? message : "");
    }
}

// MARK: - Gallery Permission Functions

bool _CheckGalleryPermission(void) {
    SEL selector = @selector(checkGalleryPermission);
    BumiGalleryManager* manager = (BumiGalleryManager*)[[NSClassFromString(@"BumiGalleryManager") alloc] init];
    
    if ([manager respondsToSelector:selector]) {
        NSNumber* result = [manager performSelector:selector];
        return [result boolValue];
    }
    return false;
}

void _RequestGalleryPermission(const char* gameObjectName, const char* callbackMethod) {
    g_gameObjectName = [NSString stringWithUTF8String:gameObjectName];
    g_callbackMethod = [NSString stringWithUTF8String:callbackMethod];
    
    SEL selector = @selector(requestGalleryPermission:callbackMethod:);
    BumiGalleryManager* manager = (BumiGalleryManager*)[[NSClassFromString(@"BumiGalleryManager") alloc] init];
    
    if ([manager respondsToSelector:selector]) {
        [manager performSelector:selector withObject:g_gameObjectName withObject:g_callbackMethod];
    }
}

// MARK: - Gallery Picker Functions

void _OpenGallery(const char* gameObjectName, const char* callbackMethod) {
    g_gameObjectName = [NSString stringWithUTF8String:gameObjectName];
    g_callbackMethod = [NSString stringWithUTF8String:callbackMethod];
    
    SEL selector = @selector(openGallery:callbackMethod:);
    BumiGalleryManager* manager = (BumiGalleryManager*)[[NSClassFromString(@"BumiGalleryManager") alloc] init];
    
    if ([manager respondsToSelector:selector]) {
        [manager performSelector:selector withObject:g_gameObjectName withObject:g_callbackMethod];
    }
}

// MARK: - Upload Functions

void _UploadImage(const char* imagePath, const char* uploadUrl, const char* fieldName, const char* gameObjectName, const char* callbackMethod) {
    NSString* path = [NSString stringWithUTF8String:imagePath];
    NSString* url = [NSString stringWithUTF8String:uploadUrl];
    NSString* field = [NSString stringWithUTF8String:fieldName];
    NSString* object = [NSString stringWithUTF8String:gameObjectName];
    NSString* method = [NSString stringWithUTF8String:callbackMethod];
    
    g_gameObjectName = object;
    g_callbackMethod = method;
    
    SEL selector = @selector(uploadImage:uploadUrl:fieldName:gameObjectName:callbackMethod:);
    BumiGalleryManager* manager = (BumiGalleryManager*)[[NSClassFromString(@"BumiGalleryManager") alloc] init];
    
    if ([manager respondsToSelector:selector]) {
        [manager performSelector:selector withObject:path withObject:url withObject:field withObject:object withObject:method];
    }
}

// MARK: - Callback Helper (Called from Swift)

extern "C" {
    void _OnGalleryResult(const char* result) {
        SendUnityMessage([g_gameObjectName UTF8String], [g_callbackMethod UTF8String], result);
    }
    
    void _OnPermissionResult(const char* result) {
        SendUnityMessage([g_gameObjectName UTF8String], [g_callbackMethod UTF8String], result);
    }
    
    void _OnUploadResult(const char* result) {
        SendUnityMessage([g_gameObjectName UTF8String], [g_callbackMethod UTF8String], result);
    }
}
