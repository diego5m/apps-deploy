using System.Text;
using Backend.Component.Application.Internal.CommandServices;
using Backend.Component.Application.Internal.QueryServices;
using Backend.Component.Domain.Repositories;
using Backend.Component.Domain.Services;
using Backend.Component.Infrastructure.Persistence.EFC.Repositories;
using Backend.IAM.Application.ACL.Services;
using Backend.IAM.Application.Internal.CommandServices;
using Backend.IAM.Application.Internal.OutboundServices;
using Backend.IAM.Application.Internal.QueryServices;
using Backend.IAM.Domain.Repositories;
using Backend.IAM.Domain.Services;
using Backend.IAM.Infrastructure.Hashing.BCrypt.Services;
using Backend.IAM.Infrastructure.Persistence.EFC.Repositories;
using Backend.IAM.Infrastructure.Pipeline.Middleware.Components;
using Backend.IAM.Infrastructure.Pipeline.Middleware.Extensions;
using Backend.IAM.Infrastructure.Tokens.JWT.Configuration;
using Backend.IAM.Infrastructure.Tokens.JWT.Services;
using Backend.IAM.Interfaces.ACL;
using Backend.Interaction.Application.Internal.CommandServices;
using Backend.Interaction.Application.Internal.QueryServices;
using Backend.Interaction.Domain.Repositories;
using Backend.Interaction.Domain.Services;
using Backend.Interaction.Infrastructure.Persistence.EFC.Repositories;
using Backend.Shared.Domain.Repositories;
using Backend.Shared.Infrastructure.Interfaces.ASAP.Configuration;
using Backend.Shared.Infrastructure.Persistence.EFC.Configuration;
using Backend.Shared.Infrastructure.Persistence.EFC.Repositories;
using Backend.TechnicalSupport;
using Backend.TechnicalSupport.Application.Internal.CommandServices;
using Backend.TechnicalSupport.Application.Internal.QueryServices;
using Backend.TechnicalSupport.Domain.Repositories;
using Backend.TechnicalSupport.Domain.Services;
using Backend.TechnicalSupport.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;


// Cart
using Backend.Orders;
using Backend.Orders.Application.Internal.CommandServices;
using Backend.Orders.Application.Internal.QueryServices;
using Backend.Orders.Domain.Repositories;
using Backend.Orders.Domain.Services;
using Backend.Orders.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
//configure Lower Case URLs
builder.Services.AddRouting(options => options.LowercaseUrls = true);

// Configure Kebab Case Route Naming Convention
builder.Services.AddControllers(options => options.Conventions.Add(new KebabCaseRouteNamingConvention()));

//builder.Services.AddSwaggerGen(options => options.EnableAnnotations());

/////////////////////////Begin Database Configuration/////////////////////////
// Add DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Verify Database Connection string
if (connectionString is null)
{
    throw new InvalidOperationException("Connection string not found");
}

// Configure Database Context and Logging Levels
builder.Services.AddDbContext<AppDbContext>(
    options =>
    {
        options.UseMySQL(connectionString);
    });


// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1",
        new OpenApiInfo
        {
            Title = "PC Master Platform API",
            Version = "v1.2",
            Description = "PC Master",
            TermsOfService = new Uri("https://tp-pcmaster.web.app/home"),
            Contact = new OpenApiContact
            {
                Name   = "PCMaster",
                Email = "contact@pcmaster.com"
            },
            License = new OpenApiLicense
            {
                Name = "Apache 2.0",
                Url  = new Uri("https://www.apache.org/licenses/LICENSE-2.0.html")
            }
        });
    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            In = ParameterLocation.Header,
            Description = "Please enter token",
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            BearerFormat = "JWT",
            Scheme = "bearer"
        });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Id = "Bearer",
                    Type = ReferenceType.SecurityScheme
                }
            },
            Array.Empty<string>()
        }
    });
    options.EnableAnnotations();
});


// Add CORS Policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllPolicy",
        policy => 
            policy.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader());
});

// Configure Dependency Injection

// Shared Bounded Context Injection Configuration
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Bounded Context Injection Configuration for Business

//Technical Support BC
builder.Services.AddScoped<ITechnicalSupportRepository, TechnicalSupportRepository>();
builder.Services.AddScoped<ITechnicalSupportQueryService, TechnicalSupportQueryService>();
builder.Services.AddScoped<ITechnicalSupportCommandService, TechnicalSupportCommandService>();
builder.Services.AddScoped<ITechnicianRepository, TechnicianRepository>();
builder.Services.AddScoped<ITechnicianQueryService, TechnicianQueryService>();
builder.Services.AddScoped<ITechnicianCommandService, TechnicianCommandService>();

//Interaction BC
builder.Services.AddScoped<IComponentReviewRepository, ComponentReviewRepository>();
builder.Services.AddScoped<IComponentReviewQueryService, ComponentReviewQueryService>();
builder.Services.AddScoped<IComponentReviewCommandService, ComponentReviewCommandService>();

builder.Services.AddScoped<ITechnicalSupportReviewRepository, TechnicalSupportReviewRepository>();
builder.Services.AddScoped<ITechnicalSupportReviewQueryService, TechnicalSupportReviewQueryService>();
builder.Services.AddScoped<ITechnicalSupportReviewCommandService, TechnicalSupportReviewCommandService>();

builder.Services.AddScoped<IWishlistRepository, WishlistRepository>();
builder.Services.AddScoped<IWishlistQueryService, WishlistQueryService>();
builder.Services.AddScoped<IWishlistCommandService, WishlistCommandService>();


//Component BC
builder.Services.AddScoped<IComponentQueryService, ComponentQueryService>();
builder.Services.AddScoped<IComponentQueryService, ComponentQueryService>();
builder.Services.AddScoped<IComponentCommandService, ComponentCommandService>();
//ERROR IN COMPONENT REPOSITORY: NOT ACCESS
builder.Services.AddScoped<IComponentRepository, ComponentRepository>();
builder.Services.AddScoped<IComponentQueryService, ComponentQueryService>();
builder.Services.AddScoped<IComponentCommandService, ComponentCommandService>();

// Injection for Cart
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<ICartQueryService, CartQueryService>();
builder.Services.AddScoped<ICartCommandService, CartCommandService>();

// IAM Bounded Context Dependency Injection Configuration

// TokenSettings Configuration
builder.Services.Configure<TokenSettings>(builder.Configuration.GetSection("TokenSettings"));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserCommandService, UserCommandService>();
builder.Services.AddScoped<IUserQueryService, UserQueryService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IHashingService, HashingService>();
builder.Services.AddScoped<IIamContextFacade, IamContextFacade>();

// Configura la autenticación JWT
/*
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["TokenSettings:Issuer"],  // Reemplaza si es necesario
            ValidAudience = builder.Configuration["TokenSettings:Audience"], // Reemplaza si es necesario
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["TokenSettings:Secret"]))
        };
    });
*/
/*
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Jwt:Authority"];
        options.Audience = builder.Configuration["Jwt:Audience"];
    });
*/


/////////////////////////End Database Configuration/////////////////////////
var app = builder.Build();

// Verify Database Objects are created
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();
    context.Database.EnsureCreated();
}

//app.UseMiddleware<RequestAuthorizationMiddleware>();

// Configure the HTTP request pipeline.

app.UseSwagger();
app.UseSwaggerUI();


// Apply CORS Policy
app.UseCors("AllowAllPolicy");

// Add Authorization Middleware to the Pipeline
app.UseRequestAuthorization();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();