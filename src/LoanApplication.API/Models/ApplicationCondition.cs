using System.ComponentModel.DataAnnotations;

namespace LoanApplication.API.Models;

public class ApplicationCondition
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid ApplicationId { get; set; }

    [Required]
    [StringLength(200)]
    public string ConditionName { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    public ConditionType ConditionType { get; set; }

    public ConditionStatus Status { get; set; } = ConditionStatus.Pending;

    public DateTime? DueDate { get; set; }

    public DateTime? SatisfiedAt { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Application? Application { get; set; }
}

public enum ConditionType
{
    PriorToApproval = 1,
    PriorToClosing = 2,
    PriorToFunding = 3,
    PostClosing = 4
}

public enum ConditionStatus
{
    Pending = 1,
    InProgress = 2,
    Satisfied = 3,
    Waived = 4,
    NotMet = 5
}
