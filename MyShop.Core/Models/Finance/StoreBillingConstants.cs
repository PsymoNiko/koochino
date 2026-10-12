namespace MyShop.Core.Models.Finance
{
    public static class StoreBillingConstants
    {
        public const string ModuleKey = "Finance";
        public const string CalculationRuleHighestBase = "HighestEligibleServiceBase";

        public static class PeriodKind
        {
            public const string Monthly = "Monthly";
            public const string Weekly = "Weekly";
            public const string Custom = "Custom";
        }

        public static class CalculationMethod
        {
            public const string Percent = "Percent";
            public const string Fixed = "Fixed";
            public const string PercentPlusFixed = "PercentPlusFixed";
        }

        public static class ServiceCode
        {
            public const string LuckyBox = "LUCKY_BOX";
            public const string DiscountCode = "DISCOUNT_CODE";
            public const string LoyaltyPoint = "LOYALTY_POINT";
            public const string Reward = "REWARD";
            public const string Retail = "RETAIL";
        }

        public static class ContractStatus
        {
            public const string Active = "Active";
            public const string Expired = "Expired";
            public const string Cancelled = "Cancelled";
        }

        public static class CommissionStatus
        {
            public const string Calculated = "Calculated";
            public const string IncludedInInvoice = "IncludedInInvoice";
            public const string Adjusted = "Adjusted";
            public const string Cancelled = "Cancelled";
            public const string Refunded = "Refunded";
        }

        public static class PeriodStatus
        {
            public const string Open = "Open";
            public const string Closed = "Closed";
            public const string Skipped = "Skipped";
        }

        public static class InvoiceStatus
        {
            public const string Issued = "Issued";
            public const string PartiallyPaid = "PartiallyPaid";
            public const string Paid = "Paid";
            public const string Overdue = "Overdue";
            public const string Cancelled = "Cancelled";
        }

        public static class OverdueAction
        {
            public const string Reminder = "Reminder";
            public const string Warning = "Warning";
            public const string LimitService = "LimitService";
            public const string ReduceVisibility = "ReduceVisibility";
            public const string Suspend = "Suspend";
        }

        public static class EventCode
        {
            public const string InvoiceOverdue = "InvoiceOverdue";
            public const string PaymentCompleted = "PaymentCompleted";
            public const string StoreSuspended = "StoreSuspended";
        }

        public static class DiscountType
        {
            public const string Percent = "Percent";
            public const string Amount = "Amount";
        }
    }

    public class StoreBillingActionResult
    {
        public bool Ok { get; set; }
        public string Error { get; set; }
        public int Id { get; set; }
        public string Message { get; set; }

        public static StoreBillingActionResult Success(int id = 0, string message = null) =>
            new() { Ok = true, Id = id, Message = message };
        public static StoreBillingActionResult Fail(string error) =>
            new() { Ok = false, Error = error };
    }
}
