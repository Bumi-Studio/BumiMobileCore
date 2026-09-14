using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.Networking;

namespace BumiMobile
{
    public static class CountryService
    {
        public static string CountryISO { get; private set; } = "ZZ";
        public static string CountryName { get; private set; } = "Unknown";
        public static bool IsInitialized { get; private set; }
        public static event Action OnCountryResolved;

        static CountryFlagDatabase flagDb;
        public static CountryFlagDatabase FlagDatabase => flagDb;

        public static void SetFlagDatabase(CountryFlagDatabase db)
        {
            flagDb = db;
            if (IsInitialized) CountryName = ResolveDisplayName(CountryISO);
        }

        public static Sprite GetFlagSprite(string isoOverride = null)
        {
            if (flagDb == null) return null;
            string iso = string.IsNullOrWhiteSpace(isoOverride) ? CountryISO : isoOverride.Trim().ToUpperInvariant();
            if (string.IsNullOrEmpty(iso) || iso == "ZZ" || iso.Length != 2) return null;
            return flagDb.GetSprite(iso);
        }

        const string PREF_KEY = "country.iso.cached";
        const float DEFAULT_IP_TIMEOUT = 5f;
        const float DEFAULT_GPS_TIMEOUT = 6f;

        public static async UniTask InitializeAsync(float ipTimeoutSeconds = DEFAULT_IP_TIMEOUT, float gpsTimeoutSeconds = DEFAULT_GPS_TIMEOUT)
        {
            if (IsInitialized) return;
            if (TryLoadCachedIso(out var cached))
            {
                CountryISO = cached;
                CountryName = ResolveDisplayName(CountryISO);
                IsInitialized = true;
                Debug.Log("[Country] Using cached ISO: " + CountryISO);
                OnCountryResolved?.Invoke();
                return;
            }

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

            Debug.LogWarning("[Country] IP API failed or invalid. Country remains ZZ.");
            CountryISO = "ZZ";
            CountryName = ResolveDisplayName(CountryISO);
            IsInitialized = true;
            OnCountryResolved?.Invoke();
        }

        public static async UniTask ForceRedetectAsync()
        {
            ClearCache();
            IsInitialized = false;
            CountryISO = "ZZ";
            CountryName = ResolveDisplayName(CountryISO);
            await InitializeAsync();
        }

        static bool IsValidIso2(string iso) => !string.IsNullOrEmpty(iso) && iso.Length == 2 && char.IsLetter(iso[0]) && char.IsLetter(iso[1]);

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

        [Serializable]
        class IpWhoResponse { public bool success; public string country_code; }

        static async UniTask<string> ResolveViaIpApiAsync(float timeoutSeconds)
        {
            try
            {
                using var req = UnityWebRequest.Get("https://ipwho.is/");
                req.timeout = Mathf.CeilToInt(timeoutSeconds);
                var op = req.SendWebRequest();
                while (!op.isDone) await UniTask.Yield();

#if UNITY_2020_2_OR_NEWER
                if (req.result != UnityWebRequest.Result.Success)
#else
                if (req.isNetworkError || req.isHttpError)
#endif
                {
                    Debug.LogWarning("[Country] IP API network error: " + req.error);
                    return null;
                }

                var json = req.downloadHandler.text;
                if (string.IsNullOrWhiteSpace(json)) return null;
                IpWhoResponse resp = null;
                try { resp = JsonUtility.FromJson<IpWhoResponse>(json); } catch { }
                if (resp != null && resp.success && IsValidIso2(resp.country_code)) return resp.country_code.ToUpperInvariant();
                Debug.LogWarning("[Country] IP API parse/invalid response: " + json);
                return null;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Country] IP API exception: " + e.Message);
                return null;
            }
        }

        static string ResolveDisplayName(string iso)
        {
            if (string.IsNullOrWhiteSpace(iso) || !IsValidIso2(iso) || string.Equals(iso, "ZZ", StringComparison.OrdinalIgnoreCase)) return "Unknown";
            if (flagDb != null)
            {
                var name = flagDb.GetDisplayName(iso);
                if (!string.IsNullOrWhiteSpace(name)) return name;
            }
            return iso.ToUpperInvariant();
        }
    }
}
