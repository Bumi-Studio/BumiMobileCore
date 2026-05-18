using System;

namespace BumiMobile
{
    public static class Core
    {
        [Obsolete("Monetization package has been removed. This method is a no-op.")]
        public static bool IsMonetizationActive() => false;

        [Obsolete("Monetization package has been removed. This method is a no-op.")]
        public static void RegisterMonetizationStatus(Func<bool> resolver) { }

        [Obsolete("Monetization package has been removed. This method is a no-op.")]
        public static void ClearMonetizationStatus() { }
    }
}
