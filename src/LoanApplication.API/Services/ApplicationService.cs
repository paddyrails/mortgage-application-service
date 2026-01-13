using Microsoft.EntityFrameworkCore;
using LoanApplication.API.Data;
using LoanApplication.API.DTOs;
using LoanApplication.API.Models;
using LoanApplication.API.Clients;

namespace LoanApplication.API.Services;

public class ApplicationService : IApplicationService
{
    private readonly ApplicationDbContext _context;
    private readonly ICustomerServiceClient _customerClient;
    private readonly IPropertyServiceClient _propertyClient;
    private readonly ILoanServiceClient _loanClient;
    private readonly ILogger<ApplicationService> _logger;

    public ApplicationService(
        ApplicationDbContext context,
        ICustomerServiceClient customerClient,
        IPropertyServiceClient propertyClient,
        ILoanServiceClient loanClient,
        ILogger<ApplicationService> logger)
    {
        _context = context;
        _customerClient = customerClient;
        _propertyClient = propertyClient;
        _loanClient = loanClient;
        _logger = logger;
    }

    public async Task<IEnumerable<ApplicationSummaryDto>> GetAllApplicationsAsync()
    {
        var applications = await _context.Applications
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        var summaries = new List<ApplicationSummaryDto>();
        foreach (var app in applications)
        {
            var customer = await _customerClient.GetCustomerAsync(app.CustomerId);
            summaries.Add(MapToSummaryDto(app, customer?.FullName));
        }

        return summaries;
    }

    public async Task<ApplicationResponseDto?> GetApplicationByIdAsync(Guid id)
    {
        var application = await _context.Applications
            .Include(a => a.Underwriting)
            .Include(a => a.Documents)
            .Include(a => a.Conditions)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (application == null) return null;

        return await MapToResponseDtoAsync(application);
    }

    public async Task<IEnumerable<ApplicationSummaryDto>> GetApplicationsByCustomerAsync(Guid customerId)
    {
        var applications = await _context.Applications
            .Where(a => a.CustomerId == customerId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        var customer = await _customerClient.GetCustomerAsync(customerId);
        return applications.Select(a => MapToSummaryDto(a, customer?.FullName));
    }

    public async Task<IEnumerable<ApplicationSummaryDto>> GetApplicationsByStatusAsync(ApplicationStatus status)
    {
        var applications = await _context.Applications
            .Where(a => a.Status == status)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        var summaries = new List<ApplicationSummaryDto>();
        foreach (var app in applications)
        {
            var customer = await _customerClient.GetCustomerAsync(app.CustomerId);
            summaries.Add(MapToSummaryDto(app, customer?.FullName));
        }

        return summaries;
    }

    public async Task<ApplicationResponseDto> CreateApplicationAsync(CreateApplicationDto dto)
    {
        // Validate customer exists
        var customerExists = await _customerClient.CustomerExistsAsync(dto.CustomerId);
        if (!customerExists)
            throw new InvalidOperationException($"Customer {dto.CustomerId} not found");

        // Validate property exists
        var propertyExists = await _propertyClient.PropertyExistsAsync(dto.PropertyId);
        if (!propertyExists)
            throw new InvalidOperationException($"Property {dto.PropertyId} not found");

        // Get property value for LTV calculation
        var property = await _propertyClient.GetPropertyAsync(dto.PropertyId);
        var propertyValue = property?.EstimatedValue ?? dto.RequestedLoanAmount + dto.DownPaymentAmount;
        var ltv = (dto.RequestedLoanAmount / propertyValue) * 100;

        var application = new Application
        {
            ApplicationNumber = await GenerateApplicationNumberAsync(),
            CustomerId = dto.CustomerId,
            PropertyId = dto.PropertyId,
            RequestedLoanAmount = dto.RequestedLoanAmount,
            DownPaymentAmount = dto.DownPaymentAmount,
            RequestedTermMonths = dto.RequestedTermMonths,
            LoanPurpose = dto.LoanPurpose,
            ApplicationType = dto.ApplicationType,
            Status = ApplicationStatus.Draft,
            LTV = Math.Round(ltv, 2),
            Notes = dto.Notes
        };

        // Add required documents based on loan purpose
        AddRequiredDocuments(application);

        _context.Applications.Add(application);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created application {ApplicationNumber}", application.ApplicationNumber);

        return await MapToResponseDtoAsync(application);
    }

    public async Task<ApplicationResponseDto?> SubmitApplicationAsync(Guid id, SubmitApplicationDto dto)
    {
        var application = await _context.Applications.FindAsync(id);
        if (application == null) return null;

        if (!dto.AcceptTerms || !dto.AuthorizeCredit)
            throw new InvalidOperationException("Must accept terms and authorize credit check");

        var previousStatus = application.Status;
        application.Status = ApplicationStatus.Submitted;
        application.SubmittedAt = DateTime.UtcNow;
        application.UpdatedAt = DateTime.UtcNow;

        _context.StatusHistory.Add(new ApplicationStatusHistory
        {
            ApplicationId = id,
            FromStatus = previousStatus,
            ToStatus = ApplicationStatus.Submitted,
            Reason = "Application submitted by borrower",
            ChangedBy = "Borrower"
        });

        await _context.SaveChangesAsync();

        _logger.LogInformation("Application {ApplicationId} submitted", id);

        return await GetApplicationByIdAsync(id);
    }

    public async Task<ApplicationResponseDto?> UpdateStatusAsync(Guid id, ApplicationStatus status, string? reason = null)
    {
        var application = await _context.Applications.FindAsync(id);
        if (application == null) return null;

        var previousStatus = application.Status;
        application.Status = status;
        application.UpdatedAt = DateTime.UtcNow;

        _context.StatusHistory.Add(new ApplicationStatusHistory
        {
            ApplicationId = id,
            FromStatus = previousStatus,
            ToStatus = status,
            Reason = reason,
            ChangedBy = "System"
        });

        await _context.SaveChangesAsync();

        return await GetApplicationByIdAsync(id);
    }

    public async Task<bool> WithdrawApplicationAsync(Guid id, string? reason = null)
    {
        var application = await _context.Applications.FindAsync(id);
        if (application == null) return false;

        var previousStatus = application.Status;
        application.Status = ApplicationStatus.Withdrawn;
        application.UpdatedAt = DateTime.UtcNow;

        _context.StatusHistory.Add(new ApplicationStatusHistory
        {
            ApplicationId = id,
            FromStatus = previousStatus,
            ToStatus = ApplicationStatus.Withdrawn,
            Reason = reason ?? "Withdrawn by borrower",
            ChangedBy = "Borrower"
        });

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<DocumentResponseDto> AddDocumentAsync(Guid applicationId, AddDocumentDto dto)
    {
        var document = new ApplicationDocument
        {
            ApplicationId = applicationId,
            DocumentName = dto.DocumentName,
            DocumentType = dto.DocumentType,
            Status = DocumentStatus.Required,
            Notes = dto.Notes
        };

        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        return new DocumentResponseDto
        {
            Id = document.Id,
            DocumentName = document.DocumentName,
            DocumentType = document.DocumentType.ToString(),
            Status = document.Status.ToString()
        };
    }

    public async Task<bool> UpdateDocumentStatusAsync(Guid documentId, DocumentStatus status)
    {
        var document = await _context.Documents.FindAsync(documentId);
        if (document == null) return false;

        document.Status = status;
        if (status == DocumentStatus.Received) document.ReceivedAt = DateTime.UtcNow;
        if (status == DocumentStatus.Approved) document.ApprovedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<ConditionResponseDto> AddConditionAsync(Guid applicationId, AddConditionDto dto)
    {
        var condition = new ApplicationCondition
        {
            ApplicationId = applicationId,
            ConditionName = dto.ConditionName,
            Description = dto.Description,
            ConditionType = dto.ConditionType,
            DueDate = dto.DueDate,
            Status = ConditionStatus.Pending
        };

        _context.Conditions.Add(condition);
        await _context.SaveChangesAsync();

        return new ConditionResponseDto
        {
            Id = condition.Id,
            ConditionName = condition.ConditionName,
            Description = condition.Description,
            ConditionType = condition.ConditionType.ToString(),
            Status = condition.Status.ToString(),
            DueDate = condition.DueDate
        };
    }

    public async Task<bool> UpdateConditionStatusAsync(Guid conditionId, ConditionStatus status)
    {
        var condition = await _context.Conditions.FindAsync(conditionId);
        if (condition == null) return false;

        condition.Status = status;
        if (status == ConditionStatus.Satisfied) condition.SatisfiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    #region Private Methods

    private async Task<string> GenerateApplicationNumberAsync()
    {
        var year = DateTime.UtcNow.Year;
        var count = await _context.Applications.CountAsync(a => a.ApplicationNumber.StartsWith($"APP-{year}"));
        return $"APP-{year}-{(count + 1):D6}";
    }

    private static void AddRequiredDocuments(Application application)
    {
        var requiredDocs = new[]
        {
            (DocumentType.DriversLicense, "Government ID"),
            (DocumentType.PayStubs, "Recent Pay Stubs (2 months)"),
            (DocumentType.W2Forms, "W-2 Forms (2 years)"),
            (DocumentType.BankStatements, "Bank Statements (2 months)"),
            (DocumentType.TaxReturns, "Tax Returns (2 years)")
        };

        foreach (var (docType, docName) in requiredDocs)
        {
            application.Documents.Add(new ApplicationDocument
            {
                ApplicationId = application.Id,
                DocumentType = docType,
                DocumentName = docName,
                Status = DocumentStatus.Required,
                RequestedAt = DateTime.UtcNow
            });
        }
    }

    private async Task<ApplicationResponseDto> MapToResponseDtoAsync(Application app)
    {
        var customerTask = _customerClient.GetCustomerAsync(app.CustomerId);
        var propertyTask = _propertyClient.GetPropertyAsync(app.PropertyId);
        
        LoanDto? loan = null;
        if (app.LoanId.HasValue)
        {
            loan = await _loanClient.GetLoanAsync(app.LoanId.Value);
        }

        await Task.WhenAll(customerTask, propertyTask);

        return new ApplicationResponseDto
        {
            Id = app.Id,
            ApplicationNumber = app.ApplicationNumber,
            CustomerId = app.CustomerId,
            PropertyId = app.PropertyId,
            LoanId = app.LoanId,
            Customer = await customerTask,
            Property = await propertyTask,
            Loan = loan,
            RequestedLoanAmount = app.RequestedLoanAmount,
            DownPaymentAmount = app.DownPaymentAmount,
            RequestedTermMonths = app.RequestedTermMonths,
            LoanPurpose = app.LoanPurpose.ToString(),
            Status = app.Status.ToString(),
            ApplicationType = app.ApplicationType.ToString(),
            LTV = app.LTV,
            DTI = app.DTI,
            OfferedInterestRate = app.OfferedInterestRate,
            ApprovedLoanAmount = app.ApprovedLoanAmount,
            CreatedAt = app.CreatedAt,
            SubmittedAt = app.SubmittedAt,
            DecisionAt = app.DecisionAt,
            DecisionReason = app.DecisionReason,
            Underwriting = app.Underwriting == null ? null : new UnderwritingResponseDto
            {
                Id = app.Underwriting.Id,
                CreditScore = app.Underwriting.CreditScore,
                CreditRating = app.Underwriting.CreditRating,
                CreditApproved = app.Underwriting.CreditApproved,
                GrossMonthlyIncome = app.Underwriting.GrossMonthlyIncome,
                CalculatedDTI = app.Underwriting.CalculatedDTI,
                IncomeVerified = app.Underwriting.IncomeVerified,
                AppraisedValue = app.Underwriting.AppraisedValue,
                CalculatedLTV = app.Underwriting.CalculatedLTV,
                PropertyApproved = app.Underwriting.PropertyApproved,
                TitleClear = app.Underwriting.TitleClear,
                EmploymentVerified = app.Underwriting.EmploymentVerified,
                Decision = app.Underwriting.Decision.ToString(),
                DecisionNotes = app.Underwriting.DecisionNotes
            },
            Documents = app.Documents.Select(d => new DocumentResponseDto
            {
                Id = d.Id,
                DocumentName = d.DocumentName,
                DocumentType = d.DocumentType.ToString(),
                Status = d.Status.ToString(),
                RequestedAt = d.RequestedAt,
                ReceivedAt = d.ReceivedAt
            }).ToList(),
            Conditions = app.Conditions.Select(c => new ConditionResponseDto
            {
                Id = c.Id,
                ConditionName = c.ConditionName,
                Description = c.Description,
                ConditionType = c.ConditionType.ToString(),
                Status = c.Status.ToString(),
                DueDate = c.DueDate
            }).ToList()
        };
    }

    private static ApplicationSummaryDto MapToSummaryDto(Application app, string? customerName)
    {
        return new ApplicationSummaryDto
        {
            Id = app.Id,
            ApplicationNumber = app.ApplicationNumber,
            CustomerId = app.CustomerId,
            CustomerName = customerName,
            RequestedLoanAmount = app.RequestedLoanAmount,
            Status = app.Status.ToString(),
            CreatedAt = app.CreatedAt,
            SubmittedAt = app.SubmittedAt
        };
    }

    #endregion
}
