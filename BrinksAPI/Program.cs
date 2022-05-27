using BrinksAPI.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
ConfigurationManager configuration = builder.Configuration;

// Add services to the container.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AnyOrigin", builder =>
    {
        builder
            .AllowAnyOrigin()
            .AllowAnyMethod();
    });
});
#region Entity Framework
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(configuration.GetConnectionString("ConnStr")));
#endregion

#region Authentication Basic
builder.Services.AddAuthentication("BasicAuthentication")
        .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>
        ("BasicAuthentication", null);
builder.Services.AddAuthorization();
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
    
    #region Basic
    option.AddSecurityDefinition("basic", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "basic",
        In = ParameterLocation.Header,
        Description = "Basic Authorization header using the Bearer scheme."
    });
    option.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type=ReferenceType.SecurityScheme,
                    Id="basic"
                }
            },
            new string[]{}
        }
    }); 
    #endregion

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

#region Reading Cargowise Configuaration From Appsettings.json (Interface)
builder.Services.AddSingleton<BrinksAPI.Interfaces.IConfigManager, BrinksAPI.Services.Config>(); 
#endregion

var app = builder.Build();
app.UseCors("AnyOrigin");
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
    options.DefaultModelsExpandDepth(-1);
    options.InjectStylesheet("/swagger-ui/css/custom.css");
    options.InjectJavascript("/swagger-ui/js/custom.js");
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
