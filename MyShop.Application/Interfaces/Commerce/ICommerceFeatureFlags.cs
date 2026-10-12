namespace MyShop.Application.Interfaces.Commerce
{
    /// <summary>
    /// Gate for NEW Commerce capabilities only. Does not control Modules:Commerce
    /// and must not be used to disable live cart/checkout/payment.
    /// </summary>
    public interface ICommerceFeatureFlags
    {
        bool IsEnabled(string featureKey);
    }
}
