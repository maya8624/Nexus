using FluentValidation;
using NexusPay.Application.Dtos;
using NexusPay.Application.Factories;
using NexusPay.Application.Interfaces;
using NexusPay.Application.Services;
using NexusPay.Application.Services.Identity;
using NexusPay.Infrastructure;
using NexusPay.Infrastructure.Interfaces;
using NexusPay.Infrastructure.Repositories;
using NexusPay.Network;
using NexusPay.Network.Interfaces;
using NexusPay.Network.Services;

namespace NexusPay.Application.Extensions
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
