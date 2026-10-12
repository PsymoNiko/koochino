using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyShop.Application.Interfaces;
using MyShop.Application.Interfaces.Commerce;
using MyShop.Application.Interfaces.Products;
using MyShop.Application.Interfaces.Security;
using MyShop.Core.Interfaces;
using MyShop.Core.Models;
using MyShop.Core.Models.Commerce;
using MyShop.Core.Models.Products;

namespace MyShop.Application.Services.Commerce
{
    public class MockPaymentGateway : IPaymentGateway
    {
        public string Provider => "Mock";

        public Task<(bool Ok, string Authority, string RedirectUrl, string Error)> StartPaymentAsync(
            long amountRial, string description, string callbackUrl, string orderNumber)
        {
            var authority = "MOCK-" + Guid.NewGuid().ToString("N").Substring(0, 12).ToUpperInvariant();
            var sep = callbackUrl.Contains("?") ? "&" : "?";
            var url = $"{callbackUrl}{sep}Authority={Uri.EscapeDataString(authority)}&Status=OK";
            return Task.FromResult<(bool, string, string, string)>((true, authority, url, null));
        }

        public Task<(bool Ok, string RefId, string Error)> VerifyAsync(string authority, long amountRial)
        {
            if (string.IsNullOrWhiteSpace(authority) || !authority.StartsWith("MOCK-", StringComparison.OrdinalIgnoreCase))
                return Task.FromResult<(bool, string, string)>((false, null, "Authority نامعتبر"));
            return Task.FromResult<(bool, string, string)>((true, "REF-" + authority.Substring(5), null));
        }
    }

    public class CommerceService : ICommerceService
    {
        private readonly ICommerceCatalogRepository _catalog;
        private readonly ICommerceCartRepository _carts;
        private readonly ICommerceOrderRepository _orders;
        private readonly IEnumerable<IPaymentGateway> _gateways;
        private readonly IProductService _products;
        private readonly IOrderPurchaseNotifier _notifier;
        private readonly ICurrentTenant _tenant;
        private readonly ILogger<CommerceService> _log;
        private readonly ICommercePricingService _pricing;
        private readonly ICommerceProductAccessRepository _access;
        private readonly IStoreService _stores;
        private readonly ICommerceFeatureFlags _flags;
        private readonly ICommerceVariationCartRepository _varCart;
        private readonly ICommerceVariationRepository _variations;
        private readonly ICommerceStockReservationRepository _reservations;
        private readonly ICommercePromotionService _promotions;
        private readonly ICommerceTaxRepository _taxRates;
        private readonly ICommerceStoreShippingRepository _storeShipping;
        private readonly IDataScopeResolver _scope;

        public CommerceService(
            ICommerceCatalogRepository catalog,
            ICommerceCartRepository carts,
            ICommerceOrderRepository orders,
            IEnumerable<IPaymentGateway> gateways,
            IProductService products = null,
            IOrderPurchaseNotifier notifier = null,
            ICurrentTenant tenant = null,
            ILogger<CommerceService> log = null,
            ICommercePricingService pricing = null,
            ICommerceProductAccessRepository access = null,
            IStoreService stores = null,
            ICommerceFeatureFlags flags = null,
            ICommerceVariationCartRepository varCart = null,
            ICommerceVariationRepository variations = null,
            ICommerceStockReservationRepository reservations = null,
            ICommercePromotionService promotions = null,
            ICommerceTaxRepository taxRates = null,
            ICommerceStoreShippingRepository storeShipping = null,
            IDataScopeResolver scope = null)
        {
            _catalog = catalog;
            _carts = carts;
            _orders = orders;
            _gateways = gateways ?? Enumerable.Empty<IPaymentGateway>();
            _products = products;
            _notifier = notifier;
            _tenant = tenant;
            _log = log;
            _pricing = pricing;
            _access = access;
            _stores = stores;
            _flags = flags;
            _varCart = varCart;
            _variations = variations;
            _reservations = reservations;
            _promotions = promotions;
            _taxRates = taxRates;
            _storeShipping = storeShipping;
            _scope = scope;
        }

        public Task<List<CommerceShippingMethod>> ListShippingAsync(int? siteId, bool activeOnly = false)
            => _catalog.ListShippingAsync(siteId, activeOnly);
        public Task<int> SaveShippingAsync(CommerceShippingMethod m) => _catalog.UpsertShippingAsync(m);
        public Task DeleteShippingAsync(int id) => _catalog.DeleteShippingAsync(id);

        public Task<List<CommercePaymentMethod>> ListPaymentAsync(int? siteId, bool activeOnly = false)
            => _catalog.ListPaymentAsync(siteId, activeOnly);
        public Task<int> SavePaymentAsync(CommercePaymentMethod m) => _catalog.UpsertPaymentAsync(m);
        public Task DeletePaymentAsync(int id) => _catalog.DeletePaymentAsync(id);

        public Task<List<CommerceDeliverySlot>> ListSlotsAsync(int? siteId, bool activeOnly = false)
            => _catalog.ListSlotsAsync(siteId, activeOnly);
        public Task<int> SaveSlotAsync(CommerceDeliverySlot m) => _catalog.UpsertSlotAsync(m);
        public Task DeleteSlotAsync(int id) => _catalog.DeleteSlotAsync(id);

        public Task<CommerceCart> GetCartAsync(int? siteId, int? userId, string guestKey)
            => _carts.GetOrCreateAsync(siteId, userId, guestKey);

        public async Task<CommerceCart> AddToCartAsync(int? siteId, int? userId, string guestKey, int productId, string title, long unitPrice, int quantity)
        {
            if (_products == null)
                throw new InvalidOperationException("سرویس محصول در دسترس نیست.");

            var p = await _products.GetByIdAsync(productId);
            if (p == null || !p.IsActive) throw new InvalidOperationException("محصول یافت نشد یا غیرفعال است.");
            if (_access != null && !await _access.ProductAccessibleAsync(productId, siteId))
                throw new InvalidOperationException("محصول یافت نشد.");

            var quote = _pricing != null
                ? _pricing.QuoteProduct(p)
                : new CommercePriceQuote { UnitPrice = CommercePricingService.ToRial(p.Price) };
            if (quote.UnitPrice <= 0)
                throw new InvalidOperationException("قیمت فروش معتبر نیست.");

            var cart0 = await _carts.GetOrCreateAsync(siteId, userId, guestKey);
            var existing = cart0.Items?.FirstOrDefault(x => x.ProductId == productId)?.Quantity ?? 0;
            var qtyAdd = Math.Max(1, quantity);
            if (existing + qtyAdd > p.Quantity)
                throw new InvalidOperationException($"موجودی «{p.Name}» کافی نیست (موجود: {p.Quantity}).");

            var qty = existing + qtyAdd;
            await _carts.SetItemAsync(cart0.Id, productId, p.Name, quote.UnitPrice, qty);
            return await _carts.GetWithItemsAsync(cart0.Id);
        }

        public Task RemoveFromCartAsync(int cartId, int productId) => _carts.RemoveItemAsync(cartId, productId);
        public Task ClearCartAsync(int cartId) => _carts.ClearAsync(cartId);

        public async Task<CommerceCheckoutResult> CheckoutAsync(int? siteId, int userId, string guestKey, CommerceCheckoutRequest request, string callbackBaseUrl)
        {
            if (userId <= 0) return CommerceCheckoutResult.Fail("برای ثبت سفارش وارد شوید.");
            if (request == null) return CommerceCheckoutResult.Fail("اطلاعات چک‌اوت ناقص است.");
            if (string.IsNullOrWhiteSpace(request.ShippingAddress))
                return CommerceCheckoutResult.Fail("آدرس ارسال الزامی است.");

            var cart = await _carts.GetOrCreateAsync(siteId, userId, guestKey);
            var varOn = VariationCartOn();
            var varLines = await LoadVariationLinesAsync(cart.Id, varOn);
            if ((cart?.Items == null || cart.Items.Count == 0) && varLines.Count == 0)
                return CommerceCheckoutResult.Fail("سبد خرید خالی است.");

            if (varOn && varLines.Count > 0)
            {
                var hasVarCol = _orders != null && await _orders.OrderItemColumnExistsAsync("VariationId");
                if (!hasVarCol)
                    return CommerceCheckoutResult.Fail("سبد تنوع روی این پایگاه داده آماده نیست. ابتدا اسکریپت Phase 5 را اعمال کنید.");
            }

            List<OrderItem> items;
            string stockError;
            if (varOn)
            {
                var built = await BuildMergedOrderItemsAsync(cart.Items, varLines);
                if (!built.Ok) return CommerceCheckoutResult.Fail(built.Error);
                items = built.Items;
                stockError = null;
            }
            else
            {
                stockError = await ValidateStockAsync(cart.Items);
                if (stockError != null) return CommerceCheckoutResult.Fail(stockError);
                items = cart.Items.Select(i => new OrderItem
                {
                    ProductId = i.ProductId,
                    Title = i.Title,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    LineTotal = i.LineTotal
                }).ToList();
                if (MultiVendorOn())
                {
                    var stamp = await StampLineStoreIdsAsync(items);
                    if (stamp != null) return CommerceCheckoutResult.Fail(stamp);
                }
            }

            var shipping = await _catalog.GetShippingAsync(request.ShippingMethodId);
            if (shipping == null || !shipping.IsActive)
                return CommerceCheckoutResult.Fail("روش ارسال معتبر نیست.");

            var payment = await _catalog.GetPaymentAsync(request.PaymentMethodId);
            if (payment == null || !payment.IsActive)
                return CommerceCheckoutResult.Fail("روش پرداخت معتبر نیست.");

            var subtotal = items.Sum(i => i.LineTotal);
            if (shipping.MinOrderAmount.HasValue && subtotal < shipping.MinOrderAmount.Value)
                return CommerceCheckoutResult.Fail($"حداقل مبلغ سفارش برای این ارسال {shipping.MinOrderAmount:N0} ریال است.");

            var storeIds = DistinctStoreIds(items);
            List<CommerceStoreShippingRate> rates = new();
            if (MultiVendorOn() && _storeShipping != null)
            {
                try { rates = await _storeShipping.ListByMethodAsync(shipping.Id) ?? new List<CommerceStoreShippingRate>(); }
                catch { rates = new List<CommerceStoreShippingRate>(); }
            }
            var shippingFee = CommerceTotals.QuoteShippingFee(shipping, subtotal, storeIds, rates);

            int? headerStoreId;
            if (MultiVendorOn())
            {
                var hasStoreCol = _orders != null && await _orders.OrderItemColumnExistsAsync("StoreId");
                if (!hasStoreCol)
                    return CommerceCheckoutResult.Fail("چندفروشنده روی این پایگاه داده آماده نیست. ابتدا اسکریپت Phase 5 یا 8 را اعمال کنید.");
                var resolved = ResolveCheckoutStores(items, multiVendor: true);
                if (!resolved.Ok) return CommerceCheckoutResult.Fail(resolved.Error);
                headerStoreId = resolved.StoreId;
            }
            else if (varOn)
            {
                var storeCheck = SingleStoreFromLines(items);
                if (!storeCheck.Ok) return CommerceCheckoutResult.Fail(storeCheck.Error);
                headerStoreId = storeCheck.StoreId;
            }
            else
                headerStoreId = request.StoreId ?? cart.StoreId;

            long discount = 0;
            int? promoId = null;
            if (PromotionsOn())
            {
                try
                {
                    var quote = await _promotions.QuoteAsync(siteId, headerStoreId, subtotal, request.PromoCode);
                    discount = quote?.DiscountAmount ?? 0;
                    promoId = quote?.PromotionId;
                }
                catch (InvalidOperationException ex)
                {
                    return CommerceCheckoutResult.Fail(ex.Message);
                }
                catch
                {
                    return CommerceCheckoutResult.Fail("پروموشن روی این پایگاه داده آماده نیست. ابتدا اسکریپت Phase 7 را اعمال کنید.");
                }
            }

            var total = subtotal;
            long tax = 0;
            if (TaxOn())
            {
                var hasTaxCol = _orders != null && await _orders.OrderColumnExistsAsync("TaxAmount");
                if (!hasTaxCol)
                    return CommerceCheckoutResult.Fail("مالیات روی این پایگاه داده آماده نیست. ابتدا اسکریپت Phase 9 را اعمال کنید.");
                try
                {
                    var rate = await _taxRates.GetActiveForSiteAsync(siteId);
                    tax = CommerceTotals.QuoteTax(rate, subtotal, discount, shippingFee);
                }
                catch
                {
                    return CommerceCheckoutResult.Fail("مالیات روی این پایگاه داده آماده نیست. ابتدا اسکریپت Phase 9 را اعمال کنید.");
                }
            }

            var final = total - discount + shippingFee + tax;
            if (final < 0) final = 0;

            var isOnline = string.Equals(payment.Code, CommercePaymentCodes.OnlineGateway, StringComparison.OrdinalIgnoreCase);
            var order = new CommerceOrderDetail
            {
                UserId = userId,
                StoreId = headerStoreId,
                SiteId = siteId,
                TotalAmount = total,
                DiscountAmount = discount,
                ShippingFee = shippingFee,
                TaxAmount = tax,
                FinalAmount = final,
                ShippingMethodId = shipping.Id,
                PaymentMethodId = payment.Id,
                DeliverySlotId = request.DeliverySlotId,
                DeliveryDate = request.DeliveryDate,
                ShippingAddress = request.ShippingAddress.Trim(),
                Notes = request.Notes,
                FulfillmentStatus = isOnline ? CommerceFulfillmentStatus.AwaitingPayment : CommerceFulfillmentStatus.Processing,
                PaymentStatus = isOnline ? CommercePaymentStatus.Pending : CommercePaymentStatus.Unpaid,
                IsCommerce = true
            };

            if (ReservationOn())
            {
                try
                {
                    await _reservations.ExpireStaleAsync();
                    var holdError = await _reservations.TryReserveLinesAsync(
                        cart.Id, items, DateTime.Now.AddMinutes(CommerceConstants.ReservationTtlMinutes));
                    if (holdError != null)
                        return CommerceCheckoutResult.Fail(holdError);
                }
                catch
                {
                    return CommerceCheckoutResult.Fail("رزرو موجودی روی این پایگاه داده آماده نیست. ابتدا اسکریپت Phase 6 را اعمال کنید.");
                }
            }

            int orderId;
            try
            {
                orderId = await _orders.CreateCommerceOrderAsync(order, items, userId);
                if (ReservationOn())
                    await _reservations.BindOrderAsync(cart.Id, orderId);
                if (promoId.HasValue && promoId.Value > 0 && discount > 0)
                {
                    try { await _promotions.RedeemAsync(promoId.Value, orderId, userId, discount); }
                    catch { /* usage count is best-effort; order already has DiscountAmount */ }
                }
            }
            catch
            {
                if (ReservationOn())
                {
                    try { await _reservations.ReleaseByCartAsync(cart.Id); } catch { }
                }
                throw;
            }

            await _carts.ClearAsync(cart.Id);
            if (varOn && _varCart != null)
            {
                try { await _varCart.ClearAsync(cart.Id); } catch { /* table may be missing */ }
            }

            if (!isOnline)
            {
                await ApplyStockForOrderAsync(orderId, items);
                await NotifyAsync(orderId);
            }

            var result = CommerceCheckoutResult.Ok("سفارش ثبت شد.");
            result.OrderId = orderId;
            result.OrderNumber = (await _orders.GetDetailAsync(orderId))?.OrderNumber;

            if (!isOnline)
                return result;

            var provider = string.IsNullOrWhiteSpace(payment.GatewayProvider) ? "Mock" : payment.GatewayProvider;
            var gateway = _gateways.FirstOrDefault(g =>
                string.Equals(g.Provider, provider, StringComparison.OrdinalIgnoreCase))
                ?? _gateways.FirstOrDefault(g => g.Provider == "Mock");

            if (gateway == null)
                return CommerceCheckoutResult.Fail("درگاه پرداخت پیکربندی نشده است.");

            var callback = (callbackBaseUrl ?? "").TrimEnd('/') + "/commerce/payment/callback";
            var start = await gateway.StartPaymentAsync(final, "سفارش " + result.OrderNumber, callback, result.OrderNumber);
            if (!start.Ok)
                return CommerceCheckoutResult.Fail(start.Error ?? "شروع پرداخت ناموفق");

            await _orders.InsertPaymentAsync(new CommercePayment
            {
                OrderId = orderId,
                PaymentMethodId = payment.Id,
                Amount = final,
                Provider = gateway.Provider,
                Authority = start.Authority,
                Status = 1
            });

            _log?.LogInformation("PaymentStarted authority={Authority} orderId={OrderId} amount={Amount}", start.Authority, orderId, final);

            result.NeedsOnlinePayment = true;
            result.PaymentRedirectUrl = start.RedirectUrl;
            result.Authority = start.Authority;
            result.Message = "در حال انتقال به درگاه پرداخت...";
            return result;
        }

        public async Task<CommerceCheckoutResult> CompleteOnlinePaymentAsync(string authority, string statusQuery)
        {
            if (string.IsNullOrWhiteSpace(authority))
                return CommerceCheckoutResult.Fail("Authority خالی است.");

            var pay = await _orders.GetPaymentByAuthorityAsync(authority);
            if (pay == null) return CommerceCheckoutResult.Fail("تراکنش یافت نشد.");

            if (!string.Equals(pay.Authority, authority, StringComparison.OrdinalIgnoreCase))
            {
                _log?.LogWarning("PaymentCompletionConflict authority={Authority} reason=authority-mismatch", authority);
                return CommerceCheckoutResult.Fail("Authority نامعتبر است.");
            }

            var order = await _orders.GetDetailAsync(pay.OrderId);
            if (order == null) return CommerceCheckoutResult.Fail("سفارش یافت نشد.");

            if (pay.Amount <= 0)
            {
                await SafeAttempt(pay, CommercePaymentAttemptTypes.Rejected, "invalid-amount", null);
                return CommerceCheckoutResult.Fail("مبلغ پرداخت نامعتبر است.");
            }

            if (_tenant?.HasSite == true && order.SiteId.HasValue && order.SiteId.Value != _tenant.SiteId)
            {
                await SafeAttempt(pay, CommercePaymentAttemptTypes.Rejected, "site-mismatch", null);
                _log?.LogWarning("PaymentCompletionConflict authority={Authority} reason=site", authority);
                return CommerceCheckoutResult.Fail("پرداخت به این سایت تعلق ندارد.");
            }

            if (pay.FinalizedState == CommercePaymentFinalizedState.Done)
            {
                await SafeAttempt(pay, CommercePaymentAttemptTypes.AlreadyProcessed, statusQuery, pay.RefId);
                _log?.LogInformation("PaymentAlreadyProcessed authority={Authority} orderId={OrderId}", authority, order.Id);
                return AlreadyProcessed(order);
            }

            if (pay.Status == CommercePaymentStatus.Paid)
            {
                await SafeAttempt(pay, CommercePaymentAttemptTypes.Callback, "paid-resume", pay.RefId);
                return await FinalizeOnceAsync(pay, order);
            }

            if (pay.FinalizedState == CommercePaymentFinalizedState.InProgress)
            {
                await SafeAttempt(pay, CommercePaymentAttemptTypes.Conflict, "in-progress", pay.RefId);
                _log?.LogInformation("PaymentCompletionConflict authority={Authority} reason=in-progress", authority);
                return AlreadyProcessed(order, "پرداخت در حال نهایی‌سازی است.");
            }

            if (order.FulfillmentStatus == CommerceFulfillmentStatus.Cancelled
                || order.FulfillmentStatus == CommerceFulfillmentStatus.Refunded
                || pay.Status == CommercePaymentStatus.Refunded)
            {
                await SafeAttempt(pay, CommercePaymentAttemptTypes.Rejected, "cancelled-or-refunded", null);
                await SafeReleaseReservationsAsync(order.Id);
                return CommerceCheckoutResult.Fail("این سفارش قابل تکمیل پرداخت نیست.");
            }

            var userOk = string.Equals(statusQuery, "OK", StringComparison.OrdinalIgnoreCase)
                || string.Equals(statusQuery, "1", StringComparison.OrdinalIgnoreCase);

            if (!userOk)
            {
                _log?.LogInformation("PaymentFailed authority={Authority} reason=user-cancel", authority);
                await _orders.TryMarkPaymentFailedIfNotPaidAsync(pay.Id, "user-cancel");
                await _orders.UpdatePaymentStatusAsync(order.Id, CommercePaymentStatus.Failed, CommerceFulfillmentStatus.Cancelled);
                await SafeReleaseReservationsAsync(order.Id);
                await SafeAttempt(pay, CommercePaymentAttemptTypes.UserCancel, statusQuery, null);
                return CommerceCheckoutResult.Fail("پرداخت توسط کاربر لغو شد.");
            }

            var gateway = _gateways.FirstOrDefault(g =>
                string.Equals(g.Provider, pay.Provider, StringComparison.OrdinalIgnoreCase))
                ?? _gateways.FirstOrDefault(g => g.Provider == "Mock");
            if (gateway == null) return CommerceCheckoutResult.Fail("درگاه یافت نشد.");

            _log?.LogInformation("PaymentVerificationStarted authority={Authority} amount={Amount}", authority, pay.Amount);
            var verify = await gateway.VerifyAsync(authority, pay.Amount);
            if (!verify.Ok)
            {
                _log?.LogInformation("PaymentFailed authority={Authority} reason=verify", authority);
                await _orders.TryMarkPaymentFailedIfNotPaidAsync(pay.Id, Truncate(verify.Error));
                await _orders.UpdatePaymentStatusAsync(order.Id, CommercePaymentStatus.Failed);
                await SafeReleaseReservationsAsync(order.Id);
                await SafeAttempt(pay, CommercePaymentAttemptTypes.VerifyFailed, Truncate(verify.Error), null);
                return CommerceCheckoutResult.Fail(verify.Error ?? "تایید پرداخت ناموفق");
            }

            _log?.LogInformation("PaymentVerified authority={Authority} ref={Ref}", authority, verify.RefId);
            var markedPaid = await _orders.TryMarkPaymentPaidAsync(pay.Id, verify.RefId, Truncate(verify.RefId));
            pay = await _orders.GetPaymentByIdAsync(pay.Id) ?? pay;
            if (!markedPaid && pay.Status == CommercePaymentStatus.Paid)
            {
                await SafeAttempt(pay, CommercePaymentAttemptTypes.AlreadyProcessed, "paid-race", pay.RefId);
                _log?.LogInformation("PaymentAlreadyProcessed authority={Authority} reason=paid-race", authority);
                return await FinalizeOnceAsync(pay, order);
            }
            if (!markedPaid)
                return CommerceCheckoutResult.Fail("وضعیت پرداخت قابل تکمیل نیست.");

            return await FinalizeOnceAsync(pay, order);
        }

        async Task<CommerceCheckoutResult> FinalizeOnceAsync(CommercePayment pay, CommerceOrderDetail order)
        {
            if (pay.FinalizedState == CommercePaymentFinalizedState.Done)
                return AlreadyProcessed(order);

            var began = await _orders.TryBeginPaymentFinalizationAsync(pay.Id);
            if (!began)
            {
                pay = await _orders.GetPaymentByIdAsync(pay.Id) ?? pay;
                if (pay.FinalizedState == CommercePaymentFinalizedState.Done)
                    return AlreadyProcessed(order);
                _log?.LogInformation("PaymentCompletionConflict authority={Authority} reason=finalize-race", pay.Authority);
                return AlreadyProcessed(order, "پرداخت در حال نهایی‌سازی است.");
            }

            try
            {
                await _orders.UpdatePaymentStatusAsync(order.Id, CommercePaymentStatus.Paid, CommerceFulfillmentStatus.Paid);
                await _orders.UpdateFulfillmentAsync(order.Id, CommerceFulfillmentStatus.Processing, null, "پرداخت تایید شد — آماده‌سازی", null);

                order = await _orders.GetDetailAsync(order.Id);
                await ApplyStockForOrderAsync(order.Id, order.Items);
                try { await NotifyAsync(order.Id); }
                catch (Exception nex)
                {
                    _log?.LogWarning(nex, "PaymentCompletionConflict authority={Authority} reason=notify", pay.Authority);
                }
                await _orders.MarkPaymentFinalizationDoneAsync(pay.Id);
                await SafeAttempt(pay, CommercePaymentAttemptTypes.Completed, "ok", pay.RefId);
                _log?.LogInformation("PaymentVerified authority={Authority} finalized orderId={OrderId}", pay.Authority, order.Id);

                return new CommerceCheckoutResult
                {
                    Success = true,
                    Message = "پرداخت با موفقیت انجام شد.",
                    OrderId = order.Id,
                    OrderNumber = order.OrderNumber
                };
            }
            catch (Exception ex)
            {
                await _orders.AbandonPaymentFinalizationAsync(pay.Id);
                _log?.LogWarning(ex, "PaymentCompletionConflict authority={Authority} reason=finalize-error", pay.Authority);
                return CommerceCheckoutResult.Fail("پرداخت ثبت شد اما نهایی‌سازی کامل نشد. دوباره تلاش کنید.");
            }
        }

        static CommerceCheckoutResult AlreadyProcessed(CommerceOrderDetail order, string message = "پرداخت قبلاً ثبت شده است.")
        {
            return new CommerceCheckoutResult
            {
                Success = true,
                Message = message,
                OrderId = order.Id,
                OrderNumber = order.OrderNumber
            };
        }

        async Task SafeAttempt(CommercePayment pay, string type, string status, string reference)
        {
            try
            {
                await _orders.InsertPaymentAttemptAsync(new CommercePaymentAttempt
                {
                    PaymentId = pay.Id,
                    AttemptType = type,
                    Authority = pay.Authority,
                    ReferenceId = reference,
                    Status = Truncate(status, 32),
                    ResponseCode = Truncate(status, 400)
                });
            }
            catch { /* attempts table may not be applied yet */ }
        }

        static string Truncate(string s, int max = 400)
        {
            if (string.IsNullOrEmpty(s)) return s;
            s = s.Replace("token", "***", StringComparison.OrdinalIgnoreCase)
                 .Replace("secret", "***", StringComparison.OrdinalIgnoreCase)
                 .Replace("password", "***", StringComparison.OrdinalIgnoreCase);
            return s.Length <= max ? s : s.Substring(0, max);
        }

        public Task<CommerceOrderDetail> GetOrderAsync(int orderId) => _orders.GetDetailAsync(orderId);
        public Task<List<CommerceOrderDetail>> ListOrdersAsync(int? siteId = null, int? userId = null, int? storeId = null)
            => _orders.ListAsync(siteId, userId, storeId);

        public async Task<List<CommerceOrderDetail>> ListOrdersForUserAsync(int actorUserId, int? siteId = null, int? userId = null, int? storeId = null)
        {
            if (_scope == null || actorUserId <= 0)
                return await _orders.ListAsync(siteId, userId, storeId);
            var query = await _scope.BuildQueryAsync(actorUserId, "Commerce.View", siteId);
            return await _orders.ListScopedAsync(query, siteId, userId, storeId);
        }

        public async Task<List<CommerceOrderDetail>> ListSellerOrdersAsync(int sellerUserId)
        {
            var ids = await OwnedStoreIdsAsync(sellerUserId);
            return await _orders.ListForStoreIdsAsync(ids);
        }

        public Task SetFulfillmentAsync(int orderId, byte status, string trackingCode, string note, int? changedByUserId)
            => _orders.UpdateFulfillmentAsync(orderId, status, trackingCode, note, changedByUserId);

        public async Task SetSellerFulfillmentAsync(int sellerUserId, int orderId, byte status, string trackingCode, string note, int? changedByUserId)
        {
            await EnsureSellerOwnsOrderAsync(sellerUserId, orderId);
            await _orders.UpdateFulfillmentAsync(orderId, status, trackingCode, note, changedByUserId);
        }

        async Task<IReadOnlyList<int>> OwnedStoreIdsAsync(int sellerUserId)
        {
            if (sellerUserId <= 0 || _stores == null)
                return Array.Empty<int>();
            var owned = await _stores.GetByUserIdAsync(sellerUserId) ?? new List<Store>();
            return owned
                .Select(s => s.StoreID != 0 ? s.StoreID : s.Id)
                .Where(id => id > 0)
                .Distinct()
                .ToList();
        }

        async Task EnsureSellerOwnsOrderAsync(int sellerUserId, int orderId)
        {
            var ids = (await OwnedStoreIdsAsync(sellerUserId)).ToHashSet();
            if (ids.Count == 0)
                throw new InvalidOperationException("فروشگاهی برای این کاربر یافت نشد.");
            var order = await _orders.GetDetailAsync(orderId);
            if (order == null)
                throw new InvalidOperationException("سفارش یافت نشد.");
            if (!SellerCanSee(order, ids))
                throw new InvalidOperationException("این سفارش به فروشگاه شما تعلق ندارد.");
        }

        public static bool SellerCanSee(CommerceOrderDetail order, ISet<int> ownedStoreIds)
        {
            if (order == null || ownedStoreIds == null || ownedStoreIds.Count == 0)
                return false;
            var lineStores = order.Items?
                .Where(i => i.StoreId.HasValue && i.StoreId.Value > 0)
                .Select(i => i.StoreId.Value)
                .ToList() ?? new List<int>();
            if (lineStores.Count > 0)
                return lineStores.Any(ownedStoreIds.Contains);
            return order.StoreId.HasValue && order.StoreId.Value > 0 && ownedStoreIds.Contains(order.StoreId.Value);
        }

        public Task<CommerceDashboardKpis> GetDashboardKpisAsync(int? siteId)
            => _orders.GetDashboardKpisAsync(siteId);

        bool VariationCartOn() =>
            _flags != null && _flags.IsEnabled(CommerceConstants.Features.VariationCart);

        bool ReservationOn() =>
            _flags != null && _flags.IsEnabled(CommerceConstants.Features.Reservation) && _reservations != null;

        bool PromotionsOn() =>
            _flags != null && _flags.IsEnabled(CommerceConstants.Features.Promotions) && _promotions != null;

        bool MultiVendorOn() =>
            _flags != null && _flags.IsEnabled(CommerceConstants.Features.MultiVendor);

        bool TaxOn() =>
            _flags != null && _flags.IsEnabled(CommerceConstants.Features.Tax) && _taxRates != null;

        async Task ApplyStockForOrderAsync(int orderId, IEnumerable<OrderItem> items)
        {
            if (!ReservationOn())
            {
                await DecrementStockAsync(items);
                return;
            }

            bool committed = false, active = false;
            try
            {
                committed = await _reservations.HasCommittedAsync(orderId);
                active = await _reservations.HasActiveAsync(orderId);
            }
            catch
            {
                await DecrementStockAsync(items);
                return;
            }

            if (committed)
                return;
            await DecrementStockAsync(items);
            if (active)
            {
                try { await _reservations.MarkCommittedAsync(orderId); } catch { }
            }
        }

        async Task SafeReleaseReservationsAsync(int orderId)
        {
            if (!ReservationOn()) return;
            try { await _reservations.ReleaseByOrderAsync(orderId); } catch { }
        }

        async Task<List<CommerceVariationCartItem>> LoadVariationLinesAsync(int cartId, bool varOn)
        {
            if (!varOn || _varCart == null) return new List<CommerceVariationCartItem>();
            try { return await _varCart.ListAsync(cartId) ?? new List<CommerceVariationCartItem>(); }
            catch { return new List<CommerceVariationCartItem>(); }
        }

        async Task<(bool Ok, string Error, List<OrderItem> Items)> BuildMergedOrderItemsAsync(
            List<CommerceCartItem> simple, List<CommerceVariationCartItem> vars)
        {
            var items = new List<OrderItem>();
            foreach (var i in simple ?? new List<CommerceCartItem>())
            {
                var p = _products == null ? null : await _products.GetByIdAsync(i.ProductId);
                if (p == null || !p.IsActive)
                    return (false, $"محصول «{i.Title}» یافت نشد.", null);
                if (p.Quantity < i.Quantity)
                    return (false, $"موجودی «{p.Name}» کافی نیست (موجود: {p.Quantity}، درخواستی: {i.Quantity}).", null);
                var quote = _pricing != null ? _pricing.QuoteProduct(p) : new CommercePriceQuote { UnitPrice = i.UnitPrice };
                items.Add(new OrderItem
                {
                    ProductId = p.Id,
                    StoreId = p.StoreId,
                    VariationId = null,
                    Sku = p.Sku,
                    Title = p.Name,
                    Quantity = i.Quantity,
                    UnitPrice = quote.UnitPrice,
                    DiscountAmount = 0,
                    LineTotal = quote.UnitPrice * i.Quantity
                });
            }
            foreach (var v in vars ?? new List<CommerceVariationCartItem>())
            {
                if (_variations == null)
                    return (false, "سرویس تنوع در دسترس نیست.", null);
                var variation = await _variations.GetAsync(v.VariationId);
                if (variation == null || !variation.IsActive)
                    return (false, "تنوع یافت نشد یا غیرفعال است.", null);
                if (variation.Stock < v.Quantity)
                    return (false, $"موجودی تنوع کافی نیست (موجود: {variation.Stock}).", null);
                var p = _products == null ? null : await _products.GetByIdAsync(variation.ProductId);
                if (p == null || !p.IsActive)
                    return (false, "محصول یافت نشد یا غیرفعال است.", null);
                var quote = _pricing != null
                    ? _pricing.QuoteVariation(p, variation)
                    : new CommercePriceQuote { UnitPrice = v.UnitPriceSnapshot };
                var title = p.Name + (string.IsNullOrWhiteSpace(variation.Sku) ? "" : " / " + variation.Sku);
                items.Add(new OrderItem
                {
                    ProductId = p.Id,
                    StoreId = p.StoreId,
                    VariationId = variation.Id,
                    Sku = variation.Sku ?? p.Sku,
                    Title = title,
                    Quantity = v.Quantity,
                    UnitPrice = quote.UnitPrice,
                    DiscountAmount = 0,
                    LineTotal = quote.UnitPrice * v.Quantity
                });
            }
            return (true, null, items);
        }

        public static List<int> DistinctStoreIds(IEnumerable<OrderItem> items) =>
            (items ?? Enumerable.Empty<OrderItem>())
                .Select(i => i.StoreId)
                .Where(id => id.HasValue && id.Value > 0)
                .Select(id => id.Value)
                .Distinct()
                .ToList();

        public static (bool Ok, string Error, int? StoreId) SingleStoreFromLines(IEnumerable<OrderItem> items)
        {
            var ids = DistinctStoreIds(items);
            if (ids.Count == 0)
                return (false, "فروشگاه خطوط سفارش مشخص نیست.", null);
            if (ids.Count > 1)
                return (false, "در این مرحله فقط خرید از یک فروشگاه در هر سفارش مجاز است.", null);
            return (true, null, ids[0]);
        }

        /// <summary>
        /// Header StoreId is compatibility only. Mixed vendors → null header; ownership stays on lines.
        /// </summary>
        public static (bool Ok, string Error, int? StoreId) ResolveCheckoutStores(IEnumerable<OrderItem> items, bool multiVendor)
        {
            var ids = DistinctStoreIds(items);
            if (ids.Count == 0)
                return (false, "فروشگاه خطوط سفارش مشخص نیست.", null);
            if (!multiVendor && ids.Count > 1)
                return (false, "در این مرحله فقط خرید از یک فروشگاه در هر سفارش مجاز است.", null);
            return (true, null, ids.Count == 1 ? ids[0] : (int?)null);
        }

        async Task<string> StampLineStoreIdsAsync(List<OrderItem> items)
        {
            if (_products == null) return "سرویس محصول در دسترس نیست.";
            foreach (var i in items ?? new List<OrderItem>())
            {
                if (i.StoreId.HasValue && i.StoreId.Value > 0) continue;
                if (!i.ProductId.HasValue || i.ProductId.Value <= 0)
                    return "فروشگاه خطوط سفارش مشخص نیست.";
                var p = await _products.GetByIdAsync(i.ProductId.Value);
                if (p == null || !p.IsActive)
                    return $"محصول «{i.Title}» یافت نشد.";
                if (!p.StoreId.HasValue || p.StoreId.Value <= 0)
                    return "فروشگاه محصول مشخص نیست.";
                i.StoreId = p.StoreId;
            }
            return null;
        }

        async Task<string> ValidateStockAsync(IEnumerable<CommerceCartItem> items)
        {
            if (_products == null) return null;
            foreach (var i in items)
            {
                var p = await _products.GetByIdAsync(i.ProductId);
                if (p == null) return $"محصول «{i.Title}» یافت نشد.";
                if (p.Quantity < i.Quantity)
                    return $"موجودی «{p.Name}» کافی نیست (موجود: {p.Quantity}، درخواستی: {i.Quantity}).";
            }
            return null;
        }

        async Task DecrementStockAsync(IEnumerable<OrderItem> items)
        {
            if (items == null) return;
            foreach (var i in items)
            {
                if (i.VariationId.HasValue && i.VariationId.Value > 0)
                {
                    if (_variations != null)
                        await _variations.DecrementStockAsync(i.VariationId.Value, i.Quantity);
                    continue;
                }
                if (_products == null) continue;
                if (!i.ProductId.HasValue || i.ProductId <= 0) continue;
                var p = await _products.GetByIdAsync(i.ProductId.Value);
                if (p == null) continue;
                var newQty = Math.Max(0, p.Quantity - i.Quantity);
                await _products.UpdateAsync(new UpdateProductDto
                {
                    Id = p.Id,
                    CompanyId = p.CompanyId,
                    StoreId = p.StoreId,
                    CategoryId = p.CategoryId,
                    UnitId = p.UnitId,
                    Name = p.Name,
                    Slug = p.Slug,
                    Description = p.Description,
                    ShortDescription = p.ShortDescription,
                    Price = p.Price,
                    CompareAtPrice = p.CompareAtPrice,
                    Cost = p.Cost,
                    Sku = p.Sku,
                    Barcode = p.Barcode,
                    Quantity = newQty,
                    IsActive = p.IsActive,
                    IsFeatured = p.IsFeatured,
                    ImageUrl = p.ImageUrl,
                    SortOrder = p.SortOrder
                });
            }
        }

        async Task NotifyAsync(int orderId)
        {
            if (_notifier == null) return;
            try
            {
                var d = await _orders.GetDetailAsync(orderId);
                if (d == null) return;
                await _notifier.NotifyNewOrderAsync(new Order
                {
                    Id = d.Id,
                    OrderNumber = d.OrderNumber,
                    UserId = d.UserId,
                    StoreId = d.StoreId,
                    OrderType = 0,
                    TotalAmount = d.TotalAmount,
                    DiscountAmount = d.DiscountAmount,
                    FinalAmount = d.FinalAmount,
                    OrderDate = d.OrderDate,
                    Status = 0,
                    StoreName = d.StoreName,
                    CustomerName = d.CustomerName,
                    Notes = "Commerce: " + (d.ShippingAddress ?? "")
                });
            }
            catch { }
        }
    }

    public class UserAddressService : IUserAddressService
    {
        private readonly IUserAddressRepository _repo;
        public UserAddressService(IUserAddressRepository repo) => _repo = repo;
        public Task<List<UserAddress>> ListAsync(int userId) => _repo.ListByUserAsync(userId);
        public Task<int> SaveAsync(UserAddress address) => _repo.UpsertAsync(address);
        public Task DeleteAsync(int id, int userId) => _repo.DeleteAsync(id, userId);
        public Task SetDefaultAsync(int id, int userId) => _repo.SetDefaultAsync(id, userId);
    }
}
