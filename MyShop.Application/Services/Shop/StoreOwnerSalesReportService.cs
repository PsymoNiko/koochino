using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyShop.Application.Interfaces;
using MyShop.Core.Helpers;
using MyShop.Core.Models.Shop;

namespace MyShop.Application.Services
{
    public class StoreOwnerSalesReportService : IStoreOwnerSalesReportService
    {
        private readonly IStoreOwnerSalesReportRepository _repo;

        public StoreOwnerSalesReportService(IStoreOwnerSalesReportRepository repo)
        {
            _repo = repo;
        }

        public async Task<StoreOwnerSalesReportDto> GetAsync(StoreOwnerSalesFilter filter)
        {
            filter ??= new StoreOwnerSalesFilter();
            if (filter.StoreOwnerUserId <= 0)
                return new StoreOwnerSalesReportDto();

            if (filter.FromDate.HasValue && filter.ToDate.HasValue && filter.ToDate.Value.Date < filter.FromDate.Value.Date)
                (filter.FromDate, filter.ToDate) = (filter.ToDate, filter.FromDate);

            var summary = await _repo.GetSummaryAsync(filter) ?? new StoreOwnerSalesSummaryDto();
            var days = await _repo.GetByDayAsync(filter) ?? new List<StoreOwnerSalesDayDto>();
            var stores = await _repo.GetByStoreAsync(filter) ?? new List<StoreOwnerStoreSalesDto>();

            var from = filter.FromDate?.Date ?? (days.Count > 0 ? days[0].SaleDate.Date : DateTime.Today);
            var to = filter.ToDate?.Date ?? (days.Count > 0 ? days[^1].SaleDate.Date : DateTime.Today);
            if ((to - from).TotalDays > 366 * 6)
                from = to.AddYears(-5);

            return new StoreOwnerSalesReportDto
            {
                Summary = summary,
                Months = StoreOwnerSalesReportMath.FillPersianMonths(
                    StoreOwnerSalesReportMath.ByPersianMonth(days), from, to),
                Years = StoreOwnerSalesReportMath.ByPersianYear(days),
                Stores = StoreOwnerSalesReportMath.WithShare(stores)
            };
        }

        public Task<List<StoreOwnerSalesCustomerDto>> SearchCustomersAsync(int storeOwnerUserId, int? storeId, string searchText)
        {
            if (storeOwnerUserId <= 0)
                return Task.FromResult(new List<StoreOwnerSalesCustomerDto>());

            var q = StoreOwnerSalesReportMath.NormalizeSearch(searchText);
            if (q.Length < 2)
                return Task.FromResult(new List<StoreOwnerSalesCustomerDto>());

            return _repo.SearchCustomersAsync(storeOwnerUserId, storeId, q);
        }
    }
}
