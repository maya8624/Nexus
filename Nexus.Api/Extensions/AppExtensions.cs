using FluentValidation;
using Nexus.Api.Filters;
using Nexus.Application.Dtos;
using Nexus.Application.Dtos.Requests;
using Nexus.Application.Factories;
using Nexus.Application.Interfaces;
using Nexus.Application.Interfaces.Business;
using Nexus.Application.Interfaces.Repository;
using Nexus.Application.Services;
using Nexus.Application.Services.Identity;
using Nexus.Infrastructure;
using Nexus.Infrastructure.Repositories;
using Nexus.Network.Interfaces;
using Nexus.Network.Services;

namespace Nexus.Application.Extensions
{
    public static class AppExtensions
    {
        public static void AddApplicationServices(this IServiceCollection services)
        {
            services.AddHttpClient();
            services.AddControllers();

            services.AddScoped<IHttpClientService, HttpClientService>();
            services.AddScoped<IAiService, AiService>();
            services.AddScoped<IPayPalAuthService, PayPalAuthService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<IPropertyService, PropertyService>();
            services.AddScoped<IPayPalService, PayPalService>();
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<IPropertyRepository, PropertyRepository>();
            services.AddScoped<IPaymentRepository, PaymentRepository>();
            services.AddScoped<IRefundRepository, RefundRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IListingRepository, ListingRepository>();
            services.AddScoped<IAuthServiceFactory, AuthServiceFactory>();
            services.AddScoped<IAuthService, AuthGoogleService>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IUserContext, UserContext>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IInspectionBookingService, InspectionBookingService>();
            services.AddScoped<IInspectionBookingRepository, InspectionBookingRepository>();
            services.AddScoped<IInspectionSlotService, InspectionSlotService>();
            services.AddScoped<IInspectionSlotRepository, InspectionSlotRepository>();
            services.AddScoped<IDepositService, DepositService>();
            services.AddScoped<IDepositRepository, DepositRepository>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IAgentRepository, AgentRepository>();
            services.AddScoped<IValidator<CreateOrderRequest>, CreateOrderRequestValidator>();
            services.AddScoped<IValidator<CancelBookingRequest>, CancelBookingRequestValidator>();
            services.AddScoped<IValidator<RefundRequest>, RefundRequestValidator>();
            services.AddScoped<IValidator<OrderPaymentRequest>, OrderPaymentRequestValidator>();
            services.AddScoped<IValidator<CreateDepositRequest>, CreateDepositRequestValidator>();
            services.AddScoped<IValidator<EmailLoginRequest>, EmailLoginRequestValidator>();
            services.AddScoped<IValidator<RegisterRequest>, RegisterRequestValidator>();
            services.AddScoped<IValidator<ExternalLoginRequest>, ExternalLoginRequestValidator>();
            services.AddScoped<IValidator<ChatRequest>, ChatRequestValidator>();
            services.AddScoped<IValidator<InspectionBookingRequest>, InspectionBookingRequestValidator>();
            services.AddScoped<IValidator<InternalInspectionBookingRequest>, InternalInspectionBookingRequestValidator>();
            services.AddScoped<IValidator<InspectionSlotRequest>, InspectionSlotRequestValidator>();
            services.AddScoped<IValidator<PropertyQueryRequest>, PropertyQueryRequestValidator>();
            services.AddScoped<IPasswordHasherService, PasswordHasherService>();
            services.AddScoped<InternalApiKeyFilter>();
        }
    }
}
