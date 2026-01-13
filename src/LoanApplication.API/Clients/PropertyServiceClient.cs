using System.Net.Http.Json;

namespace LoanApplication.API.Clients;

public class PropertyServiceClient : IPropertyServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PropertyServiceClient> _logger;

    public PropertyServiceClient(HttpClient httpClient, ILogger<PropertyServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<PropertyDto?> GetPropertyAsync(Guid propertyId)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<PropertyDto>>($"/api/properties/{propertyId}");
            return response?.Success == true ? response.Data : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching property {PropertyId}", propertyId);
            return null;
        }
    }

    public async Task<AppraisalDto?> GetAppraisalAsync(Guid propertyId)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<AppraisalDto>>($"/api/properties/{propertyId}/appraisal");
            return response?.Success == true ? response.Data : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching appraisal for {PropertyId}", propertyId);
            return null;
        }
    }

    public async Task<TitleDto?> GetTitleSearchAsync(Guid propertyId)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<TitleDto>>($"/api/properties/{propertyId}/title");
            return response?.Success == true ? response.Data : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching title for {PropertyId}", propertyId);
            return null;
        }
    }

    public async Task<bool> PropertyExistsAsync(Guid propertyId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/properties/{propertyId}");
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }
}
