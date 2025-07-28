namespace BumiMobile
{
    public enum PurchaseFailureReason
    {
        PurchasingUnavailable = 0,
        ExistingPurchasePending = 1,
        ProductUnavailable = 2,
        SignatureInvalid = 3,
        UserCancelled = 4,
        PaymentDeclined = 5,
        DuplicateTransaction = 6,
        Unknown = 7
    }
}