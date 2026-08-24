using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using RecruitmentAPI.Data;
using RecruitmentAPI.Models;
using RecruitmentAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// MVC + API controllers. AddControllersWithViews hỗ trợ cả Razor Views và các API hiện có.
builder.Services.AddControllersWithViews();

// Swagger cho phần Web API cũ.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Recruitment API",
        Version = "v1",
        Description = "API quản lý ứng tuyển và xử lý hồ sơ tuyển dụng (SQL thuần, không dùng EF Core)."
    });
});

// Database + business services.
builder.Services.AddScoped<SqlConnectionFactory>();
builder.Services.AddScoped<JobService>();
builder.Services.AddScoped<CandidateService>();
builder.Services.AddScoped<ApplicationService>();
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

// Cookie Authentication cho giao diện MVC.
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
        options.Cookie.Name = "Recruitment.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

var app = builder.Build();

// Static files cho CSS/frontend MVC.
app.UseStaticFiles();
app.UseRouting();

// Authentication phải đứng trước Authorization.
app.UseAuthentication();
app.UseAuthorization();

// Swagger vẫn được giữ để test backend API.
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Recruitment API v1");
    options.RoutePrefix = "swagger";
});

// Route MVC mặc định: mở website sẽ đi tới trang đăng nhập.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

// Attribute routing cho /api/jobs, /api/candidates, /api/applications.
app.MapControllers();

app.Run();
