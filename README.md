# Loan Application Service (Orchestrator)

Main orchestrator service for the Mortgage Application system. Coordinates all other microservices.

## ⚠️ Dependencies (ALL SERVICES)

This service depends on all other services:
- **Customer Service** (http://localhost:5001)
- **Property Service** (http://localhost:5002)
- **Loans Service** (http://localhost:5003)
- **Payments Service** (http://localhost:5004)

## Application Workflow

```
1. Create Application    → POST /api/applications
2. Submit Application    → POST /api/applications/{id}/submit
3. Run Underwriting     → POST /api/applications/{id}/underwrite
4. Make Decision        → POST /api/applications/{id}/decision
5. Create & Fund Loan   → POST /api/applications/{id}/fund
```

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | /api/applications | Get all applications |
| GET | /api/applications/{id} | Get application details |
| GET | /api/applications/customer/{id} | Get by customer |
| GET | /api/applications/status/{status} | Get by status |
| POST | /api/applications | Create application |
| POST | /api/applications/{id}/submit | Submit application |
| POST | /api/applications/{id}/underwrite | Run underwriting |
| POST | /api/applications/{id}/decision | Process decision |
| POST | /api/applications/{id}/fund | Create loan & fund |
| POST | /api/applications/{id}/withdraw | Withdraw application |

## Running

```bash
# Start ALL services first (in separate terminals):
# 1. Customer Service (port 5001)
# 2. Property Service (port 5002)
# 3. Loans Service (port 5003)
# 4. Payments Service (port 5004)

# Then start this orchestrator:
cd src/LoanApplication.API
dotnet run
```

Swagger UI: http://localhost:5005/swagger
