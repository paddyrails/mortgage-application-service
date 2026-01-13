namespace LoanApplication.API.Clients;

// ============================================
// Customer Service Client
// ============================================
public interface ICustomerServiceClient
{
    Task<CustomerDto?> GetCustomerAsync(Guid customerId);
    Task<CreditDto?> GetCreditAsync(Guid customerId);
    Task<IEnumerable<EmploymentDto>> GetEmploymentsAsync(Guid customerId);
    Task<bool> CustomerExistsAsync(Guid customerId);
}

public record CustomerDto
{
    public Guid Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public int Age { get; init; }
}

public record CreditDto
{
    public int CreditScore { get; init; }
    public string CreditRating { get; init; } = string.Empty;
    public decimal TotalDebt { get; init; }
    public decimal AvailableCredit { get; init; }
}

public record EmploymentDto
{
    public string EmployerName { get; init; } = string.Empty;
    public string? JobTitle { get; init; }
    public decimal AnnualIncome { get; init; }
    public int YearsEmployed { get; init; }
    public bool IsCurrent { get; init; }
}

// ============================================
// Property Service Client
// ============================================
public interface IPropertyServiceClient
{
    Task<PropertyDto?> GetPropertyAsync(Guid propertyId);
    Task<AppraisalDto?> GetAppraisalAsync(Guid propertyId);
    Task<TitleDto?> GetTitleSearchAsync(Guid propertyId);
    Task<bool> PropertyExistsAsync(Guid propertyId);
}

public record PropertyDto
{
    public Guid Id { get; init; }
    public string FullAddress { get; init; } = string.Empty;
    public string PropertyType { get; init; } = string.Empty;
    public decimal EstimatedValue { get; init; }
    public decimal ListingPrice { get; init; }
    public int YearBuilt { get; init; }
    public decimal SquareFeet { get; init; }
}

public record AppraisalDto
{
    public decimal AppraisedValue { get; init; }
    public DateTime AppraisalDate { get; init; }
    public string Status { get; init; } = string.Empty;
}

public record TitleDto
{
    public bool IsClear { get; init; }
    public string Status { get; init; } = string.Empty;
    public bool HasLiens { get; init; }
}

// ============================================
// Loans Service Client
// ============================================
public interface ILoanServiceClient
{
    Task<LoanDto?> GetLoanAsync(Guid loanId);
    Task<LoanDto?> CreateLoanAsync(CreateLoanRequest request);
    Task<bool> FundLoanAsync(Guid loanId, DateTime fundingDate, DateTime firstPaymentDate);
    Task<bool> LoanExistsAsync(Guid loanId);
}

public record LoanDto
{
    public Guid Id { get; init; }
    public string LoanNumber { get; init; } = string.Empty;
    public Guid CustomerId { get; init; }
    public Guid PropertyId { get; init; }
    public decimal PrincipalAmount { get; init; }
    public decimal InterestRate { get; init; }
    public int TermMonths { get; init; }
    public string Status { get; init; } = string.Empty;
    public decimal MonthlyPayment { get; init; }
}

public record CreateLoanRequest
{
    public Guid CustomerId { get; init; }
    public Guid PropertyId { get; init; }
    public decimal PrincipalAmount { get; init; }
    public decimal InterestRate { get; init; }
    public int TermMonths { get; init; }
    public int LoanType { get; init; }
    public decimal? DownPayment { get; init; }
}

// ============================================
// Payments Service Client
// ============================================
public interface IPaymentServiceClient
{
    Task<PaymentScheduleDto?> GetScheduleAsync(Guid loanId);
    Task<PaymentScheduleDto?> CreateScheduleAsync(CreateScheduleRequest request);
    Task<bool> SetupAutoPayAsync(Guid loanId, bool enabled);
}

public record PaymentScheduleDto
{
    public Guid Id { get; init; }
    public Guid LoanId { get; init; }
    public bool IsAutoPay { get; init; }
    public DateTime? NextPaymentDate { get; init; }
    public decimal RegularPaymentAmount { get; init; }
}

public record CreateScheduleRequest
{
    public Guid LoanId { get; init; }
    public Guid CustomerId { get; init; }
    public bool IsAutoPay { get; init; }
    public int PaymentDayOfMonth { get; init; }
    public decimal RegularPaymentAmount { get; init; }
}

// ============================================
// Generic API Response
// ============================================
public record ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string Message { get; init; } = string.Empty;
}
