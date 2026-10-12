using System.Collections.Generic;
using System.Linq;
using MyShop.Core.Models.Academy;

namespace MyShop.Core.Academy
{
    /// <summary>جمع گزارش Daily در حافظه — مستقل از SPهای گزارش دوره/شهریه.</summary>
    public static class AcademyDailyReportMath
    {
        public static AcademyDailyReportSummaryDto Summarize(IEnumerable<AcademyDailyByDateReportDto> rows)
        {
            var list = rows?.ToList() ?? new List<AcademyDailyByDateReportDto>();
            return new AcademyDailyReportSummaryDto
            {
                PendingRequests = list.Sum(x => x.PendingCount),
                WaitlistedRequests = list.Sum(x => x.WaitlistedCount),
                ApprovedRequests = list.Sum(x => x.ApprovedCount),
                RejectedRequests = list.Sum(x => x.RejectedCount),
                PresentCount = list.Sum(x => x.PresentCount),
                AbsentCount = list.Sum(x => x.AbsentCount),
                LateCount = list.Sum(x => x.LateCount),
                ExcusedCount = list.Sum(x => x.ExcusedCount)
            };
        }
    }
}
