using Microsoft.AspNetCore.Mvc;
using LoanApplication.API.DTOs;
using LoanApplication.API.Services;
using LoanApplication.API.Orchestration;
using LoanApplication.API.Models;

namespace LoanApplication.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ApplicationsController : ControllerBase
{
    private readonly IApplicationService _applicationService;
    private readonly IWorkflowOrchestrator _orchestrator;
    private readonly ILogger<ApplicationsController> _logger;

    public ApplicationsController(
        IApplicationService applicationService,
        IWorkflowOrchestrator orchestrator,
        ILogger<ApplicationsController> logger)
    {
        _applicationService = applicationService;
        _orchestrator = orchestrator;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponseDto<IEnumerable<ApplicationSummaryDto>>>> GetAll()
    {
        var applications = await _applicationService.GetAllApplicationsAsync();
        return Ok(ApiResponseDto<IEnumerable<ApplicationSummaryDto>>.SuccessResponse(applications));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponseDto<ApplicationResponseDto>>> GetById(Guid id)
    {
        var application = await _applicationService.GetApplicationByIdAsync(id);
        if (application == null)
            return NotFound(ApiResponseDto<ApplicationResponseDto>.FailResponse($"Application {id} not found"));
        return Ok(ApiResponseDto<ApplicationResponseDto>.SuccessResponse(application));
    }

    [HttpGet("customer/{customerId:guid}")]
    public async Task<ActionResult<ApiResponseDto<IEnumerable<ApplicationSummaryDto>>>> GetByCustomer(Guid customerId)
    {
        var applications = await _applicationService.GetApplicationsByCustomerAsync(customerId);
        return Ok(ApiResponseDto<IEnumerable<ApplicationSummaryDto>>.SuccessResponse(applications));
    }

    [HttpGet("status/{status}")]
    public async Task<ActionResult<ApiResponseDto<IEnumerable<ApplicationSummaryDto>>>> GetByStatus(ApplicationStatus status)
    {
        var applications = await _applicationService.GetApplicationsByStatusAsync(status);
        return Ok(ApiResponseDto<IEnumerable<ApplicationSummaryDto>>.SuccessResponse(applications));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponseDto<ApplicationResponseDto>>> Create([FromBody] CreateApplicationDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponseDto<ApplicationResponseDto>.FailResponse("Validation failed", errors));
        }

        try
        {
            var application = await _applicationService.CreateApplicationAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = application.Id },
                ApiResponseDto<ApplicationResponseDto>.SuccessResponse(application, "Application created"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponseDto<ApplicationResponseDto>.FailResponse(ex.Message));
        }
    }

    [HttpPost("{id:guid}/submit")]
    public async Task<ActionResult<ApiResponseDto<ApplicationResponseDto>>> Submit(Guid id, [FromBody] SubmitApplicationDto dto)
    {
        try
        {
            var application = await _applicationService.SubmitApplicationAsync(id, dto);
            if (application == null)
                return NotFound(ApiResponseDto<ApplicationResponseDto>.FailResponse($"Application {id} not found"));
            return Ok(ApiResponseDto<ApplicationResponseDto>.SuccessResponse(application, "Application submitted"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponseDto<ApplicationResponseDto>.FailResponse(ex.Message));
        }
    }

    [HttpPost("{id:guid}/underwrite")]
    public async Task<ActionResult<ApiResponseDto<ApplicationResponseDto>>> StartUnderwriting(Guid id)
    {
        try
        {
            var application = await _orchestrator.StartUnderwritingAsync(id);
            return Ok(ApiResponseDto<ApplicationResponseDto>.SuccessResponse(application, "Underwriting completed"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponseDto<ApplicationResponseDto>.FailResponse(ex.Message));
        }
    }

    [HttpPost("{id:guid}/decision")]
    public async Task<ActionResult<ApiResponseDto<ApplicationResponseDto>>> ProcessDecision(Guid id, [FromBody] ApplicationDecisionDto dto)
    {
        try
        {
            var application = await _orchestrator.ProcessDecisionAsync(id, dto);
            return Ok(ApiResponseDto<ApplicationResponseDto>.SuccessResponse(application, 
                dto.Approved ? "Application approved" : "Application rejected"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponseDto<ApplicationResponseDto>.FailResponse(ex.Message));
        }
    }

    [HttpPost("{id:guid}/fund")]
    public async Task<ActionResult<ApiResponseDto<ApplicationResponseDto>>> CreateLoanAndFund(Guid id)
    {
        try
        {
            var application = await _orchestrator.CreateLoanAndFundAsync(id);
            return Ok(ApiResponseDto<ApplicationResponseDto>.SuccessResponse(application, "Loan created and funded"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponseDto<ApplicationResponseDto>.FailResponse(ex.Message));
        }
    }

    [HttpPost("{id:guid}/withdraw")]
    public async Task<ActionResult<ApiResponseDto<object>>> Withdraw(Guid id, [FromQuery] string? reason = null)
    {
        var result = await _applicationService.WithdrawApplicationAsync(id, reason);
        if (!result)
            return NotFound(ApiResponseDto<object>.FailResponse($"Application {id} not found"));
        return Ok(ApiResponseDto<object>.SuccessResponse(new { Id = id }, "Application withdrawn"));
    }

    // Document endpoints
    [HttpPost("{id:guid}/documents")]
    public async Task<ActionResult<ApiResponseDto<DocumentResponseDto>>> AddDocument(Guid id, [FromBody] AddDocumentDto dto)
    {
        var document = await _applicationService.AddDocumentAsync(id, dto);
        return Ok(ApiResponseDto<DocumentResponseDto>.SuccessResponse(document, "Document added"));
    }

    [HttpPatch("documents/{documentId:guid}/status")]
    public async Task<ActionResult<ApiResponseDto<object>>> UpdateDocumentStatus(Guid documentId, [FromQuery] DocumentStatus status)
    {
        var result = await _applicationService.UpdateDocumentStatusAsync(documentId, status);
        if (!result)
            return NotFound(ApiResponseDto<object>.FailResponse("Document not found"));
        return Ok(ApiResponseDto<object>.SuccessResponse(new { DocumentId = documentId, Status = status.ToString() }, "Status updated"));
    }

    // Condition endpoints
    [HttpPost("{id:guid}/conditions")]
    public async Task<ActionResult<ApiResponseDto<ConditionResponseDto>>> AddCondition(Guid id, [FromBody] AddConditionDto dto)
    {
        var condition = await _applicationService.AddConditionAsync(id, dto);
        return Ok(ApiResponseDto<ConditionResponseDto>.SuccessResponse(condition, "Condition added"));
    }

    [HttpPatch("conditions/{conditionId:guid}/status")]
    public async Task<ActionResult<ApiResponseDto<object>>> UpdateConditionStatus(Guid conditionId, [FromQuery] ConditionStatus status)
    {
        var result = await _applicationService.UpdateConditionStatusAsync(conditionId, status);
        if (!result)
            return NotFound(ApiResponseDto<object>.FailResponse("Condition not found"));
        return Ok(ApiResponseDto<object>.SuccessResponse(new { ConditionId = conditionId, Status = status.ToString() }, "Status updated"));
    }
}
