using System.Collections;
using System.IO;
using UnityEngine;

namespace BumiMobile
{
    public static class NativeShareExampleScript
    {
        public static void TakeScreenshotAndShare()
        {
            Tween.InvokeCoroutine(TakeScreenshotAndShareCoroutine());
        }
        
        private static IEnumerator TakeScreenshotAndShareCoroutine()
        {
            yield return new WaitForEndOfFrame();

            Texture2D ss = new Texture2D( Screen.width, Screen.height, TextureFormat.RGB24, false );
            ss.ReadPixels( new Rect( 0, 0, Screen.width, Screen.height ), 0, 0 );
            ss.Apply();

            string filePath = Path.Combine( Application.temporaryCachePath, "shared img.png" );
            File.WriteAllBytes( filePath, ss.EncodeToPNG() );

            // To avoid memory leaks
            Object.Destroy(ss);

            new NativeShare()
                .AddFile(filePath)
                .SetSubject("Download Super Bolts Puzzle" )
                .SetText( "Super Bolts Puzzle on Play Store!" )
                .SetUrl( "\nhttps://t.ly/gearwizonplaystore" )
                .SetCallback( ( result, shareTarget ) => Debug.Log( "Share result: " + result + ", selected app: " + shareTarget ) )
                .Share();
        }
    }
}
