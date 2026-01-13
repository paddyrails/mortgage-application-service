using LoanApplication.API.DTOs;
using LoanApplication.API.Models;

namespace LoanApplication.API.Services;

public interface IApplicationService
{
    Task<IEnumerable<ApplicationSummaryDto>> GetAllApplicationsAsync();
    Task<ApplicationResponseDto?> GetApplicationByIdAsync(Guid id);
    Task<IEnumerable<ApplicationSummaryDto>> GetApplicationsByCustomerAsync(Guid customerId);
    Task<IEnumerable<ApplicationSummaryDto>> GetApplicationsByStatusAsync(ApplicationStatus status);
    Task<ApplicationResponseDto> CreateApplicationAsync(CreateApplicationDto dto);
    Task<ApplicationResponseDto?> SubmitApplicationAsync(Guid id, SubmitApplicationDto dto);
    Task<ApplicationResponseDto?> UpdateStatusAsync(Guid id, ApplicationStatus status, string? reason = null);
    Task<bool> WithdrawApplicationAsync(Guid id, string? reason = null);
    
    // Documents
    Task<DocumentResponseDto> AddDocumentAsync(Guid applicationId, AddDocumentDto dto);
    Task<bool> UpdateDocumentStatusAsync(Guid documentId, DocumentStatus status);
    
    // Conditions
    Task<ConditionResponseDto> AddConditionAsync(Guid applicationId, AddConditionDto dto);
    Task<bool> UpdateConditionStatusAsync(Guid conditionId, ConditionStatus status);
}
