using BrinksAPI.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
ConfigurationManager configuration = builder.Configuration;

// Add services to the container.
#region Entity Framework
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(configuration.GetConnectionString("ConnStr")));
#endregion

#region Identity
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();
#endregion

#region Authentication
builder.Services.AddAuthentication(options =>
{
options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
#endregion

#region JWT
        .AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidAudience = configuration["JWT:ValidAudience"],
        ValidIssuer = configuration["JWT:ValidIssuer"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:Secret"]))
    };
});
#endregion

#region Swagger UI
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddSwaggerGen(option =>
{
    option.CustomSchemaIds(type => type.ToString());
    option.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "BRINKS API",
        Version = "v1",
        Description = "An API to perform Cargowise operations",
        TermsOfService = new Uri("https://www.cenglobal.com/privacy-policy/"),
        Contact = new OpenApiContact
        {
            Name = "Cenglobal",
            Email = "Support@cenglobal.com",
            Url = new Uri("https://www.cenglobal.com/contact/"),
        },
        License = new OpenApiLicense
        {
            Name = "",
            Url = new Uri("https://www.cenglobal.com/about-us/"),
        }
    });
    option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter a valid token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });
    option.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type=ReferenceType.SecurityScheme,
                    Id="Bearer"
                }
            },
            new string[]{}
        }
    });


    // Set the comments path for the Swagger JSON and UI.
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    option.IncludeXmlComments(xmlPath);
});
#endregion

builder.Services.AddEndpointsApiExplorer();

#region Enum Converter
builder.Services.AddControllers().AddJsonOptions(options =>
{
options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
#endregion

#region Reading Cargowise Configuaration From Appsettings.json
builder.Services.AddSingleton<BrinksAPI.Interfaces.IConfigManager, BrinksAPI.Services.Config>(); 
#endregion

var app = builder.Build();

#region For Development
//if (app.Environment.IsDevelopment())
//{
//} 
#endregion

#region Swagger 
app.UseSwagger();
app.UseStaticFiles();
app.UseSwaggerUI(options =>
{
    options.InjectStylesheet("/swagger-ui/css/custom.css");
    options.InjectJavascript("/swgger-ui/js/custom.js");
}); 
#endregion

app.UseHttpsRedirection();

#region Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization(); 
#endregion

app.MapControllers();

#region Error Handling
//app.Use(async (context, next) =>
//{
//    await next(); 
//    if (context.Response.StatusCode == 404)
//    {
//        await context.Response.WriteAsync("tes");
//        await next();
//    }
//});

//app.UseStatusCodePages();
#endregion

app.Run();
