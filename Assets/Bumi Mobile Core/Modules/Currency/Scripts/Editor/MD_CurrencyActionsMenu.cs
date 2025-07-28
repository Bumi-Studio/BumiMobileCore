using UnityEngine;
using UnityEditor;

namespace BumiMobile
{
    public static class CurrencyActionsMenu
    {
        [MenuItem("Actions/Lots of Money", priority = 21)]
        private static void LotsOfMoney()
        {
            CurrencyController.Set(CurrencyType.Coins, 2000000);
        }

        [MenuItem("Actions/Lots of Money", true)]
        private static bool LotsOfMoneyValidation()
        {
            return Application.isPlaying;
        }

        [MenuItem("Actions/No Money", priority = 22)]
        private static void NoMoney()
        {
            CurrencyController.Set(CurrencyType.Coins, 0);
        }

        [MenuItem("Actions/No Money", true)]
        private static bool NoMoneyValidation()
        {
            return Application.isPlaying;
        }
    }
}