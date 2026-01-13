using Microsoft.AspNetCore.Mvc;
using LoanApplication.API.Clients;

namespace LoanApplication.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly ICustomerServiceClient _customerClient;
    private readonly IPropertyServiceClient _propertyClient;
    private readonly ILoanServiceClient _loanClient;
    private readonly IPaymentServiceClient _paymentClient;

    public HealthController(
        ICustomerServiceClient customerClient,
        IPropertyServiceClient propertyClient,
        ILoanServiceClient loanClient,
        IPaymentServiceClient paymentClient)
    {
        _customerClient = customerClient;
        _propertyClient = propertyClient;
        _loanClient = loanClient;
        _paymentClient = paymentClient;
    }

    [HttpGet]
    public IActionResult Health() => Ok(new 
    { 
        Status = "Healthy", 
        Service = "LoanApplication.API (Orchestrator)", 
        Timestamp = DateTime.UtcNow,
        Dependencies = new[] { "Customer.API", "Property.API", "Loans.API", "Payments.API" }
    });

    [HttpGet("live")]
    public IActionResult Live() => Ok(new { Status = "Alive" });

    [HttpGet("ready")]
    public async Task<IActionResult> Ready()
    {
        var customerOk = await CheckAsync(() => _customerClient.CustomerExistsAsync(Guid.Empty));
        var propertyOk = await CheckAsync(() => _propertyClient.PropertyExistsAsync(Guid.Empty));
        var loanOk = await CheckAsync(() => _loanClient.LoanExistsAsync(Guid.Empty));
        var paymentOk = await CheckAsync(() => _paymentClient.GetScheduleAsync(Guid.Empty));

        var allHealthy = customerOk && propertyOk && loanOk && paymentOk;

        var status = new
        {
            Status = allHealthy ? "Ready" : "Degraded",
            Dependencies = new
            {
                CustomerService = customerOk ? "Healthy" : "Unhealthy",
                PropertyService = propertyOk ? "Healthy" : "Unhealthy",
                LoansService = loanOk ? "Healthy" : "Unhealthy",
                PaymentsService = paymentOk ? "Healthy" : "Unhealthy"
            }
        };

        return allHealthy ? Ok(status) : StatusCode(503, status);
    }

    private async Task<bool> CheckAsync<T>(Func<Task<T>> check)
    {
        try { await check(); return true; }
        catch { return false; }
    }
}
