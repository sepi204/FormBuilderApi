# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files first for caching
COPY ["YekAbr.Api/YekAbr.Api.csproj", "YekAbr.Api/"]
COPY ["YekAbr.Infrastructure/YekAbr.Infrastructure.csproj", "YekAbr.Infrastructure/"]
COPY ["YekAbr.Services/YekAbr.Services.csproj", "YekAbr.Services/"]
COPY ["YekAbr.Domain/YekAbr.Domain.csproj", "YekAbr.Domain/"]

# Restore
RUN dotnet restore "YekAbr.Api/YekAbr.Api.csproj"

# Copy everything else
COPY . .

# Publish
WORKDIR "/src/YekAbr.Api"
RUN dotnet publish "YekAbr.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Development

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "YekAbr.Api.dll"]
