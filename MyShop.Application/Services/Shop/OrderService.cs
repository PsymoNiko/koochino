using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyShop.Application.Interfaces;
using MyShop.Application.Interfaces.Loyalty;
using MyShop.Application.Interfaces.Security;
using MyShop.Core.Helpers;
using MyShop.Core.Interfaces;
using MyShop.Core.Models;

namespace MyShop.Application.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepo;
        private readonly ICouponRepository _couponRepo;
        private readonly IChanceBoxRepository _chanceBoxRepo;
        private readonly IOrderPurchaseNotifier _notifier;
        private readonly ICurrentTenant _tenant;
        private readonly MyShop.Application.Interfaces.Finance.IStoreBillingCommissionService _billing;
        private readonly MyShop.Application.Interfaces.Finance.IStoreVoucherWalletRepository _voucher;
        private readonly ICustomerPurchaseRewardService _purchaseReward;
        private readonly ILoyaltyRoadmapRepository _customerWallet;
        private readonly IDataScopeResolver _scope;

        public OrderService(
            IOrderRepository orderRepo,
            ICouponRepository couponRepo,
            IChanceBoxRepository chanceBoxRepo,
            IOrderPurchaseNotifier notifier = null,
            ICurrentTenant tenant = null,
            MyShop.Application.Interfaces.Finance.IStoreBillingCommissionService billing = null,
            MyShop.Application.Interfaces.Finance.IStoreVoucherWalletRepository voucher = null,
            ICustomerPurchaseRewardService purchaseReward = null,
            ILoyaltyRoadmapRepository customerWallet = null,
            IDataScopeResolver scope = null)
        {
            _orderRepo = orderRepo;
            _couponRepo = couponRepo;
            _chanceBoxRepo = chanceBoxRepo;
            _notifier = notifier;
            _tenant = tenant;
            _billing = billing;
            _voucher = voucher;
            _purchaseReward = purchaseReward;
            _customerWallet = customerWallet;
            _scope = scope;
        }

        void ApplySite(Order order)
        {
            if (order == null) return;
            if (!order.SiteId.HasValue)
                order.SiteId = SiteScope.CurrentSiteId(_tenant);
        }

        async Task<long> ClampWalletAsync(int userId, long requested, long payableBeforeWallet)
        {
            if (requested <= 0 || payableBeforeWallet <= 0) return 0;
            long bal = 0;
            if (_customerWallet != null)
            {
                try { bal = (long)Math.Floor(await _customerWallet.GetWalletBalanceAsync(userId)); }
                catch { bal = 0; }
            }
            var max = Math.Min(requested, Math.Min(bal, payableBeforeWallet));
            return max > 0 ? max : 0;
        }

        async Task<(bool ok, string message)> AfterInsertWalletAsync(Order order, long walletAmount)
        {
            if (walletAmount <= 0 || _purchaseReward == null) return (true, "");
            order.WalletAmountApplied = walletAmount;
            var r = await _purchaseReward.ApplyWalletDebitForOrderAsync(order.Id, order.UserId, walletAmount);
            return r;
        }

        public async Task<(bool Success, string Message, int OrderId)> CreateOrderWithCouponAsync(int userId, int couponUsageId, int? storeId, long totalAmount, long discountAmount, long walletAmountApplied = 0)
        {
            try
            {
                var orderNumber = await _orderRepo.GetNextOrderNumberAsync();
                if (string.IsNullOrEmpty(orderNumber))
                    return (false, "خطا در تولید شماره سفارش.", 0);

                var afterDiscount = totalAmount - discountAmount;
                if (afterDiscount < 0) afterDiscount = 0;
                var wallet = await ClampWalletAsync(userId, walletAmountApplied, afterDiscount);
                var finalAmount = afterDiscount - wallet;
                if (finalAmount < 0) finalAmount = 0;

                var order = new Order
                {
                    OrderNumber = orderNumber,
                    UserId = userId,
                    StoreId = storeId,
                    OrderType = 1,
                    SaleChannel = 0,
                    TotalAmount = totalAmount,
                    DiscountAmount = discountAmount,
                    FinalAmount = finalAmount,
                    WalletAmountApplied = wallet > 0 ? wallet : null,
                    CouponUsageId = couponUsageId,
                    ChanceBoxCodeUsageId = null,
                    OrderDate = DateTime.Now,
                    Status = 0
                };
                ApplySite(order);

                var orderId = await _orderRepo.InsertAsync(order);
                if (orderId <= 0)
                    return (false, "خطا در ثبت سفارش.", 0);

                order.Id = orderId;
                await _couponRepo.SetCouponUsageOrderIdAsync(couponUsageId, orderId);
                var (wok, wmsg) = await AfterInsertWalletAsync(order, wallet);
                if (!wok)
                    return (false, string.IsNullOrEmpty(wmsg) ? "خطا در کسر کیف پول." : wmsg, orderId);

                await TryNotifyAsync(order);
                return (true, $"سفارش {orderNumber} ثبت شد و در انتظار تایید مغازه‌دار/ادمین است.", orderId);
            }
            catch (Exception ex)
            {
                return (false, "خطا در ثبت سفارش: " + ex.Message, 0);
            }
        }

        public async Task<(bool Success, string Message, int OrderId)> CreateOrderWithChanceBoxAsync(int userId, int chanceBoxCodeUsageId, int storeId, long purchaseAmount, long walletAmountApplied = 0)
        {
            try
            {
                var orderNumber = await _orderRepo.GetNextOrderNumberAsync();
                if (string.IsNullOrEmpty(orderNumber))
                    return (false, "خطا در تولید شماره سفارش.", 0);

                var wallet = await ClampWalletAsync(userId, walletAmountApplied, purchaseAmount);
                var finalAmount = purchaseAmount - wallet;
                if (finalAmount < 0) finalAmount = 0;

                var order = new Order
                {
                    OrderNumber = orderNumber,
                    UserId = userId,
                    StoreId = storeId,
                    OrderType = 2,
                    SaleChannel = 0,
                    TotalAmount = purchaseAmount,
                    DiscountAmount = 0,
                    FinalAmount = finalAmount,
                    WalletAmountApplied = wallet > 0 ? wallet : null,
                    CouponUsageId = null,
                    ChanceBoxCodeUsageId = chanceBoxCodeUsageId,
                    OrderDate = DateTime.Now,
                    Status = 0
                };
                ApplySite(order);

                var orderId = await _orderRepo.InsertAsync(order);
                if (orderId <= 0)
                    return (false, "خطا در ثبت سفارش.", 0);

                order.Id = orderId;
                await _chanceBoxRepo.SetChanceBoxCodeUsageOrderIdAsync(chanceBoxCodeUsageId, orderId);
                var (wok, wmsg) = await AfterInsertWalletAsync(order, wallet);
                if (!wok)
                    return (false, string.IsNullOrEmpty(wmsg) ? "خطا در کسر کیف پول." : wmsg, orderId);

                await TryNotifyAsync(order);
                return (true, $"سفارش {orderNumber} ثبت شد و در انتظار تایید مغازه‌دار/ادمین است.", orderId);
            }
            catch (Exception ex)
            {
                return (false, "خطا در ثبت سفارش: " + ex.Message, 0);
            }
        }

        public async Task<(bool Success, string Message, int OrderId)> CreateOrderNormalAsync(int userId, int storeId, long totalAmount, string? notes = null, long walletAmountApplied = 0)
        {
            try
            {
                var orderNumber = await _orderRepo.GetNextOrderNumberAsync();
                if (string.IsNullOrEmpty(orderNumber))
                    return (false, "خطا در تولید شماره سفارش.", 0);

                var wallet = await ClampWalletAsync(userId, walletAmountApplied, totalAmount);
                var finalAmount = totalAmount - wallet;
                if (finalAmount < 0) finalAmount = 0;

                var order = new Order
                {
                    OrderNumber = orderNumber,
                    UserId = userId,
                    StoreId = storeId,
                    OrderType = 0,
                    SaleChannel = 0,
                    TotalAmount = totalAmount,
                    DiscountAmount = 0,
                    FinalAmount = finalAmount,
                    WalletAmountApplied = wallet > 0 ? wallet : null,
                    CouponUsageId = null,
                    ChanceBoxCodeUsageId = null,
                    OrderDate = DateTime.Now,
                    Status = 0,
                    Notes = notes
                };
                ApplySite(order);

                var orderId = await _orderRepo.InsertAsync(order);
                if (orderId <= 0)
                    return (false, "خطا در ثبت سفارش.", 0);

                order.Id = orderId;
                var (wok, wmsg) = await AfterInsertWalletAsync(order, wallet);
                if (!wok)
                    return (false, string.IsNullOrEmpty(wmsg) ? "خطا در کسر کیف پول." : wmsg, orderId);

                await TryNotifyAsync(order);
                return (true, $"سفارش {orderNumber} ثبت شد و در انتظار تایید مغازه‌دار/ادمین است.", orderId);
            }
            catch (Exception ex)
            {
                return (false, "خطا در ثبت سفارش: " + ex.Message, 0);
            }
        }

        private async Task TryNotifyAsync(Order order)
        {
            if (_notifier == null || order == null) return;
            try { await _notifier.NotifyNewOrderAsync(order); }
            catch { /* اعلان نباید ثبت سفارش را خراب کند */ }
        }

        public async Task<bool> UpdateStatusAsync(int orderId, byte status)
        {
            var ok = await _orderRepo.UpdateStatusAsync(orderId, status);
            if (!ok) return false;
            try
            {
                if (status == 1)
                {
                    if (_billing != null) await _billing.TryCalculateForOrderAsync(orderId);
                    await TryRedeemVoucherAsync(orderId);
                    if (_purchaseReward != null) await _purchaseReward.TryCreditAsync(orderId);
                }
                else if (status == 2)
                {
                    if (_billing != null) await _billing.TryCancelForOrderAsync(orderId);
                    if (_purchaseReward != null) await _purchaseReward.TryReverseAsync(orderId);
                }
            }
            catch { }
            return true;
        }

        private async Task TryRedeemVoucherAsync(int orderId)
        {
            if (_voucher == null) return;
            try
            {
                var order = await _orderRepo.GetByIdAsync(orderId);
                if (order == null || order.OrderType != 1 || !order.StoreId.HasValue || !order.CouponUsageId.HasValue)
                    return;
                if (order.DiscountAmount <= 0) return;
                var usages = await _couponRepo.GetUsagesByUserIdAsync(order.UserId);
                var usage = usages?.Find(u => u.Id == order.CouponUsageId.Value);
                if (usage == null) return;
                var coupon = await _couponRepo.GetByIdAsync(usage.CoponID);
                if (coupon == null) return;
                await _voucher.RedeemSettleAsync(
                    coupon.StoreID,
                    order.StoreId.Value,
                    order.DiscountAmount,
                    usage.Id,
                    order.Id,
                    coupon.CoponID);
            }
            catch { }
        }

        public Task<Order?> GetByIdAsync(int orderId)
        {
            return _orderRepo.GetByIdAsync(orderId);
        }

        public Task<List<Order>> GetListAsync(DateTime? fromDate, DateTime? toDate, byte? orderType, int? storeId, int? userId, byte? status, int pageSize, int pageIndex)
        {
            return _orderRepo.GetListAsync(fromDate, toDate, orderType, storeId, userId, status, pageSize, pageIndex);
        }

        public Task<int> CountAsync(DateTime? fromDate, DateTime? toDate, byte? orderType, int? storeId, int? userId, byte? status)
        {
            return _orderRepo.CountAsync(fromDate, toDate, orderType, storeId, userId, status);
        }

        public Task<List<Order>> GetPendingOrdersAsync(int? storeId = null)
        {
            return _orderRepo.GetPendingAsync(storeId);
        }

        public async Task<List<Order>> GetPendingOrdersForUserAsync(int userId, int? storeId = null, int? siteId = null)
        {
            if (_scope == null || userId <= 0)
                return await _orderRepo.GetPendingAsync(storeId);
            var query = await _scope.BuildQueryAsync(userId, "Stores.View", siteId);
            return await _orderRepo.GetPendingScopedAsync(query, storeId);
        }

        public async Task<List<Order>> GetListForUserAsync(int userId, DateTime? fromDate, DateTime? toDate, byte? orderType, int? storeId, int? filterUserId, byte? status, int pageSize, int pageIndex, int? siteId = null)
        {
            if (_scope == null || userId <= 0)
                return await _orderRepo.GetListAsync(fromDate, toDate, orderType, storeId, filterUserId, status, pageSize, pageIndex);
            var query = await _scope.BuildQueryAsync(userId, "Stores.View", siteId);
            return await _orderRepo.GetListScopedAsync(query, fromDate, toDate, orderType, storeId, filterUserId, status, pageSize, pageIndex);
        }

        public async Task<int> CountForUserAsync(int userId, DateTime? fromDate, DateTime? toDate, byte? orderType, int? storeId, int? filterUserId, byte? status, int? siteId = null)
        {
            if (_scope == null || userId <= 0)
                return await _orderRepo.CountAsync(fromDate, toDate, orderType, storeId, filterUserId, status);
            var query = await _scope.BuildQueryAsync(userId, "Stores.View", siteId);
            return await _orderRepo.CountScopedAsync(query, fromDate, toDate, orderType, storeId, filterUserId, status);
        }

        public Task<List<Order>> GetListByStoreOwnerAsync(int storeOwnerUserId, DateTime? fromDate, DateTime? toDate, byte? orderType, byte? status, string? searchType, string? searchText, int pageSize, int pageIndex)
        {
            return _orderRepo.GetListByStoreOwnerAsync(storeOwnerUserId, fromDate, toDate, orderType, status, searchType, searchText, pageSize, pageIndex);
        }
    }
}
