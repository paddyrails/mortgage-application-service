using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoanApplication.API.Models;

public class Underwriting
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid ApplicationId { get; set; }

    // Credit Analysis
    public int? CreditScore { get; set; }

    [StringLength(20)]
    public string? CreditRating { get; set; }

    public bool CreditApproved { get; set; }

    // Income Analysis
    [Column(TypeName = "decimal(18,2)")]
    public decimal? GrossMonthlyIncome { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? MonthlyDebtPayments { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal? CalculatedDTI { get; set; }

    public bool IncomeVerified { get; set; }

    // Property Analysis
    [Column(TypeName = "decimal(18,2)")]
    public decimal? AppraisedValue { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal? CalculatedLTV { get; set; }

    public bool PropertyApproved { get; set; }

    public bool TitleClear { get; set; }

    // Employment
    public bool EmploymentVerified { get; set; }

    public int? YearsEmployed { get; set; }

    // Assets
    [Column(TypeName = "decimal(18,2)")]
    public decimal? TotalAssets { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? LiquidAssets { get; set; }

    public bool AssetsVerified { get; set; }

    // Decision
    public UnderwritingDecision Decision { get; set; } = UnderwritingDecision.Pending;

    [StringLength(50)]
    public string? UnderwriterName { get; set; }

    [StringLength(1000)]
    public string? DecisionNotes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    // Navigation
    public Application? Application { get; set; }
}

public enum UnderwritingDecision
{
    Pending = 1,
    Approved = 2,
    ApprovedWithConditions = 3,
    Suspended = 4,
    Denied = 5
}
