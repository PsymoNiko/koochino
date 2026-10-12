using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using MyShop.Application.Interfaces;
using MyShop.Application.Services;
using MyShop.Core.Helpers;
using MyShop.Core.Models.Shop;
using MyShop.Core.Routing;

namespace MyShop.Tests
{
    public class StoreOwnerSalesReportTests
    {
        [Fact]
        public void ReportsRoute_MapsToShop()
        {
            Assert.Equal("Shop", PublicRouteModuleMapping.GetModuleForPath("/storeowner/reports"));
            Assert.Equal("Shop", PublicRouteModuleMapping.GetModuleForPath("/StoreOwner/Reports"));
            Assert.Equal("Commerce", PublicRouteModuleMapping.GetModuleForPath("/storeowner/commerce-reports"));
        }

        [Fact]
        public void Nowruz_SplitsPersianYearAndMonth()
        {
            var pc = new PersianCalendar();
            var lastEsfand = pc.ToDateTime(1404, 12, pc.GetDaysInMonth(1404, 12), 0, 0, 0, 0);
            var firstFarvardin = pc.ToDateTime(1405, 1, 1, 0, 0, 0, 0);
            var days = new[]
            {
                new StoreOwnerSalesDayDto { SaleDate = lastEsfand, Amount = 100, OrderCount = 1 },
                new StoreOwnerSalesDayDto { SaleDate = firstFarvardin, Amount = 250, OrderCount = 2 }
            };

            var months = StoreOwnerSalesReportMath.ByPersianMonth(days);
            var years = StoreOwnerSalesReportMath.ByPersianYear(days);

            Assert.Equal(2, months.Count);
            Assert.Contains(months, m => m.Year == 1404 && m.Month == 12 && m.Amount == 100);
            Assert.Contains(months, m => m.Year == 1405 && m.Month == 1 && m.Amount == 250);
            Assert.Equal(new[] { 1404, 1405 }, years.Select(y => y.Year).ToArray());
            Assert.Equal(100, years.First(y => y.Year == 1404).Amount);
            Assert.Equal(250, years.First(y => y.Year == 1405).Amount);
        }

        [Fact]
        public void FillPersianMonths_InsertsZeroGaps()
        {
            var pc = new PersianCalendar();
            var farvardin = pc.ToDateTime(1405, 1, 1, 0, 0, 0, 0);
            var khordad = pc.ToDateTime(1405, 3, 1, 0, 0, 0, 0);
            var months = StoreOwnerSalesReportMath.ByPersianMonth(new[]
            {
                new StoreOwnerSalesDayDto { SaleDate = farvardin, Amount = 10, OrderCount = 1 },
                new StoreOwnerSalesDayDto { SaleDate = khordad, Amount = 30, OrderCount = 1 }
            });

            var filled = StoreOwnerSalesReportMath.FillPersianMonths(months, farvardin, khordad);
            Assert.Equal(3, filled.Count);
            Assert.Equal(10, filled[0].Amount);
            Assert.Equal(0, filled[1].Amount);
            Assert.Equal(30, filled[2].Amount);
            Assert.Equal("خر", filled[2].ShortLabel);
        }

        [Fact]
        public void WithShare_OrdersAndPercents()
        {
            var rows = StoreOwnerSalesReportMath.WithShare(new[]
            {
                new StoreOwnerStoreSalesDto { StoreId = 1, StoreName = "A", Amount = 25 },
                new StoreOwnerStoreSalesDto { StoreId = 2, StoreName = "B", Amount = 75 }
            });

            Assert.Equal(2, rows[0].StoreId);
            Assert.Equal(75, rows[0].SharePercent);
            Assert.Equal(25, rows[1].SharePercent);
        }

        [Fact]
        public void PresetYear_StartsFarvardinFirst()
        {
            var pc = new PersianCalendar();
            var today = pc.ToDateTime(1405, 5, 26, 0, 0, 0, 0);
            var range = StoreOwnerSalesReportMath.PresetRange("year", today);
            Assert.Equal(pc.ToDateTime(1405, 1, 1, 0, 0, 0, 0), range.From);
            Assert.Equal(today, range.To);
        }

        [Fact]
        public void NormalizeSearch_PersianDigitsAndLikeChars()
        {
            Assert.Equal("0912", StoreOwnerSalesReportMath.NormalizeSearch("۰۹۱۲"));
            Assert.Equal("ali", StoreOwnerSalesReportMath.NormalizeSearch(" %ali_ "));
            Assert.Equal("", StoreOwnerSalesReportMath.NormalizeSearch("  "));
        }

        [Fact]
        public void BarPercent_ZeroAndMinimumVisible()
        {
            Assert.Equal(0, StoreOwnerSalesReportMath.BarPercent(0, 100));
            Assert.True(StoreOwnerSalesReportMath.BarPercent(1, 1000) >= 4);
            Assert.Equal(100, StoreOwnerSalesReportMath.BarPercent(50, 50));
        }

        [Fact]
        public async Task Service_SearchRequiresTwoChars()
        {
            var repo = new FakeRepo();
            var svc = new StoreOwnerSalesReportService(repo);
            var empty = await svc.SearchCustomersAsync(9, null, "ا");
            Assert.Empty(empty);
            Assert.Equal(0, repo.SearchCalls);

            var hits = await svc.SearchCustomersAsync(9, 3, "۰۹۱۲");
            Assert.Equal(1, repo.SearchCalls);
            Assert.Equal("0912", repo.LastSearch);
            Assert.Equal(3, repo.LastStoreId);
        }

        [Fact]
        public async Task Service_BuildsMonthsFromDays()
        {
            var pc = new PersianCalendar();
            var from = pc.ToDateTime(1405, 1, 1, 0, 0, 0, 0);
            var to = pc.ToDateTime(1405, 2, 1, 0, 0, 0, 0);
            var repo = new FakeRepo
            {
                Days =
                {
                    new StoreOwnerSalesDayDto { SaleDate = from, Amount = 40, OrderCount = 2 }
                },
                Summary = new StoreOwnerSalesSummaryDto { TotalAmount = 40, OrderCount = 2, AvgOrderAmount = 20, CustomerCount = 1 }
            };
            var svc = new StoreOwnerSalesReportService(repo);
            var report = await svc.GetAsync(new StoreOwnerSalesFilter
            {
                StoreOwnerUserId = 4,
                FromDate = from,
                ToDate = to
            });

            Assert.Equal(40, report.Summary.TotalAmount);
            Assert.Equal(2, report.Months.Count);
            Assert.Equal(40, report.Months[0].Amount);
            Assert.Equal(0, report.Months[1].Amount);
        }

        sealed class FakeRepo : IStoreOwnerSalesReportRepository
        {
            public StoreOwnerSalesSummaryDto Summary { get; set; } = new();
            public List<StoreOwnerSalesDayDto> Days { get; set; } = new();
            public List<StoreOwnerStoreSalesDto> Stores { get; set; } = new();
            public int SearchCalls { get; private set; }
            public string LastSearch { get; private set; } = "";
            public int? LastStoreId { get; private set; }

            public Task<StoreOwnerSalesSummaryDto> GetSummaryAsync(StoreOwnerSalesFilter filter)
                => Task.FromResult(Summary);

            public Task<List<StoreOwnerSalesDayDto>> GetByDayAsync(StoreOwnerSalesFilter filter)
                => Task.FromResult(Days);

            public Task<List<StoreOwnerStoreSalesDto>> GetByStoreAsync(StoreOwnerSalesFilter filter)
                => Task.FromResult(Stores);

            public Task<List<StoreOwnerSalesCustomerDto>> SearchCustomersAsync(int storeOwnerUserId, int? storeId, string searchText)
            {
                SearchCalls++;
                LastSearch = searchText;
                LastStoreId = storeId;
                return Task.FromResult(new List<StoreOwnerSalesCustomerDto>
                {
                    new() { UserId = 1, FullName = "علی", Phone = searchText }
                });
            }
        }
    }
}
