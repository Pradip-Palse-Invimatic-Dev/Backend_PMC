using FluentEmail.Core;
using FluentEmail.MailKitSmtp;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MyWebApp.Api.Common;
using MyWebApp.Api.Services;
using MyWebApp.Data;
using MyWebApp.Mappings;
using MyWebApp.Models;
using System.Text;


QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Configure PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure Email
var smtpConfig = builder.Configuration.GetSection("SmtpClient");
builder.Services
    .AddFluentEmail(smtpConfig["User"], smtpConfig["FromName"])
    .AddMailKitSender(new SmtpClientOptions
    {
        Server = smtpConfig["Server"],
        Port = int.Parse(smtpConfig["Port"] ?? "587"),
        User = smtpConfig["User"],
        Password = smtpConfig["Password"],
        RequiresAuthentication = true,
        SocketOptions = (MailKit.Security.SecureSocketOptions)int.Parse(smtpConfig["SocketOptions"] ?? "3")
    });

LoadConfiguration(builder);
LoadCORSConfiguration(builder);
LoadIdentityConfiguration(builder.Services);
LoadAuthenticationConfiguration(builder.Services);

// Services
builder.Services.AddAutoMapper(options => options.AddProfile<MappingProfile>());
builder.Services.AddTransient<OtpAttemptService>();
builder.Services.AddTransient<EmailService>();
builder.Services.AddTransient<FileService>();
builder.Services.AddTransient<JWTService>();
builder.Services.AddTransient<AuthService>();
builder.Services.AddTransient<PdfService>();
builder.Services.AddTransient<PaymentService>();
builder.Services.AddTransient<ApplicationService>();

// BillDesk Payment Services
builder.Services.AddScoped<IBillDeskConfigService, BillDeskConfigService>();
builder.Services.AddScoped<IBillDeskPaymentService, BillDeskPaymentService>();
builder.Services.AddScoped<IPluginContextService, PluginContextService>();

// Certificate Generation Service (Using iText7 instead of QuestPDF)
builder.Services.AddScoped<ISECertificateGenerationService, SECertificateGenerationService>();

// Challan Service
builder.Services.AddScoped<IChallanService, ChallanService>();

// Upload File Service
// builder.Services.AddScoped<IUploadFileService, UploadFileService>();

// // Font Manager Service
// builder.Services.AddSingleton<IFontManager, FontManager>();

// Ensure wwwroot path is set up
// builder.Services.AddSingleton<IWebHostEnvironment>(sp => sp.GetRequiredService<IWebHostEnvironment>());

// Configure HttpClient for HSM API calls
builder.Services.AddHttpClient("HSM_OTP", client =>
{
    var otpBaseUrl = builder.Configuration.GetValue<string>("HSM:OtpBaseUrl") ?? "http://210.212.188.44:8001/jrequest/";
    client.BaseAddress = new Uri(otpBaseUrl);
    client.DefaultRequestHeaders.Add("User-Agent", "PMC-Application-Service");
    var timeout = builder.Configuration.GetValue<string>("HSM:Timeout");
    if (!string.IsNullOrEmpty(timeout) && TimeSpan.TryParse(timeout, out var timeoutSpan))
    {
        client.Timeout = timeoutSpan;
    }
});

builder.Services.AddHttpClient("HSM_SIGN", client =>
{
    var signBaseUrl = builder.Configuration.GetValue<string>("HSM:SignBaseUrl") ?? "http://210.212.188.35:8080/emSigner/";
    client.BaseAddress = new Uri(signBaseUrl);
    client.DefaultRequestHeaders.Add("User-Agent", "PMC-Application-Service");
    var timeout = builder.Configuration.GetValue<string>("HSM:Timeout");
    if (!string.IsNullOrEmpty(timeout) && TimeSpan.TryParse(timeout, out var timeoutSpan))
    {
        client.Timeout = timeoutSpan;
    }
});

// Add API Controllers
builder.Services.AddControllers();

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PMC Application API",
        Version = "v1",
        Description = "PMC Registration Management System API"
    });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "JWT Authentication",
        Description = "Enter JWT Bearer token **_only_**",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme
        }
    };
    c.AddSecurityDefinition(securityScheme.Reference.Id, securityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            securityScheme, new string[] { }
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// Enable Swagger middleware
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "PMC Application API v1");
    options.RoutePrefix = string.Empty;
    options.DocumentTitle = "PMC Application API";
});

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseCors("corsOrigins");

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

Console.WriteLine("=== Application Starting ===");
Console.WriteLine("Swagger UI: http://localhost:5000 or https://localhost:5001");
Console.WriteLine("=========================");

app.Run();

// ==================== HELPER METHODS ====================

void LoadConfiguration(WebApplicationBuilder builder)
{
    var jwtSection = builder.Configuration.GetSection("Jwt");
    builder.Services.Configure<JWTSettings>(jwtSection);

    var jwtSettings = jwtSection.Get<JWTSettings>();
    if (string.IsNullOrEmpty(jwtSettings?.Key) ||
        string.IsNullOrEmpty(jwtSettings?.Issuer) ||
        string.IsNullOrEmpty(jwtSettings?.Audience))
    {
        throw new InvalidOperationException("JWT settings are not properly configured");
    }
}

void LoadCORSConfiguration(WebApplicationBuilder builder)
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy(name: "corsOrigins", policy =>
        {
            var origins = builder.Configuration["CORSSettings:Origins"]?.Split(",") ?? new[] { "http://localhost:5173" };
            policy.WithOrigins(origins)
                .AllowCredentials()
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
    });
}

void LoadIdentityConfiguration(IServiceCollection services)
{
    services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredLength = 6;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();
}

void LoadAuthenticationConfiguration(IServiceCollection services)
{
    services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured");
        var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer is not configured");
        var jwtAudience = builder.Configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience is not configured");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateLifetime = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });
}