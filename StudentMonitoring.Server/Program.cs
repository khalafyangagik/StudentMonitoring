
using Npgsql;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using StudentMonitoring.Server.DTOs;
using StudentMonitoring.Server.Repositories;
using StudentMonitoring.Server.Services;

namespace StudentMonitoring.Server;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // The Windows Event Log provider can throw when its source is unavailable,
        // which otherwise may take down startup while reporting a harmless warning.
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.AddDebug();

        var dataProtection = builder.Services.AddDataProtection();
        var keyPath = builder.Configuration["DataProtection:KeysPath"];
        if (string.IsNullOrWhiteSpace(keyPath) && builder.Environment.IsDevelopment())
            keyPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtection-Keys-Dev");
        if (!string.IsNullOrWhiteSpace(keyPath))
        {
            Directory.CreateDirectory(keyPath);
            dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keyPath));
            if (OperatingSystem.IsWindows() && !builder.Environment.IsDevelopment())
                dataProtection.ProtectKeysWithDpapi();
        }

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = "StudentMonitoring.Auth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = true;
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };
                options.Events.OnValidatePrincipal = async context =>
                {
                    var email = context.Principal?.FindFirstValue(ClaimTypes.Email);
                    var authService = context.HttpContext.RequestServices.GetRequiredService<AuthService>();
                    var account = string.IsNullOrWhiteSpace(email) ? null
                        : await authService.FindByEmailAsync(email, context.HttpContext.RequestAborted);
                    var claimedRole = context.Principal?.FindFirstValue(ClaimTypes.Role);
                    if (account is null || !account.IsActive || !account.EmailVerified ||
                        !string.Equals(account.Role, claimedRole, StringComparison.OrdinalIgnoreCase))
                    {
                        context.RejectPrincipal();
                        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    }
                };
            });
        builder.Services.AddAuthorization();

        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Database connection string 'ConnectionStrings:DefaultConnection' is missing. " +
                "Set it with .NET User Secrets or an environment variable.");
        }

        builder.Services.AddSingleton(NpgsqlDataSource.Create(connectionString));
        builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        builder.Services.AddScoped<IDepartmentService, DepartmentService>();
        builder.Services.AddScoped<ICourseRepository, CourseRepository>();
        builder.Services.AddScoped<ICourseService, CourseService>();
        builder.Services.AddScoped<ISubjectRepository, SubjectRepository>();
        builder.Services.AddScoped<ISubjectService, SubjectService>();
        builder.Services.AddScoped<IGroupRepository, GroupRepository>();
        builder.Services.AddScoped<IGroupService, GroupService>();
        builder.Services.AddScoped<IStudentRepository, StudentRepository>();
        builder.Services.AddScoped<IStudentService, StudentService>();
        builder.Services.AddScoped<IAttendanceRepository, AttendanceRepository>();
        builder.Services.AddScoped<IAttendanceService, AttendanceService>();
        builder.Services.AddScoped<ISessionRepository, SessionRepository>();
        builder.Services.AddScoped<ISessionService, SessionService>();
        builder.Services.AddScoped<IRoleRepository, RoleRepository>();
        builder.Services.AddScoped<IRoleService, RoleService>();
        builder.Services.AddScoped<IStudentPortalRepository, StudentPortalRepository>();
        builder.Services.AddScoped<StudentPortalService>();
        builder.Services.AddScoped<ILecturerPortalRepository, LecturerPortalRepository>();
        builder.Services.AddScoped<ILecturerGradeRepository, LecturerGradeRepository>();
        builder.Services.AddScoped<LecturerPortalService>();
        builder.Services.AddScoped<ISuperAdminRepository, SuperAdminRepository>();
        builder.Services.AddScoped<SuperAdminService>();
        builder.Services.AddScoped<InvitationRepository>();
        builder.Services.AddScoped<UserInvitationService>();
        builder.Services.AddScoped<SuperAdminBootstrapper>();
        builder.Services.AddScoped<IAuthRepository, AuthRepository>();
        builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
        builder.Services.AddScoped<AuthService>();
        builder.Services.AddSingleton<Microsoft.AspNetCore.Identity.PasswordHasher<AuthPasswordSubject>>();

        var app = builder.Build();

        await using (var scope = app.Services.CreateAsyncScope())
            await scope.ServiceProvider.GetRequiredService<SuperAdminBootstrapper>()
                .PromoteConfiguredAccountAsync(CancellationToken.None);

        app.UseDefaultFiles();
        app.UseStaticFiles();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers().RequireAuthorization();
        app.MapFallbackToFile("/index.html");

        app.Run();
    }
}
