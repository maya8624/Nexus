using FluentValidation;
using Nexus.Application.Dtos;
using Nexus.Application.Factories;
using Nexus.Application.Interfaces;
using Nexus.Application.Services;
using Nexus.Application.Services.Identity;
using Nexus.Infrastructure;
using Nexus.Infrastructure.Interfaces;
using Nexus.Infrastructure.Repositories;
using Nexus.Network;
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
            services.AddScoped<IPayPalAuthService, PayPalAuthService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<IPaymentService, PaymentServcie>();
            services.AddScoped<IPayPalService, PayPalService>();
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<IPaymentRepository, PaymentRepository>();
            services.AddScoped<IRefundRepository, RefundRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IAuthServiceFactory, AuthServiceFactory>();
            services.AddScoped<IAuthService, AuthGoogleService>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IUserContext, UserContext>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IValidator<CreateOrderRequest>, CreateOrderRequestValidator>();
            services.AddScoped<IValidator<RefundRequest>, RefundRequestValidator>();
            services.AddScoped<IValidator<OrderPaymentRequest>, OrderPaymentRequestValidator>();
            services.AddScoped<IValidator<EmailLoginRequest>, EmailLoginRequestValidator>();
            services.AddScoped<IValidator<ExternalLoginRequest>, ExternalLoginRequestValidator>();
            services.AddScoped<IPasswordHasherService, PasswordHasherService>();
        }
    }
}
