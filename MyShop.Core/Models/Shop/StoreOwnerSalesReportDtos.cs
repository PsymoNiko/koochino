using System;
using System.Collections.Generic;

namespace MyShop.Core.Models.Shop
{
    public class StoreOwnerSalesDayDto
    {
        public DateTime SaleDate { get; set; }
        public long Amount { get; set; }
        public int OrderCount { get; set; }
    }

    public class StoreOwnerSalesPeriodDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string Label { get; set; } = "";
        public string ShortLabel { get; set; } = "";
        public long Amount { get; set; }
        public int OrderCount { get; set; }
    }

    public class StoreOwnerStoreSalesDto
    {
        public int StoreId { get; set; }
        public string StoreName { get; set; } = "";
        public long Amount { get; set; }
        public int OrderCount { get; set; }
        public int CustomerCount { get; set; }
        public double SharePercent { get; set; }
    }

    public class StoreOwnerSalesSummaryDto
    {
        public long TotalAmount { get; set; }
        public int OrderCount { get; set; }
        public int CustomerCount { get; set; }
        public long AvgOrderAmount { get; set; }
    }

    public class StoreOwnerSalesCustomerDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = "";
        public string Phone { get; set; } = "";
        public int OrderCount { get; set; }
        public long TotalAmount { get; set; }
    }

    public class StoreOwnerSalesReportDto
    {
        public StoreOwnerSalesSummaryDto Summary { get; set; } = new();
        public List<StoreOwnerSalesPeriodDto> Months { get; set; } = new();
        public List<StoreOwnerSalesPeriodDto> Years { get; set; } = new();
        public List<StoreOwnerStoreSalesDto> Stores { get; set; } = new();
    }

    public class StoreOwnerSalesFilter
    {
        public int StoreOwnerUserId { get; set; }
        public int? StoreId { get; set; }
        public int? CustomerUserId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
