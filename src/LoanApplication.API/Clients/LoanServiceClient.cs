using System.Net.Http.Json;

namespace LoanApplication.API.Clients;

public class LoanServiceClient : ILoanServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LoanServiceClient> _logger;

    public LoanServiceClient(HttpClient httpClient, ILogger<LoanServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<LoanDto?> GetLoanAsync(Guid loanId)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<LoanDto>>($"/api/loans/{loanId}?enrich=false");
            return response?.Success == true ? response.Data : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching loan {LoanId}", loanId);
            return null;
        }
    }

    public async Task<LoanDto?> CreateLoanAsync(CreateLoanRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/loans", request);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<LoanDto>>();
                return result?.Data;
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating loan");
            return null;
        }
    }

    public async Task<bool> FundLoanAsync(Guid loanId, DateTime fundingDate, DateTime firstPaymentDate)
    {
        try
        {
            var request = new { FundingDate = fundingDate, FirstPaymentDate = firstPaymentDate };
            var response = await _httpClient.PostAsJsonAsync($"/api/loans/{loanId}/fund", request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error funding loan {LoanId}", loanId);
            return false;
        }
    }

    public async Task<bool> LoanExistsAsync(Guid loanId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/loans/{loanId}?enrich=false");
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }
}
