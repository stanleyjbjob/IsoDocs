using IsoDocs.Api.Middleware;
using IsoDocs.Application;
using IsoDocs.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// 註冊各層 DI
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// API 元件
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "IsoDocs API",
        Version = "v1",
        Description = "ISO 文件管理系統 API"
    });
});

// 後續將於 issue #2 加入 Microsoft.Identity.Web 認證設定
// builder.Services.AddAuthentication().AddMicrosoftIdentityWebApi(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .WithOrigins(
                  builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                      ?? new[] { "http://localhost:5173" });
    });
});

builder.Services.AddProblemDetails();

var app = builder.Build();

// 全域例外處理（必須在最前）
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();

// 後續會啟用
// app.UseAuthentication();
// app.UseAuthorization();

app.MapControllers();

app.Run();

/// <summary>
/// 提供給整合測試使用的 Program 進入點。
/// </summary>
public partial class Program { }
