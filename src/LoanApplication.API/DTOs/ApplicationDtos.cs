using System.ComponentModel.DataAnnotations;
using LoanApplication.API.Models;
using LoanApplication.API.Clients;

namespace LoanApplication.API.DTOs;

// Full Application Response with enriched data
public record ApplicationResponseDto
{
    public Guid Id { get; init; }
    public string ApplicationNumber { get; init; } = string.Empty;
    public Guid CustomerId { get; init; }
    public Guid PropertyId { get; init; }
    public Guid? LoanId { get; init; }
    
    // Enriched data from other services
    public CustomerDto? Customer { get; init; }
    public PropertyDto? Property { get; init; }
    public LoanDto? Loan { get; init; }
    
    public decimal RequestedLoanAmount { get; init; }
    public decimal DownPaymentAmount { get; init; }
    public int RequestedTermMonths { get; init; }
    public string LoanPurpose { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string ApplicationType { get; init; } = string.Empty;
    
    public decimal? LTV { get; init; }
    public decimal? DTI { get; init; }
    public decimal? OfferedInterestRate { get; init; }
    public decimal? ApprovedLoanAmount { get; init; }
    
    public DateTime CreatedAt { get; init; }
    public DateTime? SubmittedAt { get; init; }
    public DateTime? DecisionAt { get; init; }
    public string? DecisionReason { get; init; }
    
    public UnderwritingResponseDto? Underwriting { get; init; }
    public List<DocumentResponseDto> Documents { get; init; } = new();
    public List<ConditionResponseDto> Conditions { get; init; } = new();
}

// Summary for listings
public record ApplicationSummaryDto
{
    public Guid Id { get; init; }
    public string ApplicationNumber { get; init; } = string.Empty;
    public Guid CustomerId { get; init; }
    public string? CustomerName { get; init; }
    public decimal RequestedLoanAmount { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? SubmittedAt { get; init; }
}

// Create Application DTO
public record CreateApplicationDto
{
    [Required]
    public Guid CustomerId { get; init; }

    [Required]
    public Guid PropertyId { get; init; }

    [Required]
    [Range(10000, 10000000)]
    public decimal RequestedLoanAmount { get; init; }

    [Range(0, 10000000)]
    public decimal DownPaymentAmount { get; init; }

    [Required]
    [Range(60, 480)]
    public int RequestedTermMonths { get; init; }

    [Required]
    public LoanPurpose LoanPurpose { get; init; }

    public ApplicationType ApplicationType { get; init; } = ApplicationType.Purchase;

    [StringLength(1000)]
    public string? Notes { get; init; }
}

// Submit Application DTO
public record SubmitApplicationDto
{
    public bool AcceptTerms { get; init; }
    public bool AuthorizeCredit { get; init; }
}

// Decision DTO
public record ApplicationDecisionDto
{
    [Required]
    public bool Approved { get; init; }

    [Range(0, 10000000)]
    public decimal? ApprovedAmount { get; init; }

    [Range(0, 20)]
    public decimal? InterestRate { get; init; }

    [StringLength(500)]
    public string? Reason { get; init; }

    public List<string>? Conditions { get; init; }
}

// Underwriting Response
public record UnderwritingResponseDto
{
    public Guid Id { get; init; }
    public int? CreditScore { get; init; }
    public string? CreditRating { get; init; }
    public bool CreditApproved { get; init; }
    public decimal? GrossMonthlyIncome { get; init; }
    public decimal? CalculatedDTI { get; init; }
    public bool IncomeVerified { get; init; }
    public decimal? AppraisedValue { get; init; }
    public decimal? CalculatedLTV { get; init; }
    public bool PropertyApproved { get; init; }
    public bool TitleClear { get; init; }
    public bool EmploymentVerified { get; init; }
    public string Decision { get; init; } = string.Empty;
    public string? DecisionNotes { get; init; }
}

// Document Response
public record DocumentResponseDto
{
    public Guid Id { get; init; }
    public string DocumentName { get; init; } = string.Empty;
    public string DocumentType { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime? RequestedAt { get; init; }
    public DateTime? ReceivedAt { get; init; }
}

// Condition Response
public record ConditionResponseDto
{
    public Guid Id { get; init; }
    public string ConditionName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string ConditionType { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime? DueDate { get; init; }
}

// Add Document DTO
public record AddDocumentDto
{
    [Required]
    [StringLength(100)]
    public string DocumentName { get; init; } = string.Empty;

    [Required]
    public DocumentType DocumentType { get; init; }

    [StringLength(500)]
    public string? Notes { get; init; }
}

// Add Condition DTO
public record AddConditionDto
{
    [Required]
    [StringLength(200)]
    public string ConditionName { get; init; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; init; }

    [Required]
    public ConditionType ConditionType { get; init; }

    public DateTime? DueDate { get; init; }
}

// API Response wrapper
public record ApiResponseDto<T>
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public T? Data { get; init; }
    public List<string>? Errors { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public static ApiResponseDto<T> SuccessResponse(T data, string message = "Success")
        => new() { Success = true, Message = message, Data = data };

    public static ApiResponseDto<T> FailResponse(string message, List<string>? errors = null)
        => new() { Success = false, Message = message, Errors = errors };
}
