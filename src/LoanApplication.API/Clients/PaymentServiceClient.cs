using System.Net.Http.Json;

namespace LoanApplication.API.Clients;

public class PaymentServiceClient : IPaymentServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PaymentServiceClient> _logger;

    public PaymentServiceClient(HttpClient httpClient, ILogger<PaymentServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<PaymentScheduleDto?> GetScheduleAsync(Guid loanId)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<PaymentScheduleDto>>($"/api/payments/schedule/loan/{loanId}");
            return response?.Success == true ? response.Data : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching schedule for loan {LoanId}", loanId);
            return null;
        }
    }

    public async Task<PaymentScheduleDto?> CreateScheduleAsync(CreateScheduleRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/payments/schedule", request);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<PaymentScheduleDto>>();
                return result?.Data;
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating schedule");
            return null;
        }
    }

    public async Task<bool> SetupAutoPayAsync(Guid loanId, bool enabled)
    {
        try
        {
            var response = await _httpClient.PatchAsync($"/api/payments/schedule/loan/{loanId}/autopay?enabled={enabled}", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting autopay for loan {LoanId}", loanId);
            return false;
        }
    }
}
