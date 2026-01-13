using System.Net.Http.Json;

namespace LoanApplication.API.Clients;

public class CustomerServiceClient : ICustomerServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CustomerServiceClient> _logger;

    public CustomerServiceClient(HttpClient httpClient, ILogger<CustomerServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<CustomerDto?> GetCustomerAsync(Guid customerId)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<CustomerDto>>($"/api/customers/{customerId}");
            return response?.Success == true ? response.Data : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching customer {CustomerId}", customerId);
            return null;
        }
    }

    public async Task<CreditDto?> GetCreditAsync(Guid customerId)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<CreditDto>>($"/api/customers/{customerId}/credit");
            return response?.Success == true ? response.Data : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching credit for {CustomerId}", customerId);
            return null;
        }
    }

    public async Task<IEnumerable<EmploymentDto>> GetEmploymentsAsync(Guid customerId)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<IEnumerable<EmploymentDto>>>($"/api/customers/{customerId}/employments");
            return response?.Success == true ? response.Data ?? Enumerable.Empty<EmploymentDto>() : Enumerable.Empty<EmploymentDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching employments for {CustomerId}", customerId);
            return Enumerable.Empty<EmploymentDto>();
        }
    }

    public async Task<bool> CustomerExistsAsync(Guid customerId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/customers/{customerId}");
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }
}
