using System.Diagnostics;
using System.Text;
using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Models;
using sopra_hris_api.Entities;
using sopra_hris_api.Helpers;
using sopra_hris_api.Services;
using sopra_hris_api.src.Helpers;
using sopra_hris_api.src.Services.API;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using sopra_hris_api.src.Services;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Serialization;
using System.Text.Json;

namespace sopra_hris_api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Trace.Listeners.Add(new MyTraceListener());
            Trace.WriteLine("Starting API");

            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //DB Initializer
            var connectionString = Configuration.GetSection("AppSettings");
            //Utility.ConnectSQL(Configuration["SQL:Server"], Configuration["SQL:Database"], Configuration["SQL:UserID"], Configuration["SQL:Password"]);

            services.AddDbContextPool<EFContext>(opt => opt.UseSqlServer(connectionString["ConnectionString"]));

            //context accesscor
            services.AddHttpContextAccessor();

            //add memory Caching
            services.AddMemoryCache();


            //Authhentication / Authorization
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
            var jwtKey = Configuration.GetSection("AppSettings:Secret").Value;
            if (jwtKey != null)
            {
                var keyx = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtKey));
                services
                    .AddAuthentication(x =>
                    {
                        x.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                        x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                        x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    })
                    .AddJwtBearer(options =>
                    {
                        options.RequireHttpsMetadata = false;
                        options.SaveToken = true;
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuerSigningKey = true,
                            IssuerSigningKey = keyx,
                            ValidateIssuer = false,
                            ValidateAudience = false,
                            ValidateLifetime = false,
                            // set clockskew to zero so tokens expire exactly at token expiration time (instead of 5 minutes later)
                            ClockSkew = TimeSpan.Zero
                        };
                    });
            }

            services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                {
                    builder
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowAnyOrigin();
                });
            });
            
            services.AddControllers().AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
            });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "SOPRA HRIS API",
                    Version = "v1",
                    Description = "SOPRA HRIS API Documentation"
                });

                // Enable support for multipart/form-data file uploads
                c.OperationFilter<FileUploadOperationFilter>();

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Description = "Please enter token",
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    BearerFormat = "JWT",
                    Scheme = "bearer"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            });

            services.AddScoped<IServiceAsync<Company>, CompanyService>();
            services.AddScoped<IServiceAsync<Division>, DivisionService>();
            services.AddScoped<IServiceAsync<DivisionDetails>, DivisionDetailService>();
            services.AddScoped<IServiceAsync<Employees>, EmployeeService>();
            services.AddScoped<IServiceAsync<EmployeeTypes>, EmployeeTypeService>();
            services.AddScoped<IServiceAsync<EmployeeDetails>, EmployeeDetailService>();
            services.AddScoped<IServiceAsync<Functions>, FunctionService>();
            services.AddScoped<IServiceAsync<FunctionDetails>, FunctionDetailService>();
            services.AddScoped<IServiceAsync<Groups>, GroupService>();
            services.AddScoped<IServiceAsync<GroupDetails>, GroupDetailService>();
            services.AddScoped<IServiceAsync<AllowanceDeduction>, AllowanceDeductionService>();
            services.AddScoped<IServiceAsync<TunjanganKinerja>, TunjanganKinerjaService>();
            services.AddScoped<IServiceSalaryAsync<Salary>, SalaryService>();
            services.AddScoped<IServiceAsync<SalaryDetails>, SalaryDetailService>();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            app.UseRouting();

            // global cors policy
            app.UseCors(x => x
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader());
            app.UseCors();

            app.UseAuthentication();
            app.UseAuthorization();
            
            //if (env.IsDevelopment())
            //{
                // Enable middleware to serve generated Swagger as a JSON endpoint.
                app.UseSwagger();

                // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
                // specifying the Swagger JSON endpoint.
                //app.UseSwaggerUI();
            //}

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", $"SOPRA HRIS API V1.0201");
                c.RoutePrefix = string.Empty;
            });
            app.UseMiddleware<JwtMiddleware>();

            app.UseEndpoints(x => x.MapControllers());
        }
    }
}
