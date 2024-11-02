using Microsoft.OpenApi.Models;

namespace API.Extensions
{
    public static class SwaggerExtension
    {
        public static IServiceCollection AddSwaggerExtension(this IServiceCollection service, IConfiguration configuration)
        {
            service.AddSwaggerGen(c =>
            {
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: Bearer 1safsfsdfdfd"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement {
                {
                    new OpenApiSecurityScheme {
                        Reference = new OpenApiReference {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    new string[] {}
                }
            });
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Example Api",
                    Version = "v1",
                    Description = "Description example",
                    Contact = new OpenApiContact
                    {
                        Name = "Example contact",
                        Email = "example@gmail.com",
                        Url = new Uri("https://example.com/contact")
                    }
                });
            });

            return service;
        }
    }
}
