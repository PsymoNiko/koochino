using MyShop.Application.Interfaces.Commerce;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Web.Services
{
    public class PaymentGatewayRegistry : IPaymentGatewayRegistry
    {
        private readonly IReadOnlyList<IPaymentGateway> _gateways;
        public PaymentGatewayRegistry(IEnumerable<IPaymentGateway> gateways) => _gateways = gateways.ToList();
        public IReadOnlyList<IPaymentGateway> GetAll() => _gateways;
        public IPaymentGateway Get(string provider) => _gateways.FirstOrDefault(x =>
            string.Equals(x.Provider, provider, StringComparison.OrdinalIgnoreCase));
    }

    public abstract class StubPaymentGateway : IPaymentGateway
    {
        public abstract string Provider { get; }

        public Task<(bool Ok, string Authority, string RedirectUrl, string Error)> StartPaymentAsync(
            long amountRial, string description, string callbackUrl, string orderNumber)
        {
            var authority = $"{Provider.ToUpperInvariant()}-MOCK-{Guid.NewGuid():N}";
            var separator = callbackUrl.Contains("?") ? "&" : "?";
            var url = $"{callbackUrl}{separator}Authority={Uri.EscapeDataString(authority)}&Status=OK&Gateway={Provider}";
            return Task.FromResult<(bool, string, string, string)>((true, authority, url, null));
        }

        public Task<(bool Ok, string RefId, string Error)> VerifyAsync(string authority, long amountRial)
        {
            var valid = !string.IsNullOrWhiteSpace(authority)
                && authority.StartsWith(Provider.ToUpperInvariant() + "-MOCK-", StringComparison.OrdinalIgnoreCase);
            return Task.FromResult<(bool, string, string)>(valid
                ? (true, "MOCK-REF-" + authority.Substring(authority.LastIndexOf('-') + 1), null)
                : (false, null, $"{Provider} هنوز به API واقعی متصل نشده است."));
        }
    }

    public class DigipayPaymentGateway : StubPaymentGateway { public override string Provider => "Digipay"; }
    public class SnappPayPaymentGateway : StubPaymentGateway { public override string Provider => "SnappPay"; }
}
