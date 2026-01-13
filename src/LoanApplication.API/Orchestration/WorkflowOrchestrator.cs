using LoanApplication.API.Data;
using LoanApplication.API.DTOs;
using LoanApplication.API.Models;
using LoanApplication.API.Clients;
using LoanApplication.API.Services;
using Microsoft.EntityFrameworkCore;

namespace LoanApplication.API.Orchestration;

public class WorkflowOrchestrator : IWorkflowOrchestrator
{
    private readonly ApplicationDbContext _context;
    private readonly ICustomerServiceClient _customerClient;
    private readonly IPropertyServiceClient _propertyClient;
    private readonly ILoanServiceClient _loanClient;
    private readonly IPaymentServiceClient _paymentClient;
    private readonly IApplicationService _applicationService;
    private readonly ILogger<WorkflowOrchestrator> _logger;

    public WorkflowOrchestrator(
        ApplicationDbContext context,
        ICustomerServiceClient customerClient,
        IPropertyServiceClient propertyClient,
        ILoanServiceClient loanClient,
        IPaymentServiceClient paymentClient,
        IApplicationService applicationService,
        ILogger<WorkflowOrchestrator> logger)
    {
        _context = context;
        _customerClient = customerClient;
        _propertyClient = propertyClient;
        _loanClient = loanClient;
        _paymentClient = paymentClient;
        _applicationService = applicationService;
        _logger = logger;
    }

    public async Task<ApplicationResponseDto> StartUnderwritingAsync(Guid applicationId)
    {
        var application = await _context.Applications
            .Include(a => a.Underwriting)
            .FirstOrDefaultAsync(a => a.Id == applicationId);

        if (application == null)
            throw new InvalidOperationException($"Application {applicationId} not found");

        _logger.LogInformation("Starting underwriting for application {ApplicationId}", applicationId);

        // Fetch data from all services in parallel
        var customerTask = _customerClient.GetCustomerAsync(application.CustomerId);
        var creditTask = _customerClient.GetCreditAsync(application.CustomerId);
        var employmentsTask = _customerClient.GetEmploymentsAsync(application.CustomerId);
        var propertyTask = _propertyClient.GetPropertyAsync(application.PropertyId);
        var appraisalTask = _propertyClient.GetAppraisalAsync(application.PropertyId);
        var titleTask = _propertyClient.GetTitleSearchAsync(application.PropertyId);

        await Task.WhenAll(customerTask, creditTask, employmentsTask, propertyTask, appraisalTask, titleTask);

        var customer = await customerTask;
        var credit = await creditTask;
        var employments = (await employmentsTask).ToList();
        var property = await propertyTask;
        var appraisal = await appraisalTask;
        var title = await titleTask;

        // Create or update underwriting
        var underwriting = application.Underwriting ?? new Underwriting { ApplicationId = applicationId };

        // Credit analysis
        if (credit != null)
        {
            underwriting.CreditScore = credit.CreditScore;
            underwriting.CreditRating = credit.CreditRating;
            underwriting.CreditApproved = credit.CreditScore >= 620;
        }

        // Income analysis
        var currentEmployment = employments.FirstOrDefault(e => e.IsCurrent);
        if (currentEmployment != null)
        {
            underwriting.GrossMonthlyIncome = currentEmployment.AnnualIncome / 12;
            underwriting.YearsEmployed = currentEmployment.YearsEmployed;
            underwriting.EmploymentVerified = true;
        }

        // Calculate DTI (estimated monthly payment / gross monthly income)
        if (underwriting.GrossMonthlyIncome > 0)
        {
            var estimatedMonthlyPayment = CalculateMonthlyPayment(
                application.RequestedLoanAmount,
                6.5m, // Estimated rate
                application.RequestedTermMonths);
            underwriting.CalculatedDTI = Math.Round((estimatedMonthlyPayment / underwriting.GrossMonthlyIncome.Value) * 100, 2);
            underwriting.IncomeVerified = true;
        }

        // Property analysis
        if (appraisal != null)
        {
            underwriting.AppraisedValue = appraisal.AppraisedValue;
            underwriting.CalculatedLTV = Math.Round((application.RequestedLoanAmount / appraisal.AppraisedValue) * 100, 2);
            underwriting.PropertyApproved = appraisal.Status == "Completed";
        }
        else if (property != null)
        {
            underwriting.AppraisedValue = property.EstimatedValue;
            underwriting.CalculatedLTV = Math.Round((application.RequestedLoanAmount / property.EstimatedValue) * 100, 2);
        }

        // Title check
        if (title != null)
        {
            underwriting.TitleClear = title.IsClear;
        }

        // Make automated decision
        underwriting.Decision = DetermineDecision(underwriting);
        underwriting.UnderwriterName = "Automated System";

        if (application.Underwriting == null)
        {
            _context.Underwritings.Add(underwriting);
        }

        // Update application
        application.LTV = underwriting.CalculatedLTV;
        application.DTI = underwriting.CalculatedDTI;
        application.Status = ApplicationStatus.Underwriting;
        application.UnderwritingStartedAt = DateTime.UtcNow;
        application.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Underwriting completed for application {ApplicationId}: {Decision}", 
            applicationId, underwriting.Decision);

        return await _applicationService.GetApplicationByIdAsync(applicationId) 
            ?? throw new InvalidOperationException("Failed to retrieve application");
    }

    public async Task<ApplicationResponseDto> ProcessDecisionAsync(Guid applicationId, ApplicationDecisionDto decision)
    {
        var application = await _context.Applications
            .Include(a => a.Underwriting)
            .FirstOrDefaultAsync(a => a.Id == applicationId);

        if (application == null)
            throw new InvalidOperationException($"Application {applicationId} not found");

        _logger.LogInformation("Processing decision for application {ApplicationId}: Approved={Approved}", 
            applicationId, decision.Approved);

        var previousStatus = application.Status;

        if (decision.Approved)
        {
            application.Status = decision.Conditions?.Any() == true 
                ? ApplicationStatus.ConditionalApproval 
                : ApplicationStatus.Approved;
            application.ApprovedLoanAmount = decision.ApprovedAmount ?? application.RequestedLoanAmount;
            application.OfferedInterestRate = decision.InterestRate;

            // Add conditions if any
            if (decision.Conditions?.Any() == true)
            {
                foreach (var condition in decision.Conditions)
                {
                    _context.Conditions.Add(new ApplicationCondition
                    {
                        ApplicationId = applicationId,
                        ConditionName = condition,
                        ConditionType = ConditionType.PriorToClosing,
                        Status = ConditionStatus.Pending
                    });
                }
            }
        }
        else
        {
            application.Status = ApplicationStatus.Rejected;
        }

        application.DecisionAt = DateTime.UtcNow;
        application.DecisionReason = decision.Reason;
        application.UpdatedAt = DateTime.UtcNow;

        // Record status change
        _context.StatusHistory.Add(new ApplicationStatusHistory
        {
            ApplicationId = applicationId,
            FromStatus = previousStatus,
            ToStatus = application.Status,
            Reason = decision.Reason,
            ChangedBy = "System"
        });

        if (application.Underwriting != null)
        {
            application.Underwriting.Decision = decision.Approved 
                ? UnderwritingDecision.Approved 
                : UnderwritingDecision.Denied;
            application.Underwriting.DecisionNotes = decision.Reason;
            application.Underwriting.CompletedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return await _applicationService.GetApplicationByIdAsync(applicationId)
            ?? throw new InvalidOperationException("Failed to retrieve application");
    }

    public async Task<ApplicationResponseDto> CreateLoanAndFundAsync(Guid applicationId)
    {
        var application = await _context.Applications
            .FirstOrDefaultAsync(a => a.Id == applicationId);

        if (application == null)
            throw new InvalidOperationException($"Application {applicationId} not found");

        if (application.Status != ApplicationStatus.Approved && application.Status != ApplicationStatus.ClearToClose)
            throw new InvalidOperationException($"Application must be approved to create loan. Current status: {application.Status}");

        _logger.LogInformation("Creating loan for application {ApplicationId}", applicationId);

        // Step 1: Create the loan
        var loanRequest = new CreateLoanRequest
        {
            CustomerId = application.CustomerId,
            PropertyId = application.PropertyId,
            PrincipalAmount = application.ApprovedLoanAmount ?? application.RequestedLoanAmount,
            InterestRate = application.OfferedInterestRate ?? 6.5m,
            TermMonths = application.RequestedTermMonths,
            LoanType = 1, // Conventional
            DownPayment = application.DownPaymentAmount
        };

        var loan = await _loanClient.CreateLoanAsync(loanRequest);
        if (loan == null)
            throw new InvalidOperationException("Failed to create loan");

        _logger.LogInformation("Created loan {LoanId} for application {ApplicationId}", loan.Id, applicationId);

        // Step 2: Fund the loan
        var fundingDate = DateTime.UtcNow;
        var firstPaymentDate = new DateTime(fundingDate.Year, fundingDate.Month, 1).AddMonths(2);
        
        var funded = await _loanClient.FundLoanAsync(loan.Id, fundingDate, firstPaymentDate);
        if (!funded)
            _logger.LogWarning("Failed to fund loan {LoanId}", loan.Id);

        // Step 3: Setup payment schedule
        var scheduleRequest = new CreateScheduleRequest
        {
            LoanId = loan.Id,
            CustomerId = application.CustomerId,
            IsAutoPay = true,
            PaymentDayOfMonth = 1,
            RegularPaymentAmount = loan.MonthlyPayment
        };

        var schedule = await _paymentClient.CreateScheduleAsync(scheduleRequest);
        if (schedule == null)
            _logger.LogWarning("Failed to create payment schedule for loan {LoanId}", loan.Id);

        // Update application
        application.LoanId = loan.Id;
        application.Status = ApplicationStatus.Funded;
        application.ClosedAt = DateTime.UtcNow;
        application.UpdatedAt = DateTime.UtcNow;

        _context.StatusHistory.Add(new ApplicationStatusHistory
        {
            ApplicationId = applicationId,
            FromStatus = ApplicationStatus.Approved,
            ToStatus = ApplicationStatus.Funded,
            Reason = $"Loan {loan.LoanNumber} created and funded",
            ChangedBy = "System"
        });

        await _context.SaveChangesAsync();

        _logger.LogInformation("Application {ApplicationId} funded with loan {LoanId}", applicationId, loan.Id);

        return await _applicationService.GetApplicationByIdAsync(applicationId)
            ?? throw new InvalidOperationException("Failed to retrieve application");
    }

    private static UnderwritingDecision DetermineDecision(Underwriting underwriting)
    {
        var issues = new List<string>();

        if (!underwriting.CreditApproved) issues.Add("Credit not approved");
        if (underwriting.CalculatedDTI > 43) issues.Add("DTI too high");
        if (underwriting.CalculatedLTV > 97) issues.Add("LTV too high");
        if (!underwriting.TitleClear) issues.Add("Title not clear");

        if (issues.Count == 0)
            return UnderwritingDecision.Approved;
        else if (issues.Count <= 2)
            return UnderwritingDecision.ApprovedWithConditions;
        else
            return UnderwritingDecision.Denied;
    }

    private static decimal CalculateMonthlyPayment(decimal principal, decimal annualRate, int termMonths)
    {
        if (termMonths <= 0 || principal <= 0) return 0;
        if (annualRate <= 0) return principal / termMonths;

        var monthlyRate = annualRate / 100 / 12;
        var payment = principal * (monthlyRate * (decimal)Math.Pow((double)(1 + monthlyRate), termMonths))
                      / ((decimal)Math.Pow((double)(1 + monthlyRate), termMonths) - 1);
        return Math.Round(payment, 2);
    }
}
