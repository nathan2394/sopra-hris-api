using System;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using sopra_hris_api.Entities;
using sopra_hris_api.Helpers;
using sopra_hris_api.Services;
using sopra_hris_api.src.Entities;
using sopra_hris_api.src.Helpers;
using sopra_hris_api.src.Services;
using sopra_hris_api.src.Services.API;
using Utility = sopra_hris_api.Helpers.Utility;

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

            services.AddSingleton<IConfiguration>(Configuration);
            Utility.SetConfiguration(Configuration);

            //context accesscor
            services.AddHttpContextAccessor();

            //add memory Caching
            services.AddMemoryCache();

            //Authhentication / Authorization
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
            var jwtKey = Configuration.GetSection("AppSettings:Secret").Value;
            if (jwtKey != null)
            {
                var keyx = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
                services
                    .AddAuthentication(x =>
                    {
                        x.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                        x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                        x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                        x.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    })
                    .AddCookie()
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

            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IServiceAsync<Users>, UserService>();
            services.AddScoped<IServiceAsync<Company>, CompanyService>();
            services.AddScoped<IServiceAsync<Blogs>, BlogService>();
            services.AddScoped<IServiceAsync<Configurations>, ConfigurationService>();
            services.AddScoped<IServiceAsync<Division>, DivisionService>();
            services.AddScoped<IServiceAsync<DivisionDetails>, DivisionDetailService>();
            services.AddScoped<IServiceAsync<Departments>, DepartmentService>();
            services.AddScoped<IServiceAsync<DepartmentDetails>, DepartmentDetailService>();
            services.AddScoped<IServiceEmployeeAsync<Employees>, EmployeeService>();
            services.AddScoped<IServiceAsync<EmployeeJobTitles>, EmployeeJobTitleService>();
            services.AddScoped<IServiceAsync<EmployeeIdeas>, EmployeeIdeaService>();
            services.AddScoped<IServiceAsync<EmployeeIdeaDetails>, EmployeeIdeaDetailService>();
            services.AddScoped<IServiceAsync<EmployeeTypes>, EmployeeTypeService>();
            services.AddScoped<IServiceAsync<EmployeeDetails>, EmployeeDetailService>();
            services.AddScoped<IServiceAsync<FAQ>, FAQService>();
            services.AddScoped<IServiceAsync<Functions>, FunctionService>();
            services.AddScoped<IServiceAsync<FunctionDetails>, FunctionDetailService>();
            services.AddScoped<IServiceAsync<Groups>, GroupService>();
            services.AddScoped<IServiceAsync<GroupDetails>, GroupDetailService>();
            services.AddScoped<IServiceAsync<Shifts>, ShiftService>();
            services.AddScoped<IServiceAsync<Holidays>, HolidayService>();
            services.AddScoped<IServiceAsync<Machines>, MachineService>();
            services.AddScoped<IServiceUnattendanceOVTAsync<Unattendances>, UnattendanceService>();
            services.AddScoped<IServiceAsync<PKWTContracts>, PKWTContractService>();
            services.AddScoped<IServiceOVTAsync<Overtimes>, OvertimeService>();
            services.AddScoped<IServiceAsync<Reasons>, ReasonService>();
            services.AddScoped<IServiceEmployeeTransferShiftAsync<EmployeeTransferShifts>, EmployeeTransferShiftService>();
            services.AddScoped<IServiceAsync<UnattendanceTypes>, UnattendanceTypeService>();
            services.AddScoped<IServiceAttendancesAsync<Attendances>, AttendanceService>();
            services.AddScoped<IServiceAsync<AllowanceDeduction>, AllowanceDeductionService>();
            services.AddScoped<IServiceAsync<TunjanganMasaKerja>, TunjanganMasaKerjaService>();
            services.AddScoped<IServiceSalaryAsync<Salary>, SalaryService>();
            services.AddScoped<IServiceSalaryDetailsAsync<SalaryDetails>, SalaryDetailService>();
            services.AddScoped<IServiceAsync<GroupShifts>, GroupShiftService>();
            services.AddScoped<IServiceUploadAsync<AllowanceMeals>, AllowanceMealService>();
            services.AddScoped<IServiceUnattendanceOVTAsync<BudgetingOvertimes>, BudgetingOvertimeService>();
            services.AddScoped<IServiceEmployeeShiftAsync<EmployeeShifts>, EmployeeShiftService>();
            services.AddScoped<IServiceAsync<EmployeeLeaveQuotas>, EmployeeLeaveQuotaService>();
            services.AddScoped<IServiceAsync<SupervisorBenefit>, SupervisorBenefitService>();
            services.AddScoped<IServiceAsync<EmployeeMonthlyReward>, EmployeeMonthlyRewardService>();
            services.AddScoped<IServiceAsync<AttendanceIncentive>, AttendanceIncentiveService>();
            services.AddScoped<IServiceJobsAsync<Jobs>, JobService>();
            services.AddScoped<IServiceJobsAsync<Candidates>, CandidateService>();
            services.AddScoped<IServiceAsync<Applicants>, ApplicantService>();
            services.AddScoped<IServiceAsync<ApplicantFamilys>, ApplicantFamilyService>();
            services.AddScoped<IServiceAsync<ApplicantOtherInfo>, ApplicantOtherInfoService>();
            services.AddScoped<IServiceAsync<AttendanceIncentive>, AttendanceIncentiveService>();
            services.AddScoped<IServiceAsync<IdeaAwards>, IdeaAwardService>();            
            services.AddScoped<IServiceAsync<EducationHistory>, EducationHistoryService>();
            services.AddScoped<IServiceAsync<LanguageSkills>, LanguageSkillService>();
            services.AddScoped<IServiceAsync<OrganizationalHistory>, OrganizationalHistoryService>();
            services.AddScoped<IServiceAsync<OtherReferences>, OtherReferenceService>();
            services.AddScoped<IServiceAsync<WorkExperience>, WorkExperienceService>();
            services.AddScoped<IServiceAsync<WarningLetters>, WarningLetterService>();
            services.AddScoped<IServiceDashboardAsync<DashboardDTO>, DashboardService>();
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
            var attachmentDirectory = Path.Combine(Directory.GetCurrentDirectory(), "AttachmentFiles");
            if (!Directory.Exists(attachmentDirectory))
                Directory.CreateDirectory(attachmentDirectory);

            var attachmentIdeasDirectory = Path.Combine(Directory.GetCurrentDirectory(), "EmployeeIdeasFiles");
            if (!Directory.Exists(attachmentIdeasDirectory))
                Directory.CreateDirectory(attachmentIdeasDirectory);

            var attachmentApplicationsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "ApplicationsFiles");
            if (!Directory.Exists(attachmentApplicationsDirectory))
                Directory.CreateDirectory(attachmentApplicationsDirectory);

            var attachmentContractsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "ContractsFiles");
            if (!Directory.Exists(attachmentContractsDirectory))
                Directory.CreateDirectory(attachmentContractsDirectory);

            var attachmentBlogsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "BlogsFiles");
            if (!Directory.Exists(attachmentBlogsDirectory))
                Directory.CreateDirectory(attachmentBlogsDirectory);

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(attachmentDirectory),
                RequestPath = "/AttachmentFiles",
            });

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(attachmentIdeasDirectory),
                RequestPath = "/EmployeeIdeasFiles"
            });

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(attachmentApplicationsDirectory),
                RequestPath = "/ApplicationsFiles"
            });

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(attachmentContractsDirectory),
                RequestPath = "/ContractsFiles"
            });

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(attachmentBlogsDirectory),
                RequestPath = "/BlogsFiles"
            });

            app.UseEndpoints(x => x.MapControllers());
        }
    }
}
