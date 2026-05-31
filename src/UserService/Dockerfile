# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files
COPY src/Shared/Shared.csproj src/Shared/
COPY src/UserService/UserService.csproj src/UserService/

# Restore dependencies
RUN dotnet restore src/UserService/UserService.csproj

# Copy all source code
COPY src/Shared/ src/Shared/
COPY src/UserService/ src/UserService/

# Build and publish
WORKDIR /src/src/UserService
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "UserService.dll"]