import Foundation
import Photos
import UIKit

// Declare the C callbacks from BumiGalleryBridge
@_silgen_name("_OnGalleryResult")
func onGalleryResult(_ result: UnsafePointer<CChar>) -> Void

@_silgen_name("_OnPermissionResult")
func onPermissionResult(_ result: UnsafePointer<CChar>) -> Void

@_silgen_name("_OnUploadResult")
func onUploadResult(_ result: UnsafePointer<CChar>) -> Void

@objc class BumiGalleryManager: NSObject, UIImagePickerControllerDelegate, UINavigationControllerDelegate {
    static let shared = BumiGalleryManager()
    
    private var imagePickerController: UIImagePickerController?
    private var galleryCompletion: ((String?) -> Void)?
    private var permissionCompletion: ((Bool) -> Void)?
    private var uploadCompletion: ((Bool) -> Void)?
    
    private var gameObjectName: String?
    private var callbackMethod: String?
    
    // MARK: - Gallery Permission
    
    @objc func checkGalleryPermission() -> Bool {
        let status = PHPhotoLibrary.authorizationStatus()
        return status == .authorized
    }
    
    @objc func requestGalleryPermission(_ gameObjectName: String, callbackMethod: String) {
        self.gameObjectName = gameObjectName
        self.callbackMethod = callbackMethod
        
        PHPhotoLibrary.requestAuthorization { [weak self] status in
            DispatchQueue.main.async {
                let result = status == .authorized ? "granted" : "denied"
                result.withCString { cStr in
                    onPermissionResult(cStr)
                }
            }
        }
    }
    
    // MARK: - Gallery Picker
    
    @objc func openGallery(_ gameObjectName: String, callbackMethod: String) {
        self.gameObjectName = gameObjectName
        self.callbackMethod = callbackMethod
        
        DispatchQueue.main.async {
            let status = PHPhotoLibrary.authorizationStatus()
            
            if status == .denied || status == .restricted {
                "".withCString { cStr in
                    onGalleryResult(cStr)
                }
                return
            }
            
            if status == .notDetermined {
                PHPhotoLibrary.requestAuthorization { [weak self] newStatus in
                    if newStatus == .authorized {
                        DispatchQueue.main.async {
                            self?.presentImagePicker()
                        }
                    } else {
                        "".withCString { cStr in
                            onGalleryResult(cStr)
                        }
                    }
                }
            } else {
                self.presentImagePicker()
            }
        }
    }
    
    private func presentImagePicker() {
        guard let rootViewController = UIApplication.shared.windows.first?.rootViewController else {
            sendCallback(nil)
            return
        }
        
        imagePickerController = UIImagePickerController()
        imagePickerController?.sourceType = .photoLibrary
        imagePickerController?.mediaTypes = ["public.image"]
        imagePickerController?.delegate = self
        imagePickerController?.allowsEditing = false
        
        rootViewController.present(imagePickerController!, animated: true)
    }
    
    // MARK: - UIImagePickerControllerDelegate
    
    func imagePickerController(_ picker: UIImagePickerController, didFinishPickingMediaWithInfo info: [UIImagePickerController.InfoKey : Any]) {
        picker.dismiss(animated: true)
        
        guard let imageURL = info[.imageURL] as? URL else {
            if let image = info[.originalImage] as? UIImage {
                saveImageAndCallback(image)
            } else {
                "".withCString { cStr in
                    onGalleryResult(cStr)
                }
            }
            return
        }
        
        let imagePath = imageURL.path
        imagePath.withCString { cStr in
            onGalleryResult(cStr)
        }
    }
    
    func imagePickerControllerDidCancel(_ picker: UIImagePickerController) {
        picker.dismiss(animated: true)
        "".withCString { cStr in
            onGalleryResult(cStr)
        }
    }
    
    private func saveImageAndCallback(_ image: UIImage) {
        let documentDirectory = FileManager.default.urls(for: .documentDirectory, in: .userDomainMask)[0]
        let fileName = "gallery_image_\(UUID().uuidString).jpg"
        let fileURL = documentDirectory.appendingPathComponent(fileName)
        
        if let data = image.jpegData(compressionQuality: 0.8) {
            do {
                try data.write(to: fileURL)
                let path = fileURL.path
                path.withCString { cStr in
                    onGalleryResult(cStr)
                }
            } catch {
                print("Failed to save image: \(error)")
                "".withCString { cStr in
                    onGalleryResult(cStr)
                }
            }
        }
    }
    
    // MARK: - Image Upload
    
    @objc func uploadImage(_ imagePath: String, uploadUrl: String, fieldName: String, gameObjectName: String, callbackMethod: String) {
        self.gameObjectName = gameObjectName
        self.callbackMethod = callbackMethod
        
        guard let imageURL = URL(string: imagePath) ?? URL(fileURLWithPath: imagePath) else {
            "failed".withCString { cStr in
                onUploadResult(cStr)
            }
            return
        }
        
        guard let url = URL(string: uploadUrl) else {
            "failed".withCString { cStr in
                onUploadResult(cStr)
            }
            return
        }
        
        do {
            let imageData = try Data(contentsOf: imageURL)
            
            var request = URLRequest(url: url)
            request.httpMethod = "POST"
            
            let boundary = UUID().uuidString
            request.setValue("multipart/form-data; boundary=\(boundary)", forHTTPHeaderField: "Content-Type")
            
            var body = Data()
            
            // Add image data
            let imageName = imageURL.lastPathComponent
            let mimeType = "image/jpeg"
            
            body.append("--\(boundary)\r\n".data(using: .utf8)!)
            body.append("Content-Disposition: form-data; name=\"\(fieldName)\"; filename=\"\(imageName)\"\r\n".data(using: .utf8)!)
            body.append("Content-Type: \(mimeType)\r\n\r\n".data(using: .utf8)!)
            body.append(imageData)
            body.append("\r\n--\(boundary)--\r\n".data(using: .utf8)!)
            
            request.httpBody = body
            
            URLSession.shared.dataTask(with: request) { [weak self] data, response, error in
                DispatchQueue.main.async {
                    if let httpResponse = response as? HTTPURLResponse, (200...299).contains(httpResponse.statusCode) {
                        "success".withCString { cStr in
                            onUploadResult(cStr)
                        }
                    } else {
                        "failed".withCString { cStr in
                            onUploadResult(cStr)
                        }
                    }
                }
            }.resume()
            
        } catch {
            print("Error uploading image: \(error)")
            "failed".withCString { cStr in
                onUploadResult(cStr)
            }
        }
    }
    
    // MARK: - Private Helpers
    
    // Callback functions are now handled directly via the C bridge above
