using Carter;
using InnNou.Application.Abstractions;
using InnNou.Application.Common;
using InnNou.Infrastructure.Abstractions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;


var builder = WebApplication.CreateBuilder(args);


builder.Services.AddHttpContextAccessor();
builder.Services.AddCarter();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer {token}'"
    });

    options.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer"),
            new List<string>()
        }
    });
});


builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod()
              // Browsers only expose a small safelist of response headers to client-side JS by
              // default (Content-Disposition isn't one of them) — without this, every export/
              // template/file-download response's real filename is invisible to fetch() and the
              // frontend silently falls back to a generic name for every single entity.
              .WithExposedHeaders("Content-Disposition");
    });
});



builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,

            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
            )
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
{
    var defaultPermitLimit = builder.Configuration.GetValue("RateLimiting:Default:PermitLimit", 100);
    var defaultWindowSeconds = builder.Configuration.GetValue("RateLimiting:Default:WindowSeconds", 60);
    var authPermitLimit = builder.Configuration.GetValue("RateLimiting:Auth:PermitLimit", 5);
    var authWindowSeconds = builder.Configuration.GetValue("RateLimiting:Auth:WindowSeconds", 60);

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Applies to every endpoint automatically — no per-endpoint opt-in needed. Partitioned by
    // authenticated user (JWT sub) when present, else by IP. Composes with named policies below
    // (a request must satisfy both), so /auth gets an additional, stricter limit on top of this.
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var partitionKey = httpContext.User.Identity?.IsAuthenticated == true
            ? $"user:{httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value}"
            : $"ip:{httpContext.Connection.RemoteIpAddress}";

        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = defaultPermitLimit,
            Window = TimeSpan.FromSeconds(defaultWindowSeconds),
            QueueLimit = 0,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        });
    });

    // Stricter, IP-partitioned policy applied only to the /auth route group (brute-force surface —
    // login attempts are anonymous, so there's no user identity to partition by yet).
    options.AddPolicy("auth", httpContext =>
    {
        var partitionKey = $"ip:{httpContext.Connection.RemoteIpAddress}";

        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = authPermitLimit,
            Window = TimeSpan.FromSeconds(authWindowSeconds),
            QueueLimit = 0,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        });
    });

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";

        var response = ApiResponse<object>.FailureResponse(
            ErrorCodes.RateLimitExceeded,
            "Too many requests. Please slow down and try again shortly.",
            StatusCodes.Status429TooManyRequests);

        await context.HttpContext.Response.WriteAsJsonAsync(response, cancellationToken);
    };
});


var app = builder.Build();



app.UseExceptionHandler(app =>
{
    app.Run(async context =>
    {
        var error = context.Features.Get<IExceptionHandlerFeature>()?.Error;

        var (code, message, statusCode) = error switch
        {
            ApiException apiEx => (apiEx.Code, apiEx.Message, apiEx.StatusCode),
            BadHttpRequestException => (ErrorCodes.InvalidRequest, "Invalid request format", 400),
            _ => (ErrorCodes.UnhandledError, "An unexpected error occurred.", 500)
        };

        context.Response.StatusCode = statusCode;

        var response = ApiResponse<object>.FailureResponse(code, message, statusCode);

        await context.Response.WriteAsJsonAsync(response);
    });
});



app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseCors();

// Supplier logos — served straight off local disk (see CLAUDE.md's "Supplier logo" note),
// deliberately unauthenticated (same as any other public brand-image asset an <img> tag needs
// to load without attaching a JWT). Physical folder must match LocalSupplierLogoStorage exactly.
var supplierLogosPath = SupplierLogoPaths.ResolvePhysicalBasePath(builder.Configuration);
Directory.CreateDirectory(supplierLogosPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(supplierLogosPath),
    RequestPath = SupplierLogoPaths.PublicUrlPrefix
});

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

app.MapCarter();

app.MapGet("/ping", () => "pong")
    .WithName("Ping")
    .Produces<string>(200);

app.Run();