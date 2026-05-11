using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using BumiMobile.Leaderboard;

namespace BumiMobile
{
    public class UILeaderboard : UIPage
    {
        [Header("UI")]
        [SerializeField] private RectTransform panelRectTransform;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private GameObject buttonContainer;

        [Header("Buttons")]
        [SerializeField] private Button closeButton;
        [Header("Tabs")]
        [SerializeField] private UICountryButton countryButton;
        [SerializeField] private UIPlayerButton playerButton;
        [SerializeField] private UIRegionalButton regionalButton;
        [SerializeField] private UIGlobalButton globalButton;

        [Header("Avatar (Optional)")]
        [SerializeField] private Sprite fallbackAvatarSprite;

        [Header("Country Flags")]
        [SerializeField] private CountryFlagDatabase countryFlags; // for display name + flag mapping
        
        public Button RegionalButton => regionalButton.Button;
        public Button GlobalButton => globalButton.Button;
        public Button CountryButton => countryButton.Button;

        [Header("Scores")]
        [Tooltip("10 slot untuk Top 10 (urut dari #1 di index 0 sampai #10 di index 9)")]
        [SerializeField] private List<UIPlayerScore> topScores = new List<UIPlayerScore>();
        [Tooltip("Slot khusus 'You' (bukan bagian dari list), posisikan di bawah daftar")]
        [SerializeField] private UIPlayerScore selfScore;

        [Header("Scroll (Optional)")]
        [SerializeField] private ScrollRect scrollRect;

        [Header("Haptic Scroll (Simple)")]
        [SerializeField, Range(0.001f, 0.2f)] private float minDeltaNormalized = 0.02f;
        [SerializeField, Min(0)] private int hapticCooldownMs = 80;
        private Vector2 lastNormalized;
        private float lastPulseMs;

        private readonly Dictionary<LeaderboardType, List<LeaderboardEntry>> _topCache
            = new Dictionary<LeaderboardType, List<LeaderboardEntry>>();

        // No rank caching needed anymore

        private const int TOP_LIMIT = 10;
        private readonly HashSet<LeaderboardType> _refreshing = new HashSet<LeaderboardType>();

        // === NEW: track if we already did an initial fresh fetch this session ===
        private bool _didInitialFresh;

        public override void Init()
        {
            // Ensure leaderboard controller initialized before any fetches
            LeaderboardController.Init();
            closeButton.onClick.AddListener(OnCloseButtonClicked);
            backgroundImage.AddEvent(EventTriggerType.PointerDown, OnBackgroundClicked);

            // Tabs
            playerButton.Init((isSelected) => ShowPlayerInformation(LeaderboardType.Regional, /*fresh*/ false));
            countryButton.Init((isSelected) => ShowPlayerInformation(LeaderboardType.Country,  /*fresh*/ false));
            regionalButton.Init((isSelected) => ShowPlayerInformation(LeaderboardType.Regional,/*fresh*/ false));
            globalButton.Init((isSelected) => ShowPlayerInformation(LeaderboardType.Global,   /*fresh*/ false));
            // Listen refresh event dari controller (sudah di-marshal ke main thread di controller)
            LeaderboardController.OnLeaderboardUpdated += OnLeaderboardUpdated;

            // Haptic on scroll
            if (scrollRect != null)
            {
                lastNormalized = scrollRect.normalizedPosition;
                scrollRect.onValueChanged.AddListener(OnScrollChanged);
            }

            // Render something immediately (may be empty) to avoid blank UI feel
            ShowPlayerInformation(LeaderboardType.Regional, false);

            // Kick off async load flow (fire and forget) to populate
            LoadLeaderboardAsync().Forget();
        }

        private void OnDestroy()
        {
            LeaderboardController.OnLeaderboardUpdated -= OnLeaderboardUpdated;
            if (scrollRect != null)
                scrollRect.onValueChanged.RemoveListener(OnScrollChanged);
        }

        private async UniTaskVoid LoadLeaderboardAsync()
        {
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate); // ensure UI fully laid out
            buttonContainer.SetActive(true);
            try
            {
                // Pre-cache (cached fetch)
                await PrecacheAsync();
                if (!_didInitialFresh)
                {
                    await InitialFreshRefreshAsync(LeaderboardType.Regional);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[UILeaderboard] Load flow error: " + e.Message);
            }
        }

        private async UniTask PrecacheAsync()
        {
            var regionalTask = LeaderboardController.GetCachedAsync(LeaderboardType.Regional, TOP_LIMIT, false);
            var globalTask   = LeaderboardController.GetCachedAsync(LeaderboardType.Global,   TOP_LIMIT, false);
            var countryTask  = LeaderboardController.GetCachedAsync(LeaderboardType.Country,  TOP_LIMIT, false);

            var (regional, global, country) = await UniTask.WhenAll(
                regionalTask, globalTask, countryTask);

            _topCache[LeaderboardType.Regional] = regional ?? new List<LeaderboardEntry>();
            _topCache[LeaderboardType.Global]   = global   ?? new List<LeaderboardEntry>();
            _topCache[LeaderboardType.Country]  = country  ?? new List<LeaderboardEntry>();

            // no self rank cache

            ShowPlayerInformation(LeaderboardType.Regional, false);
        }

        private async UniTask InitialFreshRefreshAsync(LeaderboardType defaultTab)
        {
            _didInitialFresh = true;
            var regionalTask = LeaderboardController.GetCachedAsync(LeaderboardType.Regional, TOP_LIMIT, true);
            var globalTask   = LeaderboardController.GetCachedAsync(LeaderboardType.Global,   TOP_LIMIT, true);
            var countryTask  = LeaderboardController.GetCachedAsync(LeaderboardType.Country,  TOP_LIMIT, true);

            var (regional, global, country) = await UniTask.WhenAll(
                regionalTask, globalTask, countryTask);

            _topCache[LeaderboardType.Regional] = regional ?? new List<LeaderboardEntry>();
            _topCache[LeaderboardType.Global]   = global   ?? new List<LeaderboardEntry>();
            _topCache[LeaderboardType.Country]  = country  ?? new List<LeaderboardEntry>();

            // no self rank cache
            ShowPlayerInformation(defaultTab, false);
        }

        // === NEW: public pull-to-refresh you can call from UI ===
    public void PullToRefresh() => ForceFullRefreshAsync().Forget();

        private async UniTask ForceFullRefreshAsync()
        {
            var regionalTask = LeaderboardController.GetCachedAsync(Leaderboard.LeaderboardType.Regional, TOP_LIMIT, true);
            var globalTask   = LeaderboardController.GetCachedAsync(Leaderboard.LeaderboardType.Global,   TOP_LIMIT, true);
            var countryTask  = LeaderboardController.GetCachedAsync(Leaderboard.LeaderboardType.Country,  TOP_LIMIT, true);

            var (regional, global, country) = await UniTask.WhenAll(
                regionalTask, globalTask, countryTask);

            _topCache[LeaderboardType.Regional] = regional ?? new List<LeaderboardEntry>();
            _topCache[LeaderboardType.Global]   = global   ?? new List<LeaderboardEntry>();
            _topCache[LeaderboardType.Country]  = country  ?? new List<LeaderboardEntry>();
            // no self rank cache
            ShowPlayerInformation(LeaderboardType.Regional, false);
        }

        // ===================== EVENT ANTI-LOOP =====================
        private void OnLeaderboardUpdated(LeaderboardType type)
        {
            if (_refreshing.Contains(type)) return;
            RefreshTypeFromEventAsync(type).Forget();
        }
        private async UniTask RefreshTypeFromEventAsync(LeaderboardType type)
        {
            if (!_refreshing.Add(type)) return; // if already refreshing
            try
            {
                var topTask = LeaderboardController.GetCachedAsync(type, TOP_LIMIT, false);
                var topResult = await topTask;
                _topCache[type] = topResult ?? new List<LeaderboardEntry>();
                Show(type);
            }
            finally
            {
                _refreshing.Remove(type);
            }
        }
        // ===========================================================

        // === CHANGED: allow asking for a fresh fetch when switching tabs ===
        public void ShowPlayerInformation(LeaderboardType leaderboardType, bool fresh = false)
        {
            playerButton.OnSelected(leaderboardType == LeaderboardType.Regional || leaderboardType == LeaderboardType.Global);
            regionalButton.OnSelected(leaderboardType == LeaderboardType.Regional);
            countryButton.OnSelected(leaderboardType == LeaderboardType.Country);
            globalButton.OnSelected(leaderboardType == LeaderboardType.Global);

            if (fresh)
            {
                // Kick a one-off fresh fetch for this tab then render
                ShowFreshThenRenderAsync(leaderboardType).Forget();
            }
            else
            {
                Show(leaderboardType);
            }
        }
        private async UniTask ShowFreshThenRenderAsync(LeaderboardType leaderboardType)
        {
            var topTask = LeaderboardController.GetCachedAsync(leaderboardType, TOP_LIMIT, true);
            var topResult = await topTask;
            _topCache[leaderboardType] = topResult ?? new List<LeaderboardEntry>();
            Show(leaderboardType);
        }

        private void Show(LeaderboardType leaderboardType)
        {
            if (!_topCache.TryGetValue(leaderboardType, out var top) || top == null)
            {
                foreach (var s in topScores) s.gameObject.SetActive(false);
                if (selfScore != null) selfScore.gameObject.SetActive(false);
                Debug.Log($"[UIRankLevel] No cache yet for {leaderboardType}");
                return;
            }

            // Hide list items
            for (int i = 0; i < topScores.Count; i++)
                topScores[i].gameObject.SetActive(false);

            // Keep self row persistent on Global/Regional to avoid flicker; only reset for Country
            if (selfScore != null && leaderboardType == LeaderboardType.Country)
                selfScore.gameObject.SetActive(false);

            int shown = Mathf.Min(TOP_LIMIT, top.Count);
            if (shown == 0)
            {
                Debug.Log("[UILeaderboard] No entries to display (empty list). Consider showing a placeholder UI.");
            }
            string myUid = LeaderboardController.GetCurrentUserId();
            int countryRank = -1;
            LeaderboardEntry myCountryEntry = default;
            bool selfInTop = false;
            int selfIndex = -1;
            LeaderboardEntry selfTopEntry = default;

            // removed unused selfAppearsInTop flag
            for (int i = 0; i < shown; i++)
            {
                var entry = top[i];

                switch (leaderboardType)
                {
                    case LeaderboardType.Regional:
                    case LeaderboardType.Global:
                        {
                            bool isSelf = (!string.IsNullOrEmpty(myUid) && entry.userId == myUid);
                            // (self highlight handled directly; no aggregate flag needed)
                            if (isSelf) { selfInTop = true; selfIndex = i; selfTopEntry = entry; }
                            var avatarSprite = ResolveAvatarSprite(entry);

                            topScores[i].SetupInfo(
                                rank: i + 1,
                                playerName: entry.name,                 // player format
                                scoreText: entry.score.ToString(),
                                countryName: entry.country,
                                isSelf: isSelf,                         // highlights only if this row is the player
                                avatarSprite: avatarSprite,
                                useCountryFlag: false
                            );
                            break;
                        }

                    case LeaderboardType.Country:
                        {
                            // Country rows: show human-friendly country name, flag by ISO with robust fallback
                            string playerIso = SafeIso2(CountryService.CountryISO);
                            string entryIso = SafeIso2(entry.country);
                            bool isMyCountry = !string.IsNullOrEmpty(playerIso) && playerIso == entryIso;
                            if (isMyCountry) { countryRank = i + 1; myCountryEntry = entry; }
                            string displayName = entryIso;
                            if (countryFlags != null)
                            {
                                var n = countryFlags.GetDisplayName(entryIso);
                                if (!string.IsNullOrWhiteSpace(n)) displayName = n;
                            }
                            else if (!string.IsNullOrEmpty(playerIso) && playerIso == entryIso && !string.IsNullOrWhiteSpace(CountryService.CountryName))
                            {
                                displayName = CountryService.CountryName;
                            }

                            topScores[i].SetupInfo(
                                rank: i + 1,
                                playerName: displayName,
                                scoreText: entry.score.ToString(),
                                countryName: entryIso,
                                isSelf: isMyCountry,
                                avatarSprite: null,
                                useCountryFlag: true
                            );
                            break;
                        }
                }

                topScores[i].gameObject.SetActive(true);
            }

            // Self row
            if (selfScore != null)
            {
                if (leaderboardType == LeaderboardType.Country)
                {
                    // Self row for country list only if player's country present; if not, hide safely
                    string myIso = SafeIso2(CountryService.CountryISO);
                    if (!string.IsNullOrEmpty(myIso) && countryRank > 0)
                    {
                        string displayName = myIso;
                        if (countryFlags != null)
                        {
                            var dn = countryFlags.GetDisplayName(myIso);
                            if (!string.IsNullOrWhiteSpace(dn)) displayName = dn;
                        }
                        else if (!string.IsNullOrWhiteSpace(CountryService.CountryName))
                        {
                            displayName = CountryService.CountryName;
                        }
                        int scoreVal = myCountryEntry.score;
                        selfScore.SetupInfo(
                            rank: countryRank,
                            playerName: displayName,
                            scoreText: scoreVal.ToString(),
                            countryName: myIso,
                            isSelf: true,
                            avatarSprite: null,
                            useCountryFlag: true
                        );
                        selfScore.gameObject.SetActive(true);
                    }
                }
                else
                {
                    // Global/Regional self row behavior
                    if (selfInTop)
                    {
                        // If in top, still show a self row with the exact rank (cheap: from top list)
                        string iso = SafeIso2(selfTopEntry.country);
                        var avatarSprite = ResolveAvatarSprite(selfTopEntry);
                        selfScore.SetupInfo(
                            rank: selfIndex + 1,
                            playerName: string.IsNullOrEmpty(selfTopEntry.name) ? "You" : selfTopEntry.name,
                            scoreText: selfTopEntry.score.ToString(),
                            countryName: iso,
                            isSelf: true,
                            avatarSprite: avatarSprite,
                            useCountryFlag: false
                        );
                        selfScore.gameObject.SetActive(true);
                    }
                    else
                    {
                        // If not in top list, show a self row without rank by fetching own doc only
                        RenderSelfOutsideTopAsync().Forget();
                    }
                }
            }
        }

        // Fetch player's own doc and display a self row with rank hidden (rank=0)
        private async UniTask RenderSelfOutsideTopAsync()
        {
            try
            {
                var entry = await LeaderboardController.GetSelfEntryAsync();
                if (entry == null) return;

                var avatarSprite = ResolveAvatarSprite(entry);
                if (selfScore != null)
                {
                    selfScore.SetupInfo(
                        rank: 0, // UI should hide when <= 0
                        playerName: string.IsNullOrEmpty(entry.name) ? "You" : entry.name,
                        scoreText: entry.score.ToString(),
                        countryName: SafeIso2(entry.country),
                        isSelf: true,
                        avatarSprite: avatarSprite,
                        useCountryFlag: false
                    );
                    selfScore.gameObject.SetActive(true);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[UILeaderboard] RenderSelfOutsideTop failed: " + e.Message);
            }
        }

        // Helper to normalize ISO or return ZZ placeholder
        private Sprite ResolveAvatarSprite(LeaderboardEntry entry)
        {
            return fallbackAvatarSprite;
        }

        private static string SafeIso2(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "ZZ";
            var up = raw.Trim().ToUpperInvariant();
            if (up.Length != 2) return "ZZ";
            return up;
        }


        public void OnCloseButtonClicked()
        {
#if MODULE_HAPTIC
            Haptic.Play(Haptic.HAPTIC_LIGHT);
#endif
            AudioController.PlaySound(AudioController.buttonSound);
            UIController.HidePage<UILeaderboard>();
        }

        private void OnBackgroundClicked(PointerEventData data)
        {
            UIController.HidePage<UILeaderboard>();
        }

        public override void PlayShowAnimation()
        {
            panelRectTransform.anchoredPosition = Vector2.down * 2000f;
            panelRectTransform.DOAnchoredPosition(Vector2.zero, 0.3f).SetEasing(Ease.Type.SineOut);

            backgroundImage.SetAlpha(0f);

            // Render something immediately (cached). The coroutine will follow up with a fresh pass if needed.
            ShowPlayerInformation(LeaderboardType.Regional, false);

            backgroundImage.DOFade(0.3f, 0.3f).OnComplete(() =>
            {
                UIController.OnPageOpened(this);
            });
        }

        public override void PlayHideAnimation()
        {
            panelRectTransform.DOAnchoredPosition(Vector2.down * 2000f, 0.3f).SetEasing(Ease.Type.SineIn);
            backgroundImage.DOFade(0f, 0.3f).OnComplete(() =>
            {
                UIController.OnPageClosed(this);
            });
        }

        private void OnScrollChanged(Vector2 norm)
        {
#if MODULE_HAPTIC
            if (scrollRect == null) return;

            float delta = scrollRect.vertical
                ? Mathf.Abs(norm.y - lastNormalized.y)
                : Mathf.Abs(norm.x - lastNormalized.x);

            float nowMs = Time.realtimeSinceStartup * 1000f;

            if (delta >= minDeltaNormalized && (nowMs - lastPulseMs) >= hapticCooldownMs)
            {
                Haptic.Play(Haptic.HAPTIC_LIGHT);
                lastPulseMs = nowMs;
                lastNormalized = norm;
            }
#endif
        }
    }
}
