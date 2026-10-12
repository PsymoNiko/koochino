using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyShop.Application.Interfaces;
using MyShop.Core.Models;
using MyShop.Core.Models.Dto;

namespace MyShop.Application.Services
{
    public class ChanceBoxService : IChanceBoxService
    {
        private readonly IChanceBoxRepository _repository;

        public ChanceBoxService(IChanceBoxRepository repository)
        {
            _repository = repository;
        }

        public async Task<string> CreateCodeAsync(int storeOwnerUserId, int storeId, long minPurchaseAmount, string description, string prizeDescription)
        {
            var lastCode = await _repository.GetLastForStoreAsync(storeId);
            int nextSequence = 1;

            if (lastCode != null && !string.IsNullOrWhiteSpace(lastCode.Code) && lastCode.Code.Length >= 9)
            {
                var seqPart = lastCode.Code.Substring(lastCode.Code.Length - 5);
                if (int.TryParse(seqPart, out int lastSeq))
                {
                    nextSequence = lastSeq + 1;
                }
            }

            var newCode = new ChanceBoxCode
            {
                StoreId = storeId,
                StoreOwnerUserId = storeOwnerUserId,
                Code = $"{storeId:D4}{nextSequence:D5}",
                MinPurchaseAmount = minPurchaseAmount,
                Description = description ?? string.Empty,
                PrizeDescription = prizeDescription ?? string.Empty,
                CreatedAt = DateTime.Now,
                IsUsed = false
            };

            await _repository.CreateCodeAsync(newCode);
            return newCode.Code;
        }

        public async Task<ChanceBoxLookupResult> LookupCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return new ChanceBoxLookupResult();

            var existing = await _repository.GetByCodeAsync(code.Trim());
            if (existing == null)
            {
                return new ChanceBoxLookupResult
                {
                    Found = false,
                    Message = "کد جعبه شانس معتبر نیست."
                };
            }

            if (existing.IsUsed)
            {
                return new ChanceBoxLookupResult
                {
                    Found = true,
                    IsUsed = true,
                    StoreId = existing.StoreId,
                    Message = "این کد قبلاً استفاده شده است."
                };
            }

            return new ChanceBoxLookupResult
            {
                Found = true,
                IsUsed = false,
                MinPurchaseAmount = existing.MinPurchaseAmount,
                StoreId = existing.StoreId,
                PrizeDescription = existing.PrizeDescription ?? string.Empty,
                Description = existing.Description ?? string.Empty
            };
        }

        public async Task<UseChanceBoxResult> UseCodeAsync(int customerUserId, int storeId, long purchaseAmount, string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return new UseChanceBoxResult { Success = false, Message = "کد جعبه شانس را وارد کنید." };

            var existing = await _repository.GetByCodeAsync(code.Trim());
            if (existing == null)
            {
                return new UseChanceBoxResult
                {
                    Success = false,
                    Message = "کد جعبه شانس معتبر نیست."
                };
            }

            if (existing.StoreId != storeId)
            {
                return new UseChanceBoxResult
                {
                    Success = false,
                    Message = "این کد مربوط به این فروشگاه نیست."
                };
            }

            if (existing.IsUsed)
            {
                return new UseChanceBoxResult
                {
                    Success = false,
                    Message = "این کد قبلاً استفاده شده است."
                };
            }

            if (purchaseAmount < existing.MinPurchaseAmount)
            {
                return new UseChanceBoxResult
                {
                    Success = false,
                    Message = $"مبلغ خرید شما کمتر از حداقل مبلغ لازم ({existing.MinPurchaseAmount:N0} تومان) است."
                };
            }

            var (ok, chanceBoxCodeUsageId) = await _repository.MarkUsedAsync(existing.ChanceBoxCodeId, customerUserId, purchaseAmount);
            if (!ok)
            {
                return new UseChanceBoxResult
                {
                    Success = false,
                    Message = "خطا در ثبت استفاده از کد."
                };
            }

            return new UseChanceBoxResult
            {
                Success = true,
                Message = "کد شما با موفقیت ثبت شد. شانس شما در قرعه لحاظ شد.",
                ChanceBoxCodeUsageId = chanceBoxCodeUsageId,
                StoreId = existing.StoreId
            };
        }

        public Task<List<ChanceBoxCode>> GetCodesForStoreOwnerAsync(int storeOwnerUserId)
        {
            return _repository.GetByStoreOwnerUserIdAsync(storeOwnerUserId);
        }

        public Task<List<ChanceBoxCodeUsage>> GetUsagesForStoreOwnerAsync(int storeOwnerUserId)
        {
            return _repository.GetUsagesByStoreOwnerUserIdAsync(storeOwnerUserId);
        }

        public Task<List<AdminChanceBoxStoreDto>> GetAdminStoresWithChanceBoxAsync(string? search)
        {
            return _repository.GetAdminStoresWithChanceBoxAsync(search);
        }

        public Task<List<ChanceBoxUsageDetailDto>> GetUsagesByStoreIdForAdminAsync(int storeId)
        {
            return _repository.GetUsagesByStoreIdAsync(storeId);
        }
    }
}
