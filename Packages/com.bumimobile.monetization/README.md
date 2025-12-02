# Bumi Mobile Monetization

Unified ads + in-app purchase orchestration for Bumi Mobile projects. This package wraps the initialization pipeline, consent gating, provider abstractions, and helper APIs so individual games only wire the high-level settings.

## Features

- **Single settings asset** (`MonetizationSettings`) that embeds both `AdsSettings` and `IAPSettings`, including privacy links, debug/test devices, and enable/disable toggles.
- **Initializer module** (`MonetizationInitModule`) that plugs into the Core startup pipeline and bootstraps `Monetization`, `AdsManager`, and `IAPManager` in the correct order.
- **Ad provider abstraction**: `AdProviderHandler` implementations for AdMob, Unity Ads Legacy, LevelPlay, and a Dummy fallback. Each handles banners, interstitials, and rewarded videos with shared retry logic.
- **Consent + platform tasks**: optional UMP (GDPR) + IDFA `LoadingTask`s gate ad initialization until the required prompts finish; forced-ad persistence integrates with the Save module when `MODULE_SAVE` is present.
- **IAP wrapper layer** over Unity IAP that exposes product lookups by `ProductKeyType`, purchase events, restore helpers, and editor tooling (`IAPSettings`, `IAPItem`, `ProductData`).
- **Editor UX**: custom inspectors for Monetization/Ads/IAP settings, provider-specific containers (AdMob IDs, Unity Ads placements, LevelPlay keys), and convenience menu items to generate the nested assets.
- **Auto module defines**: the runtime asmdef now emits `MODULE_ADMOB`, `MODULE_LEVELPLAY`, and `MODULE_UNITYADS` as soon as their Unity packages are installed, keeping the relevant code paths enabled without manual define management.

## Requirements

- Unity **2021.3** or newer.
- `com.bumimobile.core` 0.1.1+ for the initializer, module registry, and helper utilities.
- Optional SDKs:
  - `com.google.ads.mobile` (Google Mobile Ads / AdMob).
  - `com.ironsource.unity` (ironSource LevelPlay).
  - `com.unity.ads` (Unity Ads, used by the legacy handler).
  - `com.unity.purchasing` if you plan to use IAPs (`MODULE_IAP`).

## Getting Started

1. **Create Monetization Settings**

   - `Assets ▸ Create ▸ Data ▸ Core ▸ Monetization Settings` generates a ScriptableObject that already contains embedded `Ads Settings` and `IAP Settings` sub-assets. Fill in privacy/terms URLs and any test devices.
   - In the Ads tab pick which `AdProvider` backs banners, interstitials, and rewarded videos. Configure provider containers (IDs, placements, consent options, LevelPlay banner sizes, etc.). Toggle UMP/IDFA if you want the built-in consent flow.
   - In the IAP tab define products (`IAPItem` entries) and map them to `ProductKeyType`s used in code.

2. **Wire the initializer**

   - Add a `MonetizationInitModule` asset to your `ProjectInitSettings` (via the Core initializer UI). Assign the Monetization Settings asset created above.
   - During startup the module calls `Monetization.Init(settings)`, `AdsManager.Init(settings)`, and `IAPManager.Init(settings)`. Optional UMP/IDFA loading tasks run before ad providers are touched.

3. **Optional: manual bootstrap**

```csharp
[SerializeField] private MonetizationSettings monetizationSettings;

IEnumerator Start()
{
    Monetization.Init(monetizationSettings);
    AdsManager.Init(monetizationSettings);
    IAPManager.Init(monetizationSettings);
    yield return null;
    AdsManager.TryToLoadFirstAds();
}
```

## Runtime Usage

- **Ads**

  - Request/show ads through `AdsManager`: `AdsManager.RequestInterstitial();`, `AdsManager.ShowInterstitial(OnInterstitialClosed);`, `AdsManager.TryToLoadFirstAds();`.
  - Subscribe to module events: `AdsManager.AdLoaded += OnAdLoaded;`, `AdsManager.AdProviderInitialized += OnProviderReady;`.
  - Force banner visibility: `AdsManager.ShowBanner();` / `AdsManager.HideBanner();`.

- **Purchasing**

```csharp
void Awake()
{
    IAPManager.PurchaseCompleted += OnPurchaseSuccess;
    IAPManager.PurchaseFailed += OnPurchaseFailed;
}

public void BuyNoAds()
{
    IAPManager.BuyProduct(ProductKeyType.NoAds);
}
```

- **Product data helpers**

```csharp
var gemPack = IAPManager.GetProductData(ProductKeyType.GemsPackLarge);
priceLabel.text = gemPack == null ? "--" : $"{gemPack.ISOCurrencyCode} {gemPack.Price}";
```

## Consent & Platform Nuances

- Enable **UMP** in `AdsSettings` to automatically show the Consent Management Platform. Debug geography and TFUA flags are exposed in the inspector. `UMPLoadingTask` blocks ad initialization until consent is resolved.
- Enable **IDFA** to request tracking authorization on iOS via `IDFALoadingTask`. Ads will only start after the tracking status is determined.
- When AdMob/Unity Ads/LevelPlay packages are installed, the asmdef emits `MODULE_ADMOB`, `MODULE_UNITYADS`, and `MODULE_LEVELPLAY` so the relevant handlers compile without extra defines.

## Troubleshooting

- `AdsManager` logs warnings when a provider is selected in `AdsSettings` but not installed or initialized—double-check the provider SDK and defines.
- If ads never load, make sure UMP/IDFA tasks finished (watch the GameLoading tasks list) or temporarily disable those toggles while debugging.
- IAP calls require the Unity Purchasing package; without it `MODULE_IAP` stays undefined and the dummy wrapper is used.
- Use the verbose logging flag on the Monetization settings asset to surface detailed provider events in the console.

## License

See the root `LICENSE` file for licensing details.
