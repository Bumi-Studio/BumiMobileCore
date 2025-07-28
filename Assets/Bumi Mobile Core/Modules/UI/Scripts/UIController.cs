using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
 
namespace BumiMobile
{
    public class UIController : MonoBehaviour
    {
        private static UIController uiController;

        [SerializeField] FloatingCloud currencyCloud;
        [SerializeField] NotchSaveArea notchSaveArea;

        private static List<UIPage> pages;
        private static Dictionary<Type, UIPage> pagesLink = new Dictionary<Type, UIPage>();

        private static List<IPopupWindow> popupWindows;
        public static bool IsPopupOpened => !popupWindows.IsNullOrEmpty();

        private static bool isTablet;
        public static bool IsTablet => isTablet;

        private static Canvas mainCanvas;
        public static Canvas MainCanvas => mainCanvas;
        public static CanvasScaler CanvasScaler { get; private set; }

        private static Camera mainCamera;

        private static SimpleCallback localPageClosedCallback;

        public static event PageCallback PageOpened;
        public static event PageCallback PageClosed;

        public static event PopupWindowCallback PopupOpened;
        public static event PopupWindowCallback PopupClosed;

        public void Init()
        {
            uiController = this;

            mainCanvas = GetComponent<Canvas>();
            CanvasScaler = GetComponent<CanvasScaler>();

            isTablet = UIUtils.IsWideScreen(Camera.main);
            mainCamera = Camera.main;

            CanvasScaler.matchWidthOrHeight = isTablet ? 1 : 0;

            popupWindows = new List<IPopupWindow>();

            pages = new List<UIPage>();
            pagesLink = new Dictionary<Type, UIPage>();
            for (int i = 0; i < transform.childCount; i++)
            {
                UIPage uiPage = transform.GetChild(i).GetComponent<UIPage>();
                if(uiPage != null)
                {
                    uiPage.CacheComponents();

                    if(pagesLink.ContainsKey(uiPage.GetType()))
                    {
                        Debug.LogError($"[UI Controller] Page {uiPage.GetType()} is already added to the UIController. Please remove the duplicate object to resolve this issue.", uiPage);

                        continue;
                    }

                    pagesLink.Add(uiPage.GetType(), uiPage);

                    pages.Add(uiPage);
                }
            }

            // Initialize global overlay
            Overlay.Init(this);
        }

        public void InitPages()
        {
            // Refresh notch save area
            notchSaveArea.Init();

            // Initialize currency cloud
            currencyCloud.Init();

            for (int i = 0; i < pages.Count; i++)
            {
                pages[i].Init();
                pages[i].DisableCanvas();
            }
        }

        public static void ResetPages()
        {
            UIController controller = uiController;
            if (controller != null)
            {
                for (int i = 0; i < pages.Count; i++)
                {
                    if (pages[i].IsPageDisplayed)
                    {
                        pages[i].Unload();
                    }
                }
            }
        }

        public static void ShowPage<T>() where T : UIPage
        {
            Type pageType = typeof(T);
            UIPage page = pagesLink[pageType];
            if (!page.IsPageDisplayed)
            {
                page.PlayShowAnimation();
                page.EnableCanvas();
                page.GraphicRaycaster.enabled = true;
            }
        }

        public static void ShowPage(UIPage page)
        {
            if (!page.IsPageDisplayed)
            {
                page.PlayShowAnimation();
                page.EnableCanvas();
                page.GraphicRaycaster.enabled = true;
            }
        }

        public static void HidePage<T>(SimpleCallback onPageClosed = null)
        {
            Type pageType = typeof(T);
            UIPage page = pagesLink[pageType];
            if (page.IsPageDisplayed)
            {
                localPageClosedCallback = onPageClosed;

                page.GraphicRaycaster.enabled = false;
                page.PlayHideAnimation();
            }
            else
            {
                onPageClosed?.Invoke();
            }
        }

        public static void DisablePage<T>()
        {
            Type pageType = typeof(T);
            UIPage page = pagesLink[pageType];
            if (page.IsPageDisplayed)
            {
                page.DisableCanvas();

                OnPageClosed(page);
            }
        }

        public static bool IsDisplayed<T>() where T : UIPage
        {
            Type type = typeof(T);
            if (pagesLink.ContainsKey(type))
            {
                return pagesLink[type].IsPageDisplayed;
            }

            return false;
        }

        public static void OnPageClosed(UIPage page)
        {
            page.DisableCanvas();

            PageClosed?.Invoke(page, page.GetType());

            if (localPageClosedCallback != null)
            {
                localPageClosedCallback.Invoke();
                localPageClosedCallback = null;
            }
        }

        public static void OnPageOpened(UIPage page)
        {
            PageOpened?.Invoke(page, page.GetType());
        }

        public static void OnPopupWindowOpened(IPopupWindow popupWindow)
        {
            if(!popupWindows.Contains(popupWindow))
            {
                popupWindows.Add(popupWindow);

                PopupOpened?.Invoke(popupWindow, true);
            }
        }

        public static void OnPopupWindowClosed(IPopupWindow popupWindow)
        {
            if (popupWindows.Contains(popupWindow))
            {
                popupWindows.Remove(popupWindow);

                PopupClosed?.Invoke(popupWindow, false);
            }
        }

        public static T GetPage<T>() where T : UIPage
        {
            UIPage page;

            if (pagesLink.TryGetValue(typeof(T), out page))
                return (T)page;

            return null;
        }

        public static Vector3 FixUIElementToWorld(Transform target, Vector3 offset)
        {
            Vector3 targPos = target.transform.position + offset;
            Vector3 camForward = mainCamera.transform.forward;

            float distInFrontOfCamera = Vector3.Dot(targPos - (mainCamera.transform.position + camForward), camForward);
            if (distInFrontOfCamera < 0f)
            {
                targPos -= camForward * distInFrontOfCamera;
            }

            return RectTransformUtility.WorldToScreenPoint(mainCamera, targPos);
        }

        private void OnDestroy()
        {
            FloatingCloud.Clear();

            Overlay.Clear();
        }

        public delegate void PageCallback(UIPage page, Type pageType);
        public delegate void PopupWindowCallback(IPopupWindow popupWindow, bool state);
    }
}

// -----------------
// UI Controller v1.2.1
// -----------------

// Changelog
// v 1.2.1
// � Added Editor script that automatically configure CanvasScaler
// v 1.2
// � Added global overlay
// v 1.1
// � Added popup callbacks and methods to handle when a custom window is opened
// � RectTransform can be added to NotchSaveArea using NotchSaveArea.RegisterRectTransform method
// v 1.0
// � Basic logic