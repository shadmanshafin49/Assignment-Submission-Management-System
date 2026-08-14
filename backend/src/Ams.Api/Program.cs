using System.Text;
using System.Text.Json.Serialization;
using Ams.Api.Filters;
using Ams.Api.Middleware;
using Ams.Api.Security;
using Ams.Application.Abstractions;
using Ams.Application.Validators;
using Ams.Infrastructure;
using Ams.Infrastructure.Persistence;
using Ams.Infrastructure.Security;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------- logging
builder.Host.UseSerilog((context, services, config) => config
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/ams-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7));

// ------------------------------------------------------------------ application
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

builder.Services.AddControllers(options => options.Filters.Add<ValidationFilter>())
    .AddJsonOptions(options =>
    {
        // Enums travel as their names ("Published"), not ordinals, so the API stays readable
        // and the frontend does not have to mirror numeric values.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();

// ----------------------------------------------------------------------- auth
var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
var jwtKey = jwtSection["Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
{
    throw new InvalidOperationException(
        "Jwt:Key is missing or shorter than 32 characters. Copy .env.example to .env and set "
        + "JWT__KEY, or set it in user-secrets. See the README for how to generate one.");
}

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),

            // Default is 5 minutes of leeway, which would let a just-expired token through.
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// ------------------------------------------------------------------------ cors
const string CorsPolicy = "FrontendPolicy";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:3000"];

builder.Services.AddCors(options =>
    options.AddPolicy(CorsPolicy, policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()));

// --------------------------------------------------------------------- swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Assignment & Submission Management API",
        Version = "v1",
        Description =
            "গাজীপুর ক্যান্টনমেন্ট বোর্ড উচ্চ বিদ্যালয় — role-based assignment and submission "
            + "management for classes ষষ্ঠ–অষ্টম on the NCTB curriculum.\n\n"
            + "**Getting started:** call `POST /api/auth/login` with a demo account, copy the "
            + "`accessToken` from the response, then click **Authorize** and paste it to call "
            + "the protected endpoints.\n\n"
            + "Demo accounts — `admin@gcbhs.edu.bd` / `Admin@123`, "
            + "`rejaul.karim@gcbhs.edu.bd` / `Teacher@123`, "
            + "`c6r01@student.gcbhs.edu.bd` / `Student@123`."
    });

    // Adds the Authorize button and sends the raw JWT as `Authorization: Bearer <token>`.
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste only the access token — Swagger adds the 'Bearer ' prefix."
    });

    options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
    {
        { new OpenApiSecuritySchemeReference("Bearer"), new List<string>() }
    });

    var xmlPath = Path.Combine(AppContext.BaseDirectory, "Ams.Api.xml");
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

// ----------------------------------------------------------------- middleware
// Ahead of the exception handler: it sets its headers through OnStarting, which runs at
// flush time, so they survive the Response.Clear() that turns an exception into ProblemDetails.
app.UseMiddleware<SecurityHeadersMiddleware>();

// Registered first among the handlers so it also catches failures thrown further down.
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSerilogRequestLogging();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "AMS API v1");
    options.DocumentTitle = "AMS API";
});

app.UseCors(CorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTimeOffset.UtcNow }))
    .AllowAnonymous()
    .WithTags("Health");

// ------------------------------------------------------- migrate + seed on boot
// Applying migrations at startup means the evaluator never has to run `dotnet ef` by hand —
// `docker compose up` is enough to get a populated database.
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Applying database migrations...");
        await db.Database.MigrateAsync();

        var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
        await seeder.SeedAsync();
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "Database migration or seeding failed");
        throw;
    }
}

app.Run();

/// <summary>Exposed so the integration tests can drive the API through WebApplicationFactory.</summary>
public partial class Program;
