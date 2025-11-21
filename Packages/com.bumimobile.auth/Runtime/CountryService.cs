// CountryService (GPS ONLY VERSION)
// ------------------------------------------------------------------
// Disederhanakan sesuai permintaan: HANYA pakai GPS + reverse geocode (Android).
// Tidak ada lagi: Telephony, Locale, CultureInfo, SystemLanguage heuristics.
// Alur:
//   InitializeAsync() -> Start LocationService -> tunggu -> ambil koordinat -> reverse geocode (Android) -> set ISO.
//   Jika gagal / tidak izin / platform tidak didukung -> ISO = "ZZ".
// iOS: placeholder (perlu native plugin CLGeocoder kalau ingin).
// Manual override & cache DIPANGKAS (bisa ditambah lagi jika perlu).
// ------------------------------------------------------------------

using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.Networking;

namespace BumiMobile
{
    public static class CountryService
    {
        public static string CountryISO { get; private set; } = "ZZ";   // ISO2 uppercase
        // Human-friendly display name (resolved after ISO known). Falls back to ISO or "Unknown".
        public static string CountryName { get; private set; } = "Unknown";
        public static bool IsInitialized { get; private set; }
        public static event Action OnCountryResolved; // dipanggil sekali saat selesai init

        // Optional database for flags + custom display names
        static CountryFlagDatabase flagDb;
        public static CountryFlagDatabase FlagDatabase => flagDb; // public read access
        public static void SetFlagDatabase(CountryFlagDatabase db)
        {
            flagDb = db;
            if (IsInitialized)
                CountryName = ResolveDisplayName(CountryISO);
        }

        /// <summary>Get flag sprite for current ISO or provided isoOverride (returns null if unknown).</summary>
        public static Sprite GetFlagSprite(string isoOverride = null)
        {
            if (flagDb == null) return null;
            string iso = string.IsNullOrWhiteSpace(isoOverride) ? CountryISO : isoOverride.Trim().ToUpperInvariant();
            if (string.IsNullOrEmpty(iso) || iso == "ZZ" || iso.Length != 2) return null;
            return flagDb.GetSprite(iso);
        }

        // ---- Config / Cache --------------------------------------------------
        const string PREF_KEY = "country.iso.cached"; // PlayerPrefs key (string)
        const float DEFAULT_IP_TIMEOUT = 5f;           // seconds
        const float DEFAULT_GPS_TIMEOUT = 6f;          // seconds (existing behavior)

        // ---- Public API Summary ---------------------------------------------
        // InitializeAsync(): Idempotent. Uses cache -> IP API -> GPS fallback -> ZZ.
        // ForceRedetectAsync(): Clears cache & re-runs full detection.

        /// <summary>Inisialisasi (idempotent). Aman dipanggil berkali-kali.</summary>
        public static async UniTask InitializeAsync(float ipTimeoutSeconds = DEFAULT_IP_TIMEOUT, float gpsTimeoutSeconds = DEFAULT_GPS_TIMEOUT)
        {
            if (IsInitialized) return;
            // 1) Cache
            if (TryLoadCachedIso(out var cached))
            {
                CountryISO = cached;
                CountryName = ResolveDisplayName(CountryISO);
                IsInitialized = true;
                Debug.Log("[Country] Using cached ISO: " + CountryISO);
                OnCountryResolved?.Invoke();
                return;
            }

            // 2) IP Geolocation API
            string isoFromIp = await ResolveViaIpApiAsync(ipTimeoutSeconds);
            if (IsValidIso2(isoFromIp))
            {
                CountryISO = isoFromIp;
                CacheIso(CountryISO);
                CountryName = ResolveDisplayName(CountryISO);
                IsInitialized = true;
                Debug.Log("[Country] IP API resolved ISO: " + CountryISO);
                OnCountryResolved?.Invoke();
                return;
            }
            Debug.LogWarning("[Country] IP API failed or invalid. No fallback (API-only mode). Country remains ZZ.");
            CountryISO = "ZZ"; // API only: stay unknown if API fails
            CountryName = ResolveDisplayName(CountryISO);
            IsInitialized = true;
            OnCountryResolved?.Invoke();
        }

        /// <summary>Paksa re-detect (hapus cache + ulang full chain IP -> GPS).</summary>
        public static async UniTask ForceRedetectAsync()
        {
            ClearCache();
            IsInitialized = false;
            CountryISO = "ZZ";
            CountryName = ResolveDisplayName(CountryISO);
            await InitializeAsync();
        }

        /// <summary>Manual override (misal user memilih di settings). Menyimpan ke cache.</summary>
        // Manual override dihapus untuk versi ini. Tambah lagi jika diperlukan.

        // Utilities ---------------------------------------------------------

        // Cache logic dihapus.

        static bool IsValidIso2(string iso) => !string.IsNullOrEmpty(iso) && iso.Length == 2 && char.IsLetter(iso[0]) && char.IsLetter(iso[1]);

        // ---- Cache Helpers ---------------------------------------------------
        static bool TryLoadCachedIso(out string iso)
        {
            iso = null;
            if (!PlayerPrefs.HasKey(PREF_KEY)) return false;
            var val = PlayerPrefs.GetString(PREF_KEY, null);
            if (IsValidIso2(val)) { iso = val.ToUpperInvariant(); return true; }
            return false;
        }
        static void CacheIso(string iso)
        {
            if (!IsValidIso2(iso)) return;
            PlayerPrefs.SetString(PREF_KEY, iso.ToUpperInvariant());
            PlayerPrefs.Save();
        }
        public static void ClearCache()
        {
            if (PlayerPrefs.HasKey(PREF_KEY)) { PlayerPrefs.DeleteKey(PREF_KEY); PlayerPrefs.Save(); }
        }

        // ---- IP Geo ---------------------------------------------------------
        // Using https://ipwho.is/ (no key) -> field country_code (ISO2) & success boolean.
        [Serializable]
        class IpWhoResponse { public bool success; public string country_code; }

        static async UniTask<string> ResolveViaIpApiAsync(float timeoutSeconds)
        {
            try
            {
                using var req = UnityWebRequest.Get("https://ipwho.is/");
                req.timeout = Mathf.CeilToInt(timeoutSeconds);
                var op = req.SendWebRequest();
                while (!op.isDone)
                    await UniTask.Yield();

#if UNITY_2020_2_OR_NEWER
                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning("[Country] IP API network error: " + req.error);
                    return null;
                }
#else
                if (req.isNetworkError || req.isHttpError)
                {
                    Debug.LogWarning("[Country] IP API network error: " + req.error);
                    return null;
                }
#endif
                var json = req.downloadHandler.text;
                if (string.IsNullOrWhiteSpace(json)) return null;
                IpWhoResponse resp = null;
                try { resp = JsonUtility.FromJson<IpWhoResponse>(json); } catch { }
                if (resp != null && resp.success && IsValidIso2(resp.country_code))
                    return resp.country_code.ToUpperInvariant();
                Debug.LogWarning("[Country] IP API parse/invalid response: " + json);
                return null;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Country] IP API exception: " + e.Message);
                return null;
            }
        }


        /// <summary>
        /// Optional: refine country via GPS + reverse geocode (Android Geocoder) tanpa memaksa user setuju terlalu cepat.
        /// Hanya dipanggil jika ingin akurasi lebih tinggi. Timeout minimal agar tidak menggantung.
        /// </summary>
        // EnhanceWithGpsAsync dihapus: deteksi sudah langsung saat InitializeAsync.

        // ----- Detection Layers -------------------------------------------
        // GPS & reverse geocode code removed (API-only mode)

        // ------------------------------------------------------------------
        // One-shot location capture for ProfileController (stores into ProfileSave)
        // ------------------------------------------------------------------
#if BUMIMOBILE_PROFILE_SAVE
        public static UniTask CaptureLocationOnceAsync(ProfileSave save, float timeoutSeconds = 6f)
        {
            if (save == null) return UniTask.CompletedTask;
            save.LocationPermissionAsked = true;
            // In API-only mode we don't access GPS; just copy current CountryISO snapshot.
            save.LocationPermissionGranted = false; // we didn't ask
            if (IsValidIso2(CountryISO)) save.LastLocationCountryISO = CountryISO;
            return UniTask.CompletedTask;
        }
#endif

        // ---- Display Name Resolution ---------------------------------------
        static string ResolveDisplayName(string iso)
        {
            if (string.IsNullOrWhiteSpace(iso)) return "Unknown";
            if (!IsValidIso2(iso) || string.Equals(iso, "ZZ", StringComparison.OrdinalIgnoreCase)) return "Unknown";

            if (flagDb != null)
            {
                var name = flagDb.GetDisplayName(iso);
                if (!string.IsNullOrWhiteSpace(name)) return name;
            }

            return iso.ToUpperInvariant();
        }
    }
}
