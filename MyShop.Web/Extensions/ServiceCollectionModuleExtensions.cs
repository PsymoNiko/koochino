using Microsoft.Extensions.DependencyInjection;
using MyShop.Application.Interfaces;
using MyShop.Application.Interfaces.Academy;
using MyShop.Application.Interfaces.Assets;
using MyShop.Application.Interfaces.Contracts;
using MyShop.Application.Interfaces.Crm;
using MyShop.Application.Interfaces.Contact;
using MyShop.Application.Interfaces.Finance;
using MyShop.Application.Interfaces.Companies;
using MyShop.Application.Interfaces.Inventory;
using MyShop.Application.Interfaces.Personnel;
using MyShop.Application.Interfaces.Products;
using MyShop.Application.Interfaces.ProjectManagement;
using MyShop.Application.Interfaces.Reports;
using MyShop.Application.Interfaces.Tickets;
using MyShop.Application.Interfaces.Training;
using MyShop.Application.Interfaces.Messaging;
using MyShop.Application.Interfaces.SocialMedia;
using MyShop.Application.Interfaces.AdminAccess;
using MyShop.Application.Interfaces.Security;
using MyShop.Application.Services;
using MyShop.Application.Services.Messaging;
using MyShop.Application.Services.SocialMedia;
using MyShop.Infrastructure.Repositories.SocialMedia;
using MyShop.Infrastructure.SocialMedia;
using MyShop.Infrastructure.SocialMedia.Providers;
using MyShop.Application.Services.AdminAccess;
using MyShop.Application.Services.Security;
using MyShop.Application.Services.Academy;
using MyShop.Application.Services.Assets;
using MyShop.Application.Services.Contracts;
using MyShop.Application.Services.Crm;
using MyShop.Application.Services.Contact;
using MyShop.Application.Services.Finance;
using MyShop.Application.Services.Companies;
using MyShop.Application.Services.Inventory;
using MyShop.Application.Services.Personnel;
using MyShop.Application.Services.Products;
using MyShop.Application.Services.ProjectManagement;
using MyShop.Application.Services.Reports;
using MyShop.Application.Services.Tickets;
using MyShop.Application.Services.Training;
using MyShop.Application.Interfaces.Vendor;
using MyShop.Application.Services.Vendor;
using MyShop.Infrastructure.Repositories.Academy;
using MyShop.Infrastructure.Repositories.Security;
using MyShop.Infrastructure.Repositories.Vendor;
using MyShop.Core.Interfaces;
using MyShop.Infrastructure.Data;
using MyShop.Infrastructure.Repository;
using MyShop.Web.Services;

namespace MyShop.Web.Extensions
{
    public static class ServiceCollectionModuleExtensions
    {
        /// <summary>ثبت ماژول کاربران (احراز هویت، کاربر، فعالیت)</summary>
        public static IServiceCollection AddUsersModule(this IServiceCollection services)
        {
            services.AddScoped<IAuthRepository, AuthRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IUserActivityRepository, UserActivityRepository>();
            services.AddScoped<IPwaInstallLogRepository, PwaInstallLogRepository>();
            services.AddScoped<IAuthService, WebAuthService>();
            services.AddScoped<IPasswordResetService, PasswordResetService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IUserActivityService, UserActivityService>();
            services.AddScoped<IPwaInstallLogService, PwaInstallLogService>();
            services.AddScoped<IAdminAccessRepository, AdminAccessRepository>();
            services.AddScoped<IPermissionRepository, PermissionRepository>();
            services.AddScoped<IPermissionService, PermissionService>();
            services.AddScoped<IConditionEvaluator, DenyConditionalByDefaultEvaluator>();
            services.AddScoped<ISecurityAdminRepository, SecurityAdminRepository>();
            services.AddScoped<ISecurityAdminService, SecurityAdminService>();
            services.AddScoped<IUserCompanyRepository, UserCompanyRepository>();
            services.AddScoped<IPortalPermissionService, PortalPermissionService>();
            services.AddScoped<IDataScopeResolver, DataScopeResolver>();
            services.AddScoped<IAdminPermissionService, AdminPermissionService>();
            services.AddScoped<MyShop.Application.Interfaces.Sites.ISiteRepository, MyShop.Infrastructure.Repositories.Sites.SiteRepository>();
            services.AddScoped<MyShop.Application.Interfaces.Sites.ISiteService, MyShop.Application.Services.Sites.SiteService>();
            return services;
        }

        /// <summary>ثبت ماژول فروشگاه‌ها و مدارک فروشگاه</summary>
        public static IServiceCollection AddStoresModule(this IServiceCollection services)
        {
            services.AddScoped<IStoreRepository, StoreRepository>();
            services.AddScoped<IStoreDocumentRepository, StoreDocumentRepository>();
            services.AddScoped<ILocationRepository, LocationRepository>();
            services.AddScoped<ILocationService, LocationService>();
            services.AddScoped<IStoreService, StoreService>();
            services.AddScoped<IStoreDocumentService, StoreDocumentService>();
            services.AddScoped<IStoreCategoryRepository, StoreCategoryRepository>();
            services.AddScoped<IStoreCategoryService, StoreCategoryService>();
            services.AddScoped<IInviteRepository, InviteRepository>();
            services.AddScoped<IInviteService, InviteService>();
            return services;
        }

        /// <summary>ثبت ماژول بلاگ (پست، نظر، سئو)</summary>
        public static IServiceCollection AddBlogModule(this IServiceCollection services)
        {
            services.AddScoped<IPostRepository, PostRepository>();
            services.AddScoped<ICommentRepository, CommentRepository>();
            services.AddScoped<IPageSeoRepository, PageSeoRepository>();
            services.AddScoped<IPostService, PostService>();
            services.AddScoped<ICommentService, CommentService>();
            services.AddScoped<PageSeoService>();
            services.AddScoped<IPageSeoService, CachedPageSeoService>();
            return services;
        }

        /// <summary>ثبت ماژول فروش (سفارش، کوپن، جعبه شانس)</summary>
        public static IServiceCollection AddShopModule(this IServiceCollection services)
        {
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<ICouponRepository, CouponRepository>();
            services.AddScoped<IChanceBoxRepository, ChanceBoxRepository>();
            services.AddScoped<IOrderPurchaseNotifier, BaleOrderPurchaseNotifier>();
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<ICouponService, CouponService>();
            services.AddScoped<IChanceBoxService, ChanceBoxService>();
            services.AddScoped<IStoreOwnerSalesReportRepository, StoreOwnerSalesReportRepository>();
            services.AddScoped<IStoreOwnerSalesReportService, StoreOwnerSalesReportService>();
            return services;
        }

        /// <summary>فروشگاه آنلاین (سبد، ارسال، پرداخت، پیگیری)</summary>
        public static IServiceCollection AddCommerceModule(this IServiceCollection services)
        {
            services.AddScoped<MyShop.Application.Interfaces.Commerce.ICommerceCatalogRepository, MyShop.Infrastructure.Repositories.Commerce.CommerceCatalogRepository>();
            services.AddScoped<MyShop.Application.Interfaces.Commerce.ICommerceCartRepository, MyShop.Infrastructure.Repositories.Commerce.CommerceCartRepository>();
            services.AddScoped<MyShop.Application.Interfaces.Commerce.ICommerceOrderRepository, MyShop.Infrastructure.Repositories.Commerce.CommerceOrderRepository>();
            services.AddScoped<MyShop.Application.Interfaces.Commerce.IUserAddressRepository, MyShop.Infrastructure.Repositories.Commerce.UserAddressRepository>();
            services.AddScoped<MyShop.Application.Interfaces.Commerce.IUserAddressService, MyShop.Application.Services.Commerce.UserAddressService>();
            services.AddScoped<MyShop.Application.Interfaces.Commerce.IPaymentGateway, MyShop.Application.Services.Commerce.MockPaymentGateway>();
            services.AddScoped<MyShop.Application.Interfaces.Commerce.ICommerceService, MyShop.Application.Services.Commerce.CommerceService>();
            services.AddScoped<MyShop.Application.Interfaces.Commerce.ICommerceFeatureFlags, MyShop.Application.Services.Commerce.CommerceFeatureFlags>();
            services.AddScoped<MyShop.Application.Interfaces.Commerce.ICommerceProductAccessRepository, MyShop.Infrastructure.Repositories.Commerce.CommerceProductAccessRepository>();
            services.AddScoped<MyShop.Application.Interfaces.Commerce.ICommerceBrandRepository, MyShop.Infrastructure.Repositories.Commerce.CommerceBrandRepository>();
            services.AddScoped<MyShop.Application.Interfaces.Commerce.ICommerceTagRepository, MyShop.Infrastructure.Repositories.Commerce.CommerceTagRepository>();
            services.AddScoped<MyShop.Application.Interfaces.Commerce.ICommerceProductImageRepository, MyShop.Infrastructure.Repositories.Commerce.CommerceProductImageRepository>();
            services.AddScoped<MyShop.Application.Interfaces.Commerce.ICommerceProductRelationRepository, MyShop.Infrastructure.Repositories.Commerce.CommerceProductRelationRepository>();
            services.AddScoped<MyShop.Application.Interfaces.Commerce.ICommerceProductSeoRepository, MyShop.Infrastructure.Repositories.Commerce.CommerceProductSeoRepository>();
            services.AddScoped<MyShop.Application.Interfaces.Commerce.ICommerceAttributeRepository, MyShop.Infrastructure.Repositories.Commerce.CommerceAttributeRepository>();
            services.AddScoped<MyShop.Application.Interfaces.Commerce.ICommerceVariationRepository, MyShop.Infrastructure.Repositories.Commerce.CommerceVariationRepository>();
            services.AddScoped<MyShop.Application.Interfaces.Commerce.ICommerceProductCategoryRepository, MyShop.Infrastructure.Repositories.Commerce.CommerceProductCategoryRepository>();
            services.AddScoped<MyShop.Application.Interfaces.Commerce.ICommerceBrandService, MyShop.Application.Services.Commerce.CommerceBrandService>();
            services.AddScoped<MyShop.Application.Interfaces.Commerce.ICommerceTagService, MyShop.Application.Services.Commerce.CommerceTagService>();
            services.AddScoped<MyShop.Application.Interfaces.Commerce.ICommerceProductImageService, MyShop.Application.Services.Commerce.CommerceProductImageService>();
            services.AddScoped<MyShop.Application.Interfaces.Commerce.ICommerceProductRelationService, MyShop.Application.Services.Commerce.CommerceProductRelationService>();
            services.AddScoped<MyShop.Application.Interfaces.Commerce.ICommerceProductSeoService, MyShop.Application.Services.Commerce.CommerceProductSeoService>();
            services.AddScoped<MyShop.Application.Interfaces.Commerce.ICommerceAttributeService, MyShop.Application.Services.Commerce.CommerceAttributeService>();
            services.AddScoped<MyShop.Application.Interfaces.Commerce.ICommerceVariationService, MyShop.Application.Services.Commerce.CommerceVariationService>();
            services.AddScoped<MyShop.Application.Interfaces.Commerce.ICommerceProductCategoryService, MyShop.Application.Services.Commerce.CommerceProductCategoryService>();
            services.AddScoped<MyShop.Application.Interfaces.Commerce.ICommerceSellerCatalogGuard, MyShop.Application.Services.Commerce.CommerceSellerCatalogGuard>();
            services.AddScoped<MyShop.Application.Interfaces.Commerce.ICommerceShopCatalogService, MyShop.Application.Services.Commerce.CommerceShopCatalogService>();
            services.AddScoped<MyShop.Application.Interfaces.Commerce.ICommercePricingService, MyShop.Application.Services.Commerce.CommercePricingService>();
            services.AddScoped<MyShop.Application.Interfaces.Commerce.ICommerceVariationCartRepository, MyShop.Infrastructure.Repositories.Commerce.CommerceVariationCartRepository>();
            services.AddScoped<MyShop.Application.Interfaces.Commerce.ICommerceVariationCartService, MyShop.Application.Services.Commerce.CommerceVariationCartService>();
            services.AddScoped<MyShop.Application.Interfaces.Commerce.ICommerceStockReservationRepository, MyShop.Infrastructure.Repositories.Commerce.CommerceStockReservationRepository>();
            services.AddScoped<MyShop.Application.Interfaces.Commerce.ICommercePromotionRepository, MyShop.Infrastructure.Repositories.Commerce.CommercePromotionRepository>();
            services.AddScoped<MyShop.Application.Interfaces.Commerce.ICommercePromotionService, MyShop.Application.Services.Commerce.CommercePromotionService>();
            services.AddScoped<MyShop.Application.Interfaces.Commerce.ICommerceTaxRepository, MyShop.Infrastructure.Repositories.Commerce.CommerceTaxRepository>();
            services.AddScoped<MyShop.Application.Interfaces.Commerce.ICommerceStoreShippingRepository, MyShop.Infrastructure.Repositories.Commerce.CommerceStoreShippingRepository>();
            services.AddScoped<MyShop.Application.Interfaces.Commerce.ICommerceRefundRepository, MyShop.Infrastructure.Repositories.Commerce.CommerceRefundRepository>();
            services.AddScoped<MyShop.Application.Interfaces.Commerce.ICommerceRefundService, MyShop.Application.Services.Commerce.CommerceRefundService>();
            services.AddScoped<MyShop.Application.Interfaces.Commerce.ICommerceWishlistRepository, MyShop.Infrastructure.Repositories.Commerce.CommerceWishlistRepository>();
            services.AddScoped<MyShop.Application.Interfaces.Commerce.ICommerceWishlistService, MyShop.Application.Services.Commerce.CommerceWishlistService>();
            services.AddScoped<MyShop.Application.Interfaces.Commerce.ICommerceProductReviewService, MyShop.Application.Services.Commerce.CommerceProductReviewService>();
            services.AddScoped<MyShop.Application.Interfaces.Commerce.ICommerceReportRepository, MyShop.Infrastructure.Repositories.Commerce.CommerceReportRepository>();
            services.AddScoped<MyShop.Application.Interfaces.Commerce.ICommerceReportService, MyShop.Application.Services.Commerce.CommerceReportService>();
            services.AddScoped<MyShop.Application.Interfaces.Commerce.ICommerceSettlementRepository, MyShop.Infrastructure.Repositories.Commerce.CommerceSettlementRepository>();
            services.AddScoped<MyShop.Application.Interfaces.Commerce.ICommerceSettlementService, MyShop.Application.Services.Commerce.CommerceSettlementService>();
            services.AddScoped<MyShop.Application.Interfaces.AdminAccess.IAdminAccessAuditRepository, MyShop.Infrastructure.Repositories.AdminAccess.AdminAccessAuditRepository>();
            return services;
        }

        /// <summary>ثبت ماژول امتیاز و ماموریت‌ها</summary>
        public static IServiceCollection AddPointsModule(this IServiceCollection services)
        {
            services.AddScoped<IPointRepository, PointRepository>();
            services.AddScoped<IPointService, PointService>();
            services.AddScoped<MyShop.Application.Interfaces.Loyalty.ILoyaltyRoadmapRepository, MyShop.Infrastructure.Repositories.Loyalty.LoyaltyRoadmapRepository>();
            services.AddScoped<MyShop.Application.Interfaces.Loyalty.ILoyaltyRoadmapService, MyShop.Application.Services.Loyalty.LoyaltyRoadmapService>();
            services.AddScoped<MyShop.Application.Interfaces.Loyalty.ICustomerPurchaseRewardRepository, MyShop.Infrastructure.Repositories.Loyalty.CustomerPurchaseRewardRepository>();
            services.AddScoped<MyShop.Application.Interfaces.Loyalty.ICustomerPurchaseRewardService, MyShop.Application.Services.Loyalty.CustomerPurchaseRewardService>();
            return services;
        }

        /// <summary>ثبت ماژول جعبه شانس لیگی</summary>
        public static IServiceCollection AddLuckyLeagueModule(this IServiceCollection services)
        {
            services.AddScoped<MyShop.Application.Interfaces.Lucky.ILuckyLeagueRepository, MyShop.Infrastructure.Repositories.Lucky.LuckyLeagueRepository>();
            services.AddScoped<MyShop.Application.Interfaces.Lucky.ILuckyLeagueService, MyShop.Application.Services.Lucky.LuckyLeagueService>();
            return services;
        }

        /// <summary>ثبت ماژول اعلان‌ها</summary>
        public static IServiceCollection AddAnnouncementsModule(this IServiceCollection services)
        {
            services.AddScoped<IAnnouncementRepository, AnnouncementRepository>();
            services.AddScoped<IAnnouncementService, AnnouncementService>();
            return services;
        }

        /// <summary>ثبت ماژول مشترک (بات بله و غیره)</summary>
        public static IServiceCollection AddCommonModule(this IServiceCollection services)
        {
            services.AddScoped<IBaleBotFeedbackRepository, BaleBotFeedbackRepository>();
            return services;
        }

        /// <summary>پیام‌رسانی و OTP (بله فاز ۱ — همیشه فعال برای ثبت‌نام)</summary>
        public static IServiceCollection AddMessagingModule(this IServiceCollection services)
        {
            services.AddScoped<IUserMessagingLinkRepository, UserMessagingLinkRepository>();
            services.AddScoped<IMessagingOtpRepository, MessagingOtpRepository>();
            services.AddScoped<IMessagingSendLogRepository, MessagingSendLogRepository>();
            services.AddScoped<ISmsLineRepository, MyShop.Infrastructure.Repositories.Messaging.SmsLineRepository>();
            services.AddScoped<ISmsTemplateRepository, MyShop.Infrastructure.Repositories.Messaging.SmsTemplateRepository>();
            services.AddScoped<ISmsTemplateService, SmsTemplateService>();
            services.AddScoped<IBotMenuItemRepository, BotMenuItemRepository>();
            services.AddScoped<IOrderNotifyRecipientRepository, OrderNotifyRecipientRepository>();
            services.AddScoped<IMessagingOtpService, MessagingOtpService>();
            services.AddScoped<ISiteBotRepository, MyShop.Infrastructure.Repositories.Messaging.SiteBotRepository>();
            services.AddScoped<IBotCredentialsResolver, MyShop.Web.Services.BotCredentialsResolver>();
            services.AddScoped<IMessagingOutboundChannelRepository, MyShop.Infrastructure.Repositories.Messaging.MessagingOutboundChannelRepository>();
            // بازخورد بله هم برای webhook لازم است حتی اگر Common خاموش باشد
            services.AddScoped<IBaleBotFeedbackRepository, BaleBotFeedbackRepository>();
            return services;
        }

        /// <summary>ثبت ماژول قراردادها</summary>
        public static IServiceCollection AddContractsModule(this IServiceCollection services)
        {
            services.AddScoped<IContractRepository, ContractRepository>();
            services.AddScoped<IContractService, ContractService>();
            return services;
        }

        /// <summary>ثبت ماژول تیکت پشتیبانی</summary>
        public static IServiceCollection AddTicketsModule(this IServiceCollection services)
        {
            services.AddScoped<ITicketRepository, TicketRepository>();
            services.AddScoped<ITicketService, TicketService>();
            return services;
        }

        /// <summary>چت زنده پشتیبانی (Support) — SignalR + صندوق اپراتور</summary>
        public static IServiceCollection AddSupportModule(this IServiceCollection services)
        {
            services.AddScoped<MyShop.Application.Interfaces.SupportChat.ISupportChatRepository, MyShop.Infrastructure.Repositories.SupportChat.SupportChatRepository>();
            services.AddScoped<MyShop.Application.Interfaces.SupportChat.ISupportDeskRepository, MyShop.Infrastructure.Repositories.SupportChat.SupportDeskRepository>();
            services.AddScoped<MyShop.Application.Interfaces.SupportChat.ISupportChatRealtimePublisher, MyShop.Web.Services.SupportChatRealtimePublisher>();
            services.AddScoped<MyShop.Application.Services.SupportChat.SupportChatService>();
            services.AddScoped<MyShop.Web.Services.SupportChatAccessTokenService>();
            services.AddScoped<MyShop.Application.Interfaces.SupportChat.ISupportChatService>(sp =>
                new MyShop.Web.Services.SupportChatCookieBinder(
                    sp.GetRequiredService<MyShop.Application.Services.SupportChat.SupportChatService>(),
                    sp.GetRequiredService<MyShop.Web.Services.SupportChatAccessTokenService>()));
            services.AddScoped<MyShop.Application.Interfaces.SupportChat.ISupportDeskService, MyShop.Application.Services.SupportChat.SupportDeskService>();
            services.AddScoped<MyShop.Application.Interfaces.SupportChat.IKnowledgeRepository, MyShop.Infrastructure.Repositories.SupportChat.KnowledgeRepository>();
            services.AddScoped<MyShop.Application.Interfaces.SupportChat.IKnowledgeService, MyShop.Application.Services.SupportChat.KnowledgeService>();
            services.AddScoped<MyShop.Application.Interfaces.SupportChat.ISupportAssistRepository, MyShop.Infrastructure.Repositories.SupportChat.SupportAssistRepository>();
            services.AddScoped<MyShop.Application.Interfaces.SupportChat.ISupportAssistService, MyShop.Application.Services.SupportChat.SupportAssistService>();
            services.AddScoped<MyShop.Application.Interfaces.SupportChat.ISupportAnalyticsRepository, MyShop.Infrastructure.Repositories.SupportChat.SupportAnalyticsRepository>();
            services.AddScoped<MyShop.Application.Interfaces.SupportChat.ISupportAnalyticsService, MyShop.Application.Services.SupportChat.SupportAnalyticsService>();
            services.AddScoped<MyShop.Application.Interfaces.SupportChat.ISupportAutoReplyService, MyShop.Application.Services.SupportChat.SupportAutoReplyService>();
            services.AddScoped<MyShop.Application.Interfaces.SupportChat.ISupportChannelOutbound, MyShop.Web.Services.SupportChannelOutbound>();
            services.AddScoped<MyShop.Application.Interfaces.SupportChat.ISupportOmnichannelService, MyShop.Application.Services.SupportChat.SupportOmnichannelService>();
            return services;
        }

        /// <summary>ثبت ماژول پرسونل</summary>
        public static IServiceCollection AddPersonnelModule(this IServiceCollection services)
        {
            services.AddScoped<IPersonnelRepository, PersonnelRepository>();
            services.AddScoped<IPersonnelService, PersonnelService>();
            return services;
        }

        /// <summary>ثبت ماژول انبار</summary>
        public static IServiceCollection AddInventoryModule(this IServiceCollection services)
        {
            services.AddScoped<IWarehouseRepository, WarehouseRepository>();
            services.AddScoped<IWarehouseService, WarehouseService>();
            services.AddScoped<IProductUnitRepository, ProductUnitRepository>();
            services.AddScoped<IPriceListRepository, PriceListRepository>();
            services.AddScoped<IProductPriceRepository, ProductPriceRepository>();
            services.AddScoped<IInventoryStockRepository, InventoryStockRepository>();
            services.AddScoped<IInventoryDocumentRepository, InventoryDocumentRepository>();
            services.AddScoped<IStoreOwnerAccessRepository, StoreOwnerAccessRepository>();
            services.AddScoped<IProductUnitService, ProductUnitService>();
            services.AddScoped<IPriceListService, PriceListService>();
            services.AddScoped<IProductPriceService, ProductPriceService>();
            services.AddScoped<IInventoryStockService, InventoryStockService>();
            services.AddScoped<IInventoryDocumentService, InventoryDocumentService>();
            services.AddScoped<IStoreOwnerPermissionService, StoreOwnerPermissionService>();
            return services;
        }

        /// <summary>ثبت ماژول CRM</summary>
        public static IServiceCollection AddCrmModule(this IServiceCollection services)
        {
            services.AddScoped<ICrmContactRepository, CrmContactRepository>();
            services.AddScoped<ICrmContactService, CrmContactService>();
            return services;
        }

        /// <summary>ارتباط — درخواست تماس از لندینگ</summary>
        public static IServiceCollection AddContactModule(this IServiceCollection services)
        {
            services.AddScoped<IContactLeadRepository, MyShop.Infrastructure.Repositories.Contact.ContactLeadRepository>();
            services.AddScoped<IContactLeadService, ContactLeadService>();
            return services;
        }

        /// <summary>ثبت ماژول گزارش‌ها</summary>
        public static IServiceCollection AddReportsModule(this IServiceCollection services)
        {
            services.AddScoped<IReportDefinitionRepository, ReportDefinitionRepository>();
            services.AddScoped<IReportDefinitionService, ReportDefinitionService>();
            return services;
        }

        /// <summary>ثبت ماژول مالی</summary>
        public static IServiceCollection AddFinanceModule(this IServiceCollection services)
        {
            services.AddScoped<IFinanceAccountRepository, FinanceAccountRepository>();
            services.AddScoped<IFinanceAccountService, FinanceAccountService>();
            services.AddScoped<IStoreBillingRepository, MyShop.Infrastructure.Repositories.Finance.StoreBillingRepository>();
            services.AddScoped<IStoreBillingSettingsService, StoreBillingSettingsService>();
            services.AddScoped<IStoreBillingCatalogService, StoreBillingCatalogService>();
            services.AddScoped<IStoreBillingCommissionService, StoreBillingCommissionService>();
            services.AddScoped<IStoreBillingInvoiceService, StoreBillingInvoiceService>();
            services.AddScoped<IStoreBillingEventSink, StoreBillingEventSink>();
            services.AddScoped<IStoreBillingOverdueService, StoreBillingOverdueService>();
            services.AddScoped<IStoreVoucherWalletRepository, MyShop.Infrastructure.Repositories.Finance.StoreVoucherWalletRepository>();
            services.AddScoped<IStoreVoucherWalletService, StoreVoucherWalletService>();
            return services;
        }

        /// <summary>ثبت ماژول دارایی‌ها</summary>
        public static IServiceCollection AddAssetsModule(this IServiceCollection services)
        {
            services.AddScoped<IAssetRepository, AssetRepository>();
            services.AddScoped<IAssetService, AssetService>();
            return services;
        }

        /// <summary>ثبت ماژول آموزش</summary>
        public static IServiceCollection AddTrainingModule(this IServiceCollection services)
        {
            services.AddScoped<ITrainingCourseRepository, TrainingCourseRepository>();
            services.AddScoped<ITrainingCourseService, TrainingCourseService>();
            return services;
        }

        /// <summary>ثبت ماژول آموزشگاه (Academy)</summary>
        public static IServiceCollection AddAcademyModule(this IServiceCollection services)
        {
            services.AddScoped<IAcademyModuleMarker, AcademyModuleMarker>();
            services.AddScoped<IAcademyStudentRepository, AcademyStudentRepository>();
            services.AddScoped<IAcademyInstructorRepository, AcademyInstructorRepository>();
            services.AddScoped<IAcademyStudentService, AcademyStudentService>();
            services.AddScoped<IAcademyInstructorService, AcademyInstructorService>();
            services.AddScoped<IAcademyLocationRepository, AcademyLocationRepository>();
            services.AddScoped<IAcademyActivityCategoryRepository, AcademyActivityCategoryRepository>();
            services.AddScoped<IAcademyLocationService, AcademyLocationService>();
            services.AddScoped<IAcademyActivityCategoryService, AcademyActivityCategoryService>();
            services.AddScoped<IAcademyActivityRepository, AcademyActivityRepository>();
            services.AddScoped<IAcademyActivitySessionRepository, AcademyActivitySessionRepository>();
            services.AddScoped<IAcademyActivityRequestRepository, AcademyActivityRequestRepository>();
            services.AddScoped<IAcademyActivityService, AcademyActivityService>();
            services.AddScoped<IAcademyActivitySessionService, AcademyActivitySessionService>();
            services.AddScoped<IAcademyActivityRequestService, AcademyActivityRequestService>();
            services.AddScoped<IAcademyCourseRepository, AcademyCourseRepository>();
            services.AddScoped<IAcademyCourseService, AcademyCourseService>();
            services.AddScoped<IAcademyScheduleExceptionRepository, AcademyScheduleExceptionRepository>();
            services.AddScoped<IAcademyScheduleExceptionService, AcademyScheduleExceptionService>();
            services.AddScoped<IAcademyCourseScheduleRequestRepository, AcademyCourseScheduleRequestRepository>();
            services.AddScoped<IAcademyCourseScheduleRequestService, AcademyCourseScheduleRequestService>();
            services.AddScoped<IAcademyEnrollmentRepository, AcademyEnrollmentRepository>();
            services.AddScoped<IAcademyEnrollmentService, AcademyEnrollmentService>();
            services.AddScoped<IAcademyAttendanceRepository, AcademyAttendanceRepository>();
            services.AddScoped<IAcademyAttendanceService, AcademyAttendanceService>();
            services.AddScoped<IAcademyPaymentRepository, AcademyPaymentRepository>();
            services.AddScoped<IAcademyPaymentService, AcademyPaymentService>();
            services.AddScoped<IAcademyNotificationRepository, AcademyNotificationRepository>();
            services.AddScoped<IAcademyNotificationService, AcademyNotificationService>();
            services.AddScoped<IAcademyReportRepository, AcademyReportRepository>();
            services.AddScoped<IAcademyReportService, AcademyReportService>();
            services.AddScoped<IAcademyCalendarRepository, AcademyCalendarRepository>();
            services.AddScoped<IAcademyCalendarService, AcademyCalendarService>();
            services.AddScoped<IAcademyCertificateRepository, AcademyCertificateRepository>();
            services.AddScoped<IAcademyCertificateService, AcademyCertificateService>();
            services.AddScoped<IAcademyEvaluationRepository, AcademyEvaluationRepository>();
            services.AddScoped<IAcademyEvaluationService, AcademyEvaluationService>();
            services.AddScoped<IAcademyLessonRepository, AcademyLessonRepository>();
            services.AddScoped<IAcademyLessonService, AcademyLessonService>();
            services.AddScoped<IAcademyQuizRepository, AcademyQuizRepository>();
            services.AddScoped<IAcademyQuizService, AcademyQuizService>();
            services.AddScoped<IAcademyPointsRepository, AcademyPointsRepository>();
            services.AddScoped<IAcademyPointsService, AcademyPointsService>();
            return services;
        }

        /// <summary>ثبت ماژول محصولات</summary>
        public static IServiceCollection AddProductsModule(this IServiceCollection services)
        {
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IProductService, MyShop.Application.Services.Products.ProductService>();
            return services;
        }

        /// <summary>ثبت ماژول مدیریت پروژه</summary>
        public static IServiceCollection AddProjectManagementModule(this IServiceCollection services)
        {
            services.AddScoped<IProjectRepository, ProjectRepository>();
            services.AddScoped<IProjectService, ProjectService>();
            services.AddScoped<IProjectBoardRepository, ProjectBoardRepository>();
            services.AddScoped<IProjectBoardService, ProjectBoardService>();
            return services;
        }

        /// <summary>ثبت ماژول شرکت‌ها و هلدینگ</summary>
        public static IServiceCollection AddCompaniesModule(this IServiceCollection services)
        {
            services.AddScoped<IHoldingRepository, HoldingRepository>();
            services.AddScoped<ICompanyRepository, CompanyRepository>();
            services.AddScoped<IHoldingService, HoldingService>();
            services.AddScoped<ICompanyService, CompanyService>();
            return services;
        }

        /// <summary>ثبت ماژول اسلایدرها</summary>
        public static IServiceCollection AddSlidersModule(this IServiceCollection services)
        {
            services.AddScoped<ISliderRepository, SliderRepository>();
            services.AddScoped<ISliderService, SliderService>();
            return services;
        }

        /// <summary>پکیج‌ها و اشتراک‌های فروشنده</summary>
        public static IServiceCollection AddVendorPackagesModule(this IServiceCollection services)
        {
            services.AddScoped<IVendorPackageRepository, VendorPackageRepository>();
            services.AddScoped<IVendorPackageService, VendorPackageService>();
            return services;
        }

        /// <summary>
        /// ماژول شبکه‌های اجتماعی (جدا از Messaging/OTP).
        /// بات‌ها و وب‌هوک‌های موجود را جایگزین نمی‌کند؛ SiteBots را فقط به‌صورت پل خواندنی reuse می‌کند.
        /// </summary>
        public static IServiceCollection AddSocialMediaModule(this IServiceCollection services)
        {
            services.AddSingleton<ISocialCredentialProtector, AesSocialCredentialProtector>();

            services.AddScoped<ISocialAccountRepository, SocialAccountRepository>();
            services.AddScoped<ISocialCredentialRepository, SocialCredentialRepository>();
            services.AddScoped<ISocialTargetRepository, SocialTargetRepository>();
            services.AddScoped<ISocialConversationRepository, SocialConversationRepository>();
            services.AddScoped<ISocialInboundMessageRepository, SocialInboundMessageRepository>();
            services.AddScoped<ISocialPublishJobRepository, SocialPublishJobRepository>();
            services.AddScoped<ISocialDeliveryStatusRepository, SocialDeliveryStatusRepository>();
            services.AddScoped<ISocialOperationLogRepository, SocialOperationLogRepository>();
            services.AddScoped<ISocialPostRepository, SocialPostRepository>();

            services.AddScoped<ISocialOperationLogService, SocialOperationLogService>();
            services.AddScoped<ISocialAccountService, SocialAccountService>();
            services.AddScoped<ISocialInboxService, SocialInboxService>();
            services.AddScoped<ISocialPublishService, SocialPublishService>();
            services.AddScoped<ISocialScheduleService, SocialScheduleService>();
            services.AddScoped<ISocialWebhookIntakeService, SocialWebhookIntakeService>();
            services.AddScoped<ISocialProviderResolver, SocialProviderResolver>();

            // Providers (HttpClient typed) — parallel to Web Messaging channels; do not remove BaleMessagingChannel/TelegramMessagingChannel
            services.AddHttpClient<TelegramSocialProvider>();
            services.AddHttpClient<BaleSocialProvider>();
            services.AddTransient<InstagramSocialProvider>();

            services.AddTransient<ISocialProvider>(sp => sp.GetRequiredService<TelegramSocialProvider>());
            services.AddTransient<ISocialProvider>(sp => sp.GetRequiredService<BaleSocialProvider>());
            services.AddTransient<ISocialProvider>(sp => sp.GetRequiredService<InstagramSocialProvider>());

            services.AddTransient<ISocialPublisher>(sp => sp.GetRequiredService<TelegramSocialProvider>());
            services.AddTransient<ISocialPublisher>(sp => sp.GetRequiredService<BaleSocialProvider>());
            services.AddTransient<ISocialPublisher>(sp => sp.GetRequiredService<InstagramSocialProvider>());

            services.AddTransient<ISocialMessageSender>(sp => sp.GetRequiredService<TelegramSocialProvider>());
            services.AddTransient<ISocialMessageSender>(sp => sp.GetRequiredService<BaleSocialProvider>());
            services.AddTransient<ISocialMessageSender>(sp => sp.GetRequiredService<InstagramSocialProvider>());

            services.AddTransient<ISocialWebhookHandler>(sp => sp.GetRequiredService<TelegramSocialProvider>());
            services.AddTransient<ISocialWebhookHandler>(sp => sp.GetRequiredService<BaleSocialProvider>());
            services.AddTransient<ISocialWebhookHandler>(sp => sp.GetRequiredService<InstagramSocialProvider>());

            services.AddTransient<ISocialInboxReader>(sp => sp.GetRequiredService<TelegramSocialProvider>());
            services.AddTransient<ISocialInboxReader>(sp => sp.GetRequiredService<BaleSocialProvider>());
            services.AddTransient<ISocialInboxReader>(sp => sp.GetRequiredService<InstagramSocialProvider>());

            services.AddTransient<ISocialCommentService>(sp => sp.GetRequiredService<InstagramSocialProvider>());

            return services;
        }
    }
}
