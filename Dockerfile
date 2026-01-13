FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5005

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/LoanApplication.API/LoanApplication.API.csproj", "LoanApplication.API/"]
RUN dotnet restore "LoanApplication.API/LoanApplication.API.csproj"
COPY src/LoanApplication.API/. LoanApplication.API/
WORKDIR "/src/LoanApplication.API"
RUN dotnet build "LoanApplication.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LoanApplication.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:5005
ENTRYPOINT ["dotnet", "LoanApplication.API.dll"]
