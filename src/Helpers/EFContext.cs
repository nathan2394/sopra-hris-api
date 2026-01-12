using Microsoft.EntityFrameworkCore;
using sopra_hris_api.Entities;
using sopra_hris_api.src.Entities;
using static sopra_hris_api.src.Entities.DashboardDTO;

namespace sopra_hris_api.src.Helpers
{
    public class EFContext : DbContext
    {
        public EFContext(DbContextOptions<EFContext> opts) : base(opts) { }
        public DbSet<AllowanceDeduction> AllowanceDeduction { get; set; }
        public DbSet<AllowanceMeals> AllowanceMeals {  get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<DivisionDetails> DivisionDetails { get; set; }
        public DbSet<Division> Divisions { get; set; }
        public DbSet<DepartmentDetails> DepartmentDetails { get; set; }
        public DbSet<Departments> Departments { get; set; }
        public DbSet<EmployeeDetails> EmployeeDetails { get; set; }
        public DbSet<Employees> Employees { get; set; }
        public DbSet<IdeaAwards> IdeaAwards { get; set; }
        public DbSet<EmployeeIdeas> EmployeeIdeas { get; set; }
        public DbSet<EmployeeIdeaDetails> EmployeeIdeaDetails { get; set; }
        public DbSet<EmployeeTypes> EmployeeTypes { get; set; }
        public DbSet<EmployeeJobTitles> EmployeeJobTitles { get; set; }
        public DbSet<EmployeeLeaveQuotas> EmployeeLeaveQuotas { get; set; }
        public DbSet<AttendanceIncentive> AttendanceIncentive { get; set; }
        public DbSet<EmployeeMonthlyReward> EmployeeMonthlyReward { get; set; }
        public DbSet<SupervisorBenefit> SupervisorBenefit { get; set; }
        public DbSet<FAQ> FAQ { get; set; }
        public DbSet<FunctionDetails> FunctionDetails { get; set; }
        public DbSet<Functions> Functions { get; set; }
        public DbSet<GroupDetails> GroupDetails { get; set; }
        public DbSet<Groups> Groups { get; set; }
        public DbSet<Modules> Modules { get; set; }
        public DbSet<RoleDetails> RoleDetails { get; set; }
        public DbSet<Roles> Roles { get; set; }
        public DbSet<TunjanganMasaKerja> TunjanganMasaKerja { get; set; }
        public DbSet<UserLogs> UserLogs { get; set; }
        public DbSet<Users> Users { get; set; }
        public DbSet<Salary> Salary { get; set; }
        public DbSet<SalaryDetails> SalaryDetails { get; set; }
        public DbSet<SalaryHistory> SalaryHistory { get; set; }
        public DbSet<Holidays> Holidays { get; set; }
        public DbSet<Shifts> Shifts { get; set; }
        public DbSet<AttendanceDetails> AttendanceDetails { get; set; }
        public DbSet<UnattendanceTypes> UnattendanceTypes { get; set; }
        public virtual DbSet<EmailDTO> EmailDTO { get; set; }
        public DbSet<Attendances> Attendances { get; set; }
        public DbSet<Configurations> Configurations { get; set; }
        public DbSet<MatrixApproval> MatrixApproval { get; set; }
        public DbSet<Machines> Machines { get; set; }
        public DbSet<EmployeeTransferShifts> EmployeeTransferShifts { get; set; }
        public DbSet<GroupShifts> GroupShifts { get; set; }
        public DbSet<EmployeeShifts> EmployeeShifts { get; set; }
        public virtual DbSet<EmployeeShiftsDTO> EmployeeShiftsDTO { get; set; }
        public DbSet<Overtimes> Overtimes { get; set; }
        public DbSet<Reasons> Reasons { get; set; }
        public DbSet<BudgetingOvertimes> BudgetingOvertimes { get; set; }
        public DbSet<Unattendances> Unattendances { get; set; }
        public DbSet<PKWTContracts> PKWTContracts { get; set; }
        public DbSet<Jobs> Jobs { get; set; }
        public DbSet<Blogs> Blogs { get; set; }
        public DbSet<Candidates> Candidates { get; set; }
        public DbSet<Applicants> Applicants { get; set; }
        public DbSet<ApplicantSkillList> ApplicantSkillList { get; set; }
        public DbSet<ApplicantCertifications> ApplicantCertifications { get; set; }
        public DbSet<ApplicantSocialMedia> ApplicantSocialMedia { get; set; }
        public DbSet<WarningLetters> WarningLetters { get; set; }
        public DbSet<ApplicantOtherInfo> ApplicantOtherInfo { get; set; }
        public DbSet<OtherReferences> OtherReferences { get; set; }
        public DbSet<WorkExperience> WorkExperience { get; set; }
        public DbSet<LanguageSkills> LanguageSkills { get; set; }
        public DbSet<OrganizationalHistory> OrganizationalHistory { get; set; }
        public DbSet<EducationHistory> EducationHistory { get; set; }
        public DbSet<ApplicantFamilys> ApplicantFamilys { get; set; }
        public DbSet<TestResponses> TestResponses { get; set; }
        public DbSet<TestSessions> TestSessions { get; set; }
        public DbSet<AnswerOptions> AnswerOptions { get; set; }
        public DbSet<Questions> Questions { get; set; }
        public DbSet<QuestionCategories> QuestionCategories { get; set; }
        public virtual DbSet<MasterEmployeePayroll> MasterEmployeePayroll { get; set; }
        public virtual DbSet<SalaryDetailReportsDTO> SalaryDetailReportsDTO { get; set; }
        public virtual DbSet<SalaryPayrollSummaryDTO> SalaryPayrollSummaryDTO { get; set; }
        public virtual DbSet<SalaryPayrollSummaryTotalDTO> SalaryPayrollSummaryTotalDTO { get; set; }
        public virtual DbSet<SalaryCalculatorModel> SalaryCalculatorModel { get; set; }
        public virtual DbSet<AttendanceSummary> AttendanceSummary { get; set; }
        public virtual DbSet<AttendanceShift> AttendanceShift { get; set; }
        public virtual DbSet<AttendanceCheck> AttendanceCheck { get; set; }
        public virtual DbSet<DashboardAttendanceSummary> DashboardAttendanceSummary { get; set; }
        public virtual DbSet<DashboardBudgetOvertimes> DashboardBudgetOvertimes { get; set; }
        public virtual DbSet<DashboardAttendanceNormalAbnormal> DashboardAttendanceNormalAbnormal { get; set; }
        public virtual DbSet<DashboardAttendanceByShift> DashboardAttendanceByShift { get; set; }
        public virtual DbSet<DashboardApproval> DashboardApproval { get; set; }
        public virtual DbSet<DashboardDetaillOVT> DashboardDetaillOVT { get; set; }
        public virtual DbSet<DashboardDetaillMeals> DashboardDetaillMeals { get; set; }
        public virtual DbSet<DashboardDetaillAbsent> DashboardDetaillAbsent { get; set; }
        public virtual DbSet<DashboardDetaillLate> DashboardDetaillLate { get; set; }
        public virtual DbSet<CandidateDTO> CandidateDTO { get; set; }
        public virtual DbSet<SessionQuestionRaw> SessionQuestionRaw { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
