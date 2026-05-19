# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY *.sln .
COPY GoWithFlow.API/*.csproj GoWithFlow.API/
COPY GoWithFlow.Application/*.csproj GoWithFlow.Application/
COPY GoWithFlow.Domain/*.csproj GoWithFlow.Domain/
COPY GoWithFlow.Infrastructure/*.csproj GoWithFlow.Infrastructure/

RUN dotnet restore

COPY . .
WORKDIR /src/GoWithFlow.API
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_ENVIRONMENT=Production
ENV DOTNET_hostBuilder__reloadConfigOnChange=false
ENV ASPNETCORE_hostBuilder__reloadConfigOnChange=false
ENV PORT=10000
EXPOSE 10000
COPY --from=build /app/publish .
ENTRYPOINT ["sh", "-c", "export ASPNETCORE_URLS=\"${ASPNETCORE_URLS:-http://0.0.0.0:${PORT:-10000}}\"; exec dotnet GoWithFlow.API.dll"]
