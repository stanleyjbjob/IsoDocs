using System.Reflection;
using FluentValidation;
using IsoDocs.Application.Common.Behaviors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace IsoDocs.Application;

/// <summary>
/// Application 層的 DI 註冊入口。在 Program.cs 中以 <c>builder.Services.AddApplication()</c> 呼叫。
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // 註冊 MediatR 並掃描本組件中的所有 Handler
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);

            // Pipeline 順序：Logging → Validation → Handler
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        // 註冊本組件中的所有 FluentValidation 驗證器
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
