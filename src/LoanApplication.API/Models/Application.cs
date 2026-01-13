using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoanApplication.API.Models;

public class Application
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(20)]
    public string ApplicationNumber { get; set; } = string.Empty;

    // References to other services
    [Required]
    public Guid CustomerId { get; set; }

    [Required]
    public Guid PropertyId { get; set; }

    public Guid? LoanId { get; set; }  // Set after loan is created

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal RequestedLoanAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal DownPaymentAmount { get; set; }

    [Required]
    public int RequestedTermMonths { get; set; }

    [Required]
    public LoanPurpose LoanPurpose { get; set; }

    [Required]
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Draft;

    public ApplicationType ApplicationType { get; set; } = ApplicationType.Purchase;

    // Calculated fields
    [Column(TypeName = "decimal(5,2)")]
    public decimal? LTV { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal? DTI { get; set; }

    [Column(TypeName = "decimal(5,3)")]
    public decimal? OfferedInterestRate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? ApprovedLoanAmount { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? UnderwritingStartedAt { get; set; }
    public DateTime? DecisionAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    [StringLength(500)]
    public string? DecisionReason { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    // Navigation
    public List<ApplicationDocument> Documents { get; set; } = new();
    public List<ApplicationCondition> Conditions { get; set; } = new();
    public List<ApplicationStatusHistory> StatusHistory { get; set; } = new();
    public Underwriting? Underwriting { get; set; }
}

public enum ApplicationStatus
{
    Draft = 1,
    Submitted = 2,
    DocumentsRequested = 3,
    DocumentsReceived = 4,
    InReview = 5,
    Underwriting = 6,
    ConditionalApproval = 7,
    Approved = 8,
    Rejected = 9,
    CounterOffer = 10,
    AcceptedByBorrower = 11,
    ClearToClose = 12,
    Closed = 13,
    Funded = 14,
    Withdrawn = 15,
    Expired = 16
}

public enum LoanPurpose
{
    Purchase = 1,
    Refinance = 2,
    CashOutRefinance = 3,
    HomeEquity = 4,
    Construction = 5
}

public enum ApplicationType
{
    Purchase = 1,
    Refinance = 2,
    HELOC = 3,
    ReverseMortgage = 4
}
