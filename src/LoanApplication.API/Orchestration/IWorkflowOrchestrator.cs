using LoanApplication.API.DTOs;

namespace LoanApplication.API.Orchestration;

public interface IWorkflowOrchestrator
{
    Task<ApplicationResponseDto> StartUnderwritingAsync(Guid applicationId);
    Task<ApplicationResponseDto> ProcessDecisionAsync(Guid applicationId, ApplicationDecisionDto decision);
    Task<ApplicationResponseDto> CreateLoanAndFundAsync(Guid applicationId);
}
