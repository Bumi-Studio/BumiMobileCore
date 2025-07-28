# 🔊 Audio Module – Unity Modular Toolkit

The `Audio` module serves as a global sound manager for both 2D and 3D audio in Unity.  
It supports SFX/BGM playback, per-type volume control (Music/Sound), and optimized performance through AudioSource pooling.

---

## ✅ Features

- 🔉 Play `AudioClip` (2D & 3D) with pitch and volume control
- 🎧 Automatically creates an `AudioListener` and allows it to be reattached
- 🔁 AudioSource pooling for better performance
- 💾 Persistent volume settings for each audio type (Music/Sound)
- ⚙️ Supports customizable 3D rolloff, spread, and max distance
- 🧩 Easy to use: `AudioController.PlaySound(...)`

---

## ⚙️ How to Use

### 1. Initialize at game start:

```csharp
AudioController.Init(audioClipsScriptableObject, 10);
```
### 2. Play 2D sound:
```csharp
AudioController.PlaySound(audioClip, pitch: 1.2f);
```
### 3. Play 3D sound:
```csharp
AudioController.PlaySound(audioClip, transform.position);
```
### 4. Set volume:
```csharp
AudioController.SetVolume(AudioType.Music, 0.5f);
```
### 5. Get volume:
```csharp
float vol = AudioController.GetVolume(AudioType.Sound);
```
### 6. Attach AudioListener to an object:
```csharp
AudioController.AttachAudioListener(Camera.main.transform);
```
