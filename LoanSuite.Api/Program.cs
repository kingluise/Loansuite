using LoanSuite.Infrastructure.Data;
using LoanSuite.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// -------------------- Database --------------------
// Configure the database connection using SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// -------------------- Services --------------------
// Register all application services for dependency injection
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<LoanService>();
builder.Services.AddScoped<PaymentService>(); // New service registration
builder.Services.AddScoped<DashboardService>();

// -------------------- Controllers --------------------
// Add controllers to the service container
builder.Services.AddControllers();

// -------------------- CORS Configuration --------------------
// Add CORS services to the container
// This defines a named policy that allows requests from your frontend origin
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontEndOrigin",
        builder =>
        {
            builder.WithOrigins("http://127.0.0.1:5500")
                   .AllowAnyHeader()
                   .AllowAnyMethod();
        });
});

// -------------------- Swagger with JWT --------------------
// Configure Swagger for API documentation and UI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "LoanSuite API", Version = "v1" });

    // Define the security scheme for JWT authentication in Swagger
    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter 'Bearer {your JWT token}'",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = "Bearer"
        }
    };

    // Add the security definition and requirement to Swagger
    c.AddSecurityDefinition("Bearer", securityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, Array.Empty<string>() }
    });
});

// -------------------- JWT Authentication --------------------
var jwt = builder.Configuration.GetSection("JwtSettings");
var keyBytes = Encoding.UTF8.GetBytes(jwt["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured"));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // Set to true in production
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwt["Issuer"],
        ValidAudience = jwt["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
        ClockSkew = TimeSpan.Zero,

        // CRITICAL FIX: Map the role claim from the JWT payload to ASP.NET Identity
        // Your JWT uses the full URI claim type, not "role".
        NameClaimType = "sub", // Use "sub" or "email" for the user identifier
        RoleClaimType = ClaimTypes.Role // Correctly maps to "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
    };
});

// -------------------- Authorization --------------------
// Add authorization services
builder.Services.AddAuthorization();

var app = builder.Build();

// -------------------- Middleware --------------------
// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Use the CORS policy here. It must be called before UseAuthorization().
app.UseCors("AllowFrontEndOrigin");

// The order of these two lines is critical.
// Authentication must come before Authorization.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
