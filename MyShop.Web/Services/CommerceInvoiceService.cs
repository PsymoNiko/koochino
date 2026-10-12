using System.Text;
using MyShop.Core.Models.Commerce;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MyShop.Web.Services
{
    public interface ICommerceInvoiceService
    {
        byte[] GeneratePdf(CommerceOrderDetail order);
    }

    public class CommerceInvoiceService : ICommerceInvoiceService
    {
        static CommerceInvoiceService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public byte[] GeneratePdf(CommerceOrderDetail order)
        {
            if (order == null) return Encoding.UTF8.GetBytes("%PDF-1.4 empty");

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(40);
                    page.Content().Column(col =>
                    {
                        col.Item().Text("فاکتور فروشگاه آنلاین").Bold().FontSize(18);
                        col.Item().Text($"شماره: {order.OrderNumber}");
                        col.Item().Text($"مشتری: {order.CustomerName} — {order.CustomerPhone}");
                        col.Item().Text($"آدرس: {order.ShippingAddress}");
                        col.Item().Text($"وضعیت: {order.FulfillmentStatusTitle} / {order.PaymentStatusTitle}");
                        col.Item().PaddingVertical(10);
                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(3);
                                c.RelativeColumn();
                                c.RelativeColumn();
                                c.RelativeColumn();
                            });
                            t.Header(h =>
                            {
                                h.Cell().Text("کالا");
                                h.Cell().Text("تعداد");
                                h.Cell().Text("قیمت");
                                h.Cell().Text("جمع");
                            });
                            foreach (var i in order.Items ?? new())
                            {
                                t.Cell().Text(i.Title);
                                t.Cell().Text(i.Quantity.ToString());
                                t.Cell().Text(i.UnitPrice.ToString("N0"));
                                t.Cell().Text(i.LineTotal.ToString("N0"));
                            }
                        });
                        col.Item().PaddingTop(12).Text($"هزینه ارسال: {order.ShippingFee:N0} ریال");
                        if (order.TaxAmount > 0)
                            col.Item().Text($"مالیات: {order.TaxAmount:N0} ریال");
                        col.Item().Text($"مبلغ نهایی: {order.FinalAmount:N0} ریال").Bold();
                        if (!string.IsNullOrWhiteSpace(order.TrackingCode))
                            col.Item().Text($"کد رهگیری: {order.TrackingCode}");
                    });
                });
            }).GeneratePdf();
        }
    }
}
