using FluentValidation;
using NexusPay.Application.Dtos;
using NexusPay.Application.Interfaces;
using NexusPay.Application.Services;
using NexusPay.Infrastructure.Interfaces;
using NexusPay.Infrastructure.Repositories;
using NexusPay.Network.Interfaces;
using NexusPay.Network.Services;
using NexusPay.Infrastructure;

namespace NexusPay.Application.Extensions
{
    public static class ServiceExtensions
    {   
        public static void AddServices(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(
                    builder =>
                    {
                        builder.WithOrigins(["http://localhost:5173", "http://127.0.0.1:5500", "https://localhost:7289"])
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                    });
            });

            services.AddHttpClient();
            services.AddControllers();

            services.AddScoped<IHttpClientService, HttpClientService>();
            services.AddScoped<IPayPalAuthService, PayPalAuthService>();
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<IPaymentService, PaymentServcie>();
            services.AddScoped<IPayPalService, PayPalService>();
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<IPaymentRepository, PaymentRepository>();
            services.AddScoped<IRefundRepository, RefundRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IValidator<CreateOrderRequest>, CreateOrderRequestValidator>();
            services.AddScoped<IValidator<RefundRequest>, RefundRequestValidator>();
            services.AddScoped<IValidator<OrderPaymentRequest>, OrderPaymentRequestValidator>();
        }
    }
}
