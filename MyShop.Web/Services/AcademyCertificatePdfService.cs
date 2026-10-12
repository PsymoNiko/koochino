using System;
using System.Globalization;
using MyShop.Core.Models.Academy;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MyShop.Web.Services
{
    public interface IAcademyCertificatePdfService
    {
        byte[] GeneratePdf(AcademyCertificateDto cert);
    }

    public class AcademyCertificatePdfService : IAcademyCertificatePdfService
    {
        static AcademyCertificatePdfService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public byte[] GeneratePdf(AcademyCertificateDto cert)
        {
            if (cert == null)
                return Array.Empty<byte>();

            var issuedFa = ToPersianDate(cert.IssuedAt);
            var rangeFa = CourseRangeFa(cert);
            var gold = Color.FromHex("#b45309");
            var ink = Color.FromHex("#1e293b");

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(24);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontFamily("Tahoma").FontSize(14).FontColor(ink));
                    page.ContentFromRightToLeft();

                    page.Content().Border(4).BorderColor(gold).Padding(28).Column(col =>
                    {
                        col.Item().AlignCenter().Text("گواهی پایان دوره").FontSize(28).Bold().FontColor(gold);
                        col.Item().PaddingTop(6).AlignCenter().Text("آموزشگاه").FontSize(14).FontColor(Color.FromHex("#64748b"));
                        col.Item().PaddingTop(22).AlignCenter().Text("این گواهی تأیید می‌کند که").FontSize(13);
                        col.Item().PaddingTop(10).AlignCenter().Text(cert.StudentName ?? "—").FontSize(22).Bold();
                        if (!string.IsNullOrWhiteSpace(cert.StudentNationalCode)
                            && cert.StudentNationalCode != "0000000000")
                        {
                            col.Item().AlignCenter().Text($"کد ملی: {cert.StudentNationalCode}").FontSize(11).FontColor(Color.FromHex("#64748b"));
                        }
                        col.Item().PaddingTop(14).AlignCenter().Text("دوره آموزشی زیر را با موفقیت به پایان رسانده است:").FontSize(13);
                        col.Item().PaddingTop(8).AlignCenter().Text(cert.CourseTitle ?? "—").FontSize(20).Bold().FontColor(Color.FromHex("#4c1d95"));
                        if (!string.IsNullOrWhiteSpace(rangeFa))
                            col.Item().PaddingTop(6).AlignCenter().Text(rangeFa).FontSize(12);
                        if (!string.IsNullOrWhiteSpace(cert.InstructorName))
                            col.Item().PaddingTop(4).AlignCenter().Text($"مربی: {cert.InstructorName}").FontSize(12);
                        if (!string.IsNullOrWhiteSpace(cert.LocationName))
                            col.Item().AlignCenter().Text($"محل برگزاری: {cert.LocationName}").FontSize(12);

                        col.Item().PaddingTop(28).Row(row =>
                        {
                            row.RelativeItem().AlignCenter().Column(c =>
                            {
                                c.Item().Text($"شماره گواهی: {cert.CertificateNo}").FontSize(11);
                                c.Item().Text($"تاریخ صدور: {issuedFa}").FontSize(11);
                            });
                            row.RelativeItem().AlignCenter().Column(c =>
                            {
                                c.Item().PaddingTop(18).Text("امضا / مهر آموزشگاه").FontSize(11).FontColor(Color.FromHex("#94a3b8"));
                            });
                        });
                    });
                });
            }).GeneratePdf();
        }

        private static string ToPersianDate(DateTime d)
        {
            var pc = new PersianCalendar();
            return $"{pc.GetYear(d):0000}/{pc.GetMonth(d):00}/{pc.GetDayOfMonth(d):00}";
        }

        private static string CourseRangeFa(AcademyCertificateDto cert)
        {
            if (!cert.CourseStartDate.HasValue && !cert.CourseEndDate.HasValue) return "";
            var a = cert.CourseStartDate.HasValue ? ToPersianDate(cert.CourseStartDate.Value) : "—";
            var b = cert.CourseEndDate.HasValue ? ToPersianDate(cert.CourseEndDate.Value) : "—";
            return $"بازه برگزاری: {a} تا {b}";
        }
    }
}
