package com.bumimobile.gallery;

import android.content.Intent;
import com.unity3d.player.UnityPlayerActivity;

public class GalleryActivity extends UnityPlayerActivity {
    private static final int GALLERY_REQUEST_CODE = 1001;
    private static final int PERMISSION_REQUEST_CODE = 1002;

    @Override
    protected void onActivityResult(int requestCode, int resultCode, Intent data) {
        super.onActivityResult(requestCode, resultCode, data);
        
        // Forward to GalleryManager to handle the result
        GalleryManager.onActivityResult(requestCode, resultCode, data);
    }

    @Override
    public void onRequestPermissionsResult(int requestCode, String[] permissions, int[] grantResults) {
        super.onRequestPermissionsResult(requestCode, permissions, grantResults);
        
        // Forward to GalleryManager to handle permission result
        GalleryManager.onRequestPermissionsResult(requestCode, permissions, grantResults);
    }
}
