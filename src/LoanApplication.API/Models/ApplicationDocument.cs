using System.ComponentModel.DataAnnotations;

namespace LoanApplication.API.Models;

public class ApplicationDocument
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid ApplicationId { get; set; }

    [Required]
    [StringLength(100)]
    public string DocumentName { get; set; } = string.Empty;

    [Required]
    public DocumentType DocumentType { get; set; }

    [StringLength(500)]
    public string? FilePath { get; set; }

    public DocumentStatus Status { get; set; } = DocumentStatus.Required;

    public DateTime? RequestedAt { get; set; }

    public DateTime? ReceivedAt { get; set; }

    public DateTime? ApprovedAt { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Application? Application { get; set; }
}

public enum DocumentType
{
    // Identity
    DriversLicense = 1,
    Passport = 2,
    SSNCard = 3,
    
    // Income
    PayStubs = 10,
    W2Forms = 11,
    TaxReturns = 12,
    BankStatements = 13,
    
    // Employment
    EmploymentVerification = 20,
    OfferLetter = 21,
    
    // Property
    PurchaseAgreement = 30,
    AppraisalReport = 31,
    TitleReport = 32,
    HomeownersInsurance = 33,
    
    // Other
    GiftLetter = 40,
    ExplanationLetter = 41,
    Other = 99
}

public enum DocumentStatus
{
    Required = 1,
    Requested = 2,
    Received = 3,
    UnderReview = 4,
    Approved = 5,
    Rejected = 6,
    Waived = 7
}
