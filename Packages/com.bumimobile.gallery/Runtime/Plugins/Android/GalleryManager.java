package com.bumimobile.gallery;

import android.Manifest;
import android.app.Activity;
import android.content.Intent;
import android.content.pm.PackageManager;
import android.graphics.Bitmap;
import android.net.Uri;
import android.os.Build;
import android.provider.MediaStore;
import androidx.core.app.ActivityCompat;
import androidx.core.content.ContextCompat;
import com.unity3d.player.UnityPlayer;
import java.io.File;
import java.io.FileOutputStream;
import java.io.InputStream;
import java.net.URL;
import java.util.UUID;

public class GalleryManager {
    private static final int GALLERY_REQUEST_CODE = 1001;
    private static final int PERMISSION_REQUEST_CODE = 1002;
    private static String callbackGameObject;
    private static String callbackMethod;
    private static Activity activity;

    static {
        activity = UnityPlayer.currentActivity;
    }

    /**
     * Checks if the app has permission to read gallery/photos.
     */
    public static boolean hasPermission() {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
            return ContextCompat.checkSelfPermission(
                    activity,
                    Manifest.permission.READ_MEDIA_IMAGES
            ) == PackageManager.PERMISSION_GRANTED;
        } else {
            return ContextCompat.checkSelfPermission(
                    activity,
                    Manifest.permission.READ_EXTERNAL_STORAGE
            ) == PackageManager.PERMISSION_GRANTED;
        }
    }

    /**
     * Requests permission to access gallery.
     */
    public static void requestPermission(String gameObject, String method) {
        callbackGameObject = gameObject;
        callbackMethod = method;
        
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
            ActivityCompat.requestPermissions(
                    activity,
                    new String[]{Manifest.permission.READ_MEDIA_IMAGES},
                    PERMISSION_REQUEST_CODE
            );
        } else {
            ActivityCompat.requestPermissions(
                    activity,
                    new String[]{Manifest.permission.READ_EXTERNAL_STORAGE},
                    PERMISSION_REQUEST_CODE
            );
        }
    }

    /**
     * Opens the native gallery/photo picker.
     */
    public static void openGallery(String gameObject, String method) {
        callbackGameObject = gameObject;
        callbackMethod = method;

        if (!hasPermission()) {
            requestPermission(gameObject, method);
            return;
        }

        Intent intent = new Intent();
        intent.setType("image/*");
        intent.setAction(Intent.ACTION_GET_CONTENT);
        intent.addCategory(Intent.CATEGORY_OPENABLE);

        Intent chooser = Intent.createChooser(intent, "Select an Image");
        activity.startActivityForResult(chooser, GALLERY_REQUEST_CODE);
    }

    /**
     * Uploads an image to the specified URL.
     */
    public static void uploadImage(String imagePath, String uploadUrl, String fieldName, String gameObject, String method) {
        callbackGameObject = gameObject;
        callbackMethod = method;
        
        new Thread(() -> {
            try {
                File imageFile = new File(imagePath);
                if (!imageFile.exists()) {
                    sendCallback("failed");
                    return;
                }

                URL url = new URL(uploadUrl);
                String boundary = UUID.randomUUID().toString();

                java.net.HttpURLConnection connection = (java.net.HttpURLConnection) url.openConnection();
                connection.setRequestMethod("POST");
                connection.setRequestProperty("Content-Type", "multipart/form-data; boundary=" + boundary);
                connection.setDoOutput(true);

                java.io.OutputStream outputStream = connection.getOutputStream();
                java.io.PrintWriter writer = new java.io.PrintWriter(
                        new java.io.OutputStreamWriter(outputStream, "UTF-8"),
                        true
                );

                // Write boundary
                writer.append("--").append(boundary).append("\r\n");

                // Write form data
                writer.append("Content-Disposition: form-data; name=\"")
                        .append(fieldName)
                        .append("\"; filename=\"")
                        .append(imageFile.getName())
                        .append("\"\r\n");
                writer.append("Content-Type: application/octet-stream\r\n");
                writer.append("Content-Transfer-Encoding: binary\r\n\r\n");
                writer.flush();

                // Write file content
                java.io.FileInputStream fileInputStream = new java.io.FileInputStream(imageFile);
                byte[] buffer = new byte[4096];
                int bytesRead;
                while ((bytesRead = fileInputStream.read(buffer)) != -1) {
                    outputStream.write(buffer, 0, bytesRead);
                }
                fileInputStream.close();

                // Write end boundary
                writer.append("\r\n");
                writer.append("--").append(boundary).append("--\r\n");
                writer.flush();
                writer.close();

                int responseCode = connection.getResponseCode();
                if (responseCode >= 200 && responseCode < 300) {
                    sendCallback("success");
                } else {
                    sendCallback("failed");
                }

                connection.disconnect();

            } catch (Exception e) {
                e.printStackTrace();
                sendCallback("failed");
            }
        }).start();
    }

    /**
     * Handles activity result callback (for gallery selection).
     * This is called from GalleryActivity.onActivityResult()
     */
    public static void onActivityResult(int requestCode, int resultCode, Intent data) {
        if (requestCode == GALLERY_REQUEST_CODE && resultCode == Activity.RESULT_OK && data != null) {
            Uri selectedImageUri = data.getData();
            if (selectedImageUri != null) {
                String imagePath = getRealPathFromUri(selectedImageUri);
                sendCallback(imagePath != null ? imagePath : "");
            } else {
                sendCallback("");
            }
        } else {
            // User cancelled
            sendCallback("");
        }
    }

    /**
     * Handles permission result callback.
     * This is called from GalleryActivity.onRequestPermissionsResult()
     */
    public static void onRequestPermissionsResult(int requestCode, String[] permissions, int[] grantResults) {
        if (requestCode == PERMISSION_REQUEST_CODE) {
            boolean granted = grantResults.length > 0 && grantResults[0] == PackageManager.PERMISSION_GRANTED;
            
            if (granted) {
                // Permission granted, open gallery
                openGallery(callbackGameObject, callbackMethod);
            } else {
                // Permission denied
                sendCallback("");
            }
        }
    }

    /**
     * Gets the real file path from a URI.
     */
    private static String getRealPathFromUri(Uri uri) {
        String result = null;

        if (uri.getScheme().equals("content")) {
            try {
                InputStream inputStream = activity.getContentResolver().openInputStream(uri);
                File file = new File(activity.getCacheDir(), UUID.randomUUID().toString() + ".jpg");

                FileOutputStream outputStream = new FileOutputStream(file);
                byte[] buffer = new byte[4096];
                int bytesRead;
                while ((bytesRead = inputStream.read(buffer)) != -1) {
                    outputStream.write(buffer, 0, bytesRead);
                }
                inputStream.close();
                outputStream.close();

                result = file.getAbsolutePath();
            } catch (Exception e) {
                e.printStackTrace();
            }
        } else if (uri.getScheme().equals("file")) {
            result = uri.getPath();
        }

        return result;
    }

    /**
     * Sends a callback to Unity.
     */
    private static void sendCallback(String result) {
        if (callbackGameObject != null && callbackMethod != null) {
            UnityPlayer.UnitySendMessage(callbackGameObject, callbackMethod, result != null ? result : "");
        }
    }
}
