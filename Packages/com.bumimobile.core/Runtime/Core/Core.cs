using System;

namespace BumiMobile
{
    public static class Core
    {
        private static Func<bool> monetizationStatusResolver;

        public static bool IsMonetizationActive()
        {
            return monetizationStatusResolver?.Invoke() ?? false;
        }

        public static void RegisterMonetizationStatus(Func<bool> resolver)
        {
            monetizationStatusResolver = resolver;
        }

        public static void ClearMonetizationStatus()
        {
            monetizationStatusResolver = null;
        }
    }
}
