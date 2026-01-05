//
//  BumiGalleryBridge.h
//  Bridge between C# and Swift for gallery operations
//

#ifndef BumiGalleryBridge_h
#define BumiGalleryBridge_h

#ifdef __cplusplus
extern "C" {
#endif

// Gallery permission functions
bool _CheckGalleryPermission(void);
void _RequestGalleryPermission(const char* gameObjectName, const char* callbackMethod);

// Gallery picker functions
void _OpenGallery(const char* gameObjectName, const char* callbackMethod);

// Upload functions
void _UploadImage(const char* imagePath, const char* uploadUrl, const char* fieldName, const char* gameObjectName, const char* callbackMethod);

#ifdef __cplusplus
}
#endif

#endif /* BumiGalleryBridge_h */
