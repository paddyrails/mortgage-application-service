using System.ComponentModel.DataAnnotations;

namespace LoanApplication.API.Models;

public class ApplicationStatusHistory
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid ApplicationId { get; set; }

    [Required]
    public ApplicationStatus FromStatus { get; set; }

    [Required]
    public ApplicationStatus ToStatus { get; set; }

    [StringLength(500)]
    public string? Reason { get; set; }

    [StringLength(100)]
    public string? ChangedBy { get; set; }

    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Application? Application { get; set; }
}
