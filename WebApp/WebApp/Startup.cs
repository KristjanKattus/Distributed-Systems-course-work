using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using BLL.App;
using Contracts.BLL.App;
using Contracts.DAL.App;
using Contracts.DAL.App.Repositories;
using DAL.App.EF;
using DAL.App.EF.AppDataInit;
using DAL.App.EF.Repositories;
using Domain.App.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.SwaggerGen;
using WebApp.Helpers;

namespace WebApp
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(
                    Configuration.GetConnectionString("DefaultConnection"))
                    .EnableDetailedErrors()
                    .EnableSensitiveDataLogging())
                
                ;
            services.AddDatabaseDeveloperPageExceptionFilter();
      
            services.AddScoped<IAppUnitOfWork, AppUnitOfWork>();
            services.AddScoped<IAppBLL, AppBLL>();
            services.AddScoped<IGameTeamRepository, GameTeamRepository>();
            services.AddScoped<IGameEventRepository, GameEventRepository>();
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
            services
                .AddAuthentication()
                .AddCookie(options =>
                    {
                        options.SlidingExpiration = true;
                    }
                    )
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = false;
                    options.SaveToken = true;
                    options.TokenValidationParameters = new TokenValidationParameters()
                    {
                        ValidIssuer = Configuration["JWT:Issuer"],
                        ValidAudience = Configuration["JWT:Issuer"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["JWT:Key"])),
                        ClockSkew = TimeSpan.Zero
                    };
                });

            services
                .AddIdentity<AppUser, AppRole>(options => options.SignIn.RequireConfirmedAccount = false)
                .AddDefaultUI()
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();
            services.AddControllersWithViews();

            

            // Add support for API versioning
            services.AddApiVersioning(options => options.ReportApiVersions = true);

            // Add support for m2m API documentation
            services.AddVersionedApiExplorer(options => options.GroupNameFormat = "'v'VVV");
            
            // Add support to generate human readable documentation from m2m docs
            services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
            services.AddSwaggerGen();


            services.AddAutoMapper(
                typeof(DAL.App.DTO.MappingProfiles.AutoMapperProfile),
                typeof(BLL.App.DTO.MappingProfiles.AutoMapperProfile),
                typeof(PublicApi.DTO.v1.MappingProfiles.AutoMapperProfile)
                );

            services.Configure<RequestLocalizationOptions>(options =>
            {
                //TODO should be in appsettings.json
                var appSupportedCultures = new[]
                {
                    new CultureInfo("et"),
                    new CultureInfo("en-GB")
                };
                options.SupportedCultures = appSupportedCultures;
                options.SupportedUICultures = appSupportedCultures;
                options.DefaultRequestCulture = new RequestCulture("en-GB", "en");
                options.SetDefaultCulture("en-GB");
                options.RequestCultureProviders = new List<IRequestCultureProvider>()
                {
                    new QueryStringRequestCultureProvider(),
                    new CookieRequestCultureProvider()
                };
            });


            services.AddSingleton<IConfigureOptions<MvcOptions>, ConfigureModelBindingLocalization>();


        }
        
        
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IApiVersionDescriptionProvider apiVersionDescriptionProvider)
        {
            SetupAppData(app, env, Configuration);
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();


            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                foreach (var apiVersionDescription in apiVersionDescriptionProvider.ApiVersionDescriptions)
                {
                    options.SwaggerEndpoint(
                        $"/swagger/{apiVersionDescription.GroupName}/swagger.json",
                        apiVersionDescription.GroupName.ToUpperInvariant()
                        );
                }
            }
            );
            
            
            
            app.UseStaticFiles();

            app.UseRouting();

            app.UseRequestLocalization(
                app.ApplicationServices.GetService<IOptions<RequestLocalizationOptions>>()?.Value);

            app.UseAuthentication();
            app.UseAuthorization();
            
            


            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "area",
                    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");
                
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapRazorPages();
            });
        }

        private static void SetupAppData(IApplicationBuilder app, IWebHostEnvironment env, IConfiguration configuration)
        {
            using var serviceScope =
                app.ApplicationServices
                    .GetRequiredService<IServiceScopeFactory>().
                    CreateScope();
            
            using var ctx = serviceScope.ServiceProvider.GetService<AppDbContext>();
            
            if (ctx!.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
                return;
            
            var logger = serviceScope.ServiceProvider.GetService<ILogger<Startup>>();
            if (logger == null)
            {
                throw new ApplicationException("Problem in services. Can't initialize logger");
            }
            
            if (ctx != null)
            {
                if (configuration.GetValue<bool>("AppData:DropDataBase"))
                {
                    DataInit.DropDataBase(ctx);
                }
                if (configuration.GetValue<bool>("AppData:Migrate"))
                {
                    DataInit.MigrateDataBase(ctx);
                }
                if (configuration.GetValue<bool>("AppData:SeedIdentity"))
                {
                    using var userManager = serviceScope.ServiceProvider.GetService<UserManager<AppUser>>();
                    using var roleManager = serviceScope.ServiceProvider.GetService<RoleManager<AppRole>>();

                    if (userManager != null && roleManager != null)
                    {
                        DataInit.SeedIdentity(userManager, roleManager, logger);
                    }
                    else
                    {
                        Console.Write(
                            $"No user manager {(userManager == null ? "null" : "ok")} or role manager {(roleManager == null ? "null" : "ok")}!");
                    }
                }
                if (configuration.GetValue<bool>("AppData:SeedData"))
                {
                    using var userManager = serviceScope.ServiceProvider.GetService<UserManager<AppUser>>();
                    using var roleManager = serviceScope.ServiceProvider.GetService<RoleManager<AppRole>>();
                    
                    if (userManager != null && roleManager != null)
                    {
                        DataInit.SeedAppData(ctx, logger, userManager, roleManager);
                    }
                    
                }
                // ctx.Database.EnsureDeleted();
                // ctx.Database.Migrate();
            }
        }
    }
}