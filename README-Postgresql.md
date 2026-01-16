# PostgreSQL Integration Guide for Mortgage Microservices

## Overview

This guide explains how to configure your .NET microservices to use the PostgreSQL database deployed on OpenShift.

## Database Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    PostgreSQL Server                         │
│                    Database: mortgage_db                     │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐           │
│  │  customer   │ │  property   │ │   loans     │           │
│  │  _service   │ │  _service   │  │  _service   │           │
│  │   schema    │ │   schema    │ │   schema    │           │
│  └──────┬──────┘ └──────┬──────┘ └──────┬──────┘           │
│         │               │               │                    │
│  ┌──────┴──────┐ ┌──────┴──────┐ ┌──────┴──────┐           │
│  │ customer    │ │ property    │ │ loans       │           │
│  │ _user       │ │ _user       │ │ _user       │           │
│  └─────────────┘ └─────────────┘ └─────────────┘           │
│                                                              │
│  ┌─────────────┐ ┌─────────────┐                           │
│  │  payments   │ │ application │                           │
│  │  _service   │ │  _service   │                           │
│  │   schema    │ │   schema    │                           │
│  └──────┬──────┘ └──────┬──────┘                           │
│         │               │                                    │
│  ┌──────┴──────┐ ┌──────┴──────┐                           │
│  │ payments    │ │ application │                           │
│  │ _user       │ │ _user       │                           │
│  └─────────────┘ └─────────────┘                           │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

## Workflows

| Workflow | Description |
|----------|-------------|
| `1-deploy-postgresql.yml` | Deploy PostgreSQL with all schemas |
| `2-delete-postgresql.yml` | Delete PostgreSQL and cleanup |
| `3-run-migrations.yml` | Run EF Core migrations per service |
| `4-backup-restore.yml` | Backup/restore database |

## Configuration

### 1. Add NuGet Packages

Add these packages to each microservice:

```xml
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.0" />
```

### 2. Configure DbContext

```csharp
// CustomerDbContext.cs
public class CustomerDbContext : DbContext
{
    public CustomerDbContext(DbContextOptions<CustomerDbContext> options) 
        : base(options) { }

    public DbSet<Customer> Customers { get; set; }
    public DbSet<Address> Addresses { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Set the schema for this service
        modelBuilder.HasDefaultSchema("customer_service");
        
        base.OnModelCreating(modelBuilder);
    }
}
```

### 3. Register DbContext in Program.cs

```csharp
// Program.cs
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<CustomerDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "customer_service");
        npgsqlOptions.EnableRetryOnFailure(3);
    }));
```

### 4. Update appsettings.json (for local development)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=mortgage_db;Username=customer_user;Password=localdevpassword;Search Path=customer_service"
  }
}
```

### 5. Update Kubernetes Deployment

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: customer-service
spec:
  template:
    spec:
      containers:
        - name: customer-service
          image: your-registry/customer-service:latest
          env:
            # Option 1: Full connection string from secret
            - name: ConnectionStrings__DefaultConnection
              valueFrom:
                secretKeyRef:
                  name: postgresql-customer-service
                  key: CONNECTION_STRING
            
            # Option 2: Individual values (alternative)
            # - name: DB_HOST
            #   valueFrom:
            #     secretKeyRef:
            #       name: postgresql-customer-service
            #       key: DB_HOST
            # - name: DB_PASSWORD
            #   valueFrom:
            #     secretKeyRef:
            #       name: postgresql-customer-service
            #       key: DB_PASSWORD
```

## Entity Examples

### Customer Service Entities

```csharp
// Entities/Customer.cs
namespace CustomerService.Entities;

public class Customer
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string SocialSecurityNumber { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public ICollection<Address> Addresses { get; set; } = new List<Address>();
}

public class Address
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public AddressType Type { get; set; }
    public bool IsPrimary { get; set; }
    
    // Navigation
    public Customer Customer { get; set; } = null!;
}

public enum AddressType
{
    Home,
    Work,
    Mailing
}
```

### Loans Service Entities

```csharp
// Entities/Loan.cs
namespace LoansService.Entities;

public class Loan
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }  // References customer in customer_service schema
    public Guid PropertyId { get; set; }  // References property in property_service schema
    public decimal Principal { get; set; }
    public decimal InterestRate { get; set; }
    public int TermMonths { get; set; }
    public LoanType Type { get; set; }
    public LoanStatus Status { get; set; }
    public DateTime ApplicationDate { get; set; }
    public DateTime? ApprovalDate { get; set; }
    public DateTime? ClosingDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation
    public ICollection<LoanDocument> Documents { get; set; } = new List<LoanDocument>();
}

public enum LoanType
{
    Conventional,
    FHA,
    VA,
    USDA,
    Jumbo
}

public enum LoanStatus
{
    Draft,
    Submitted,
    UnderReview,
    Approved,
    Denied,
    Closed,
    Funded
}
```

## Creating Migrations

### Initial Migration

```bash
# In your service project directory
cd src/CustomerService

# Create initial migration
dotnet ef migrations add InitialCreate --context CustomerDbContext

# Apply migration (local)
dotnet ef database update --context CustomerDbContext
```

### Using the Workflow

1. Go to **Actions** → **Run Database Migrations**
2. Select the service
3. Choose action: `update`, `script`, or `rollback`
4. Run the workflow

## Connection String Format

The secrets contain connection strings in this format:

```
Host=postgresql;Port=5432;Database=mortgage_db;Username=customer_user;Password=xxx;Search Path=customer_service
```

| Component | Value |
|-----------|-------|
| Host | `postgresql` (service name) |
| Port | `5432` |
| Database | `mortgage_db` |
| Username | `{service}_user` |
| Password | Auto-generated |
| Search Path | `{service}_service` (schema) |

## Cross-Service Data Access

Since each service has its own schema, they can't directly query other schemas. Use these patterns:

### Option 1: API Calls (Recommended)

```csharp
// In LoansService, get customer via API
public class CustomerApiClient
{
    private readonly HttpClient _httpClient;
    
    public async Task<Customer?> GetCustomerAsync(Guid customerId)
    {
        var response = await _httpClient.GetAsync($"/api/customers/{customerId}");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Customer>();
        }
        return null;
    }
}
```

### Option 2: Read-Only Cross-Schema Access

If needed, you can grant read access to specific tables:

```sql
-- Grant loans_user read access to customer_service.customers
GRANT USAGE ON SCHEMA customer_service TO loans_user;
GRANT SELECT ON customer_service.customers TO loans_user;
```

## Secrets Reference

| Secret Name | Service | Keys |
|-------------|---------|------|
| `postgresql-admin` | Admin | `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_DB` |
| `postgresql-customer-service` | Customer | `DB_HOST`, `DB_PORT`, `DB_NAME`, `DB_SCHEMA`, `DB_USER`, `DB_PASSWORD`, `CONNECTION_STRING` |
| `postgresql-property-service` | Property | Same keys |
| `postgresql-loans-service` | Loans | Same keys |
| `postgresql-payments-service` | Payments | Same keys |
| `postgresql-application-service` | Application | Same keys |

## Troubleshooting

### Connection Refused

```bash
# Check PostgreSQL is running
oc get pods -l app=postgresql

# Check service exists
oc get svc postgresql

# Test connection from another pod
oc run psql-test --rm -it --image=postgres:15 -- psql -h postgresql -U postgres -d mortgage_db
```

### Permission Denied

```bash
# Check user permissions
oc exec -it $(oc get pod -l app=postgresql -o name) -- psql -U postgres -d mortgage_db -c "\du"

# Check schema permissions
oc exec -it $(oc get pod -l app=postgresql -o name) -- psql -U postgres -d mortgage_db -c "\dn+"
```

### Schema Doesn't Exist

```bash
# Re-run init script
POD=$(oc get pod -l app=postgresql -o jsonpath='{.items[0].metadata.name}')
oc exec -it $POD -- bash /docker-entrypoint-initdb.d/init-schemas.sh
```
