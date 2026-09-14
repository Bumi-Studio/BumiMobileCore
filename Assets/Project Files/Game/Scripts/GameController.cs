using BumiMobile;
using UnityEngine;

public class GameController : MonoBehaviour
{
    private static GameController instance;

    [SerializeField] private  UIController uiController;

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            if (uiController != null)
            {
                uiController.Init();
                uiController.InitPages();
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        UIController.ShowPage<UILeaderboard>();
    }
}
