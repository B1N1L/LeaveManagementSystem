FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/Shared/Shared.csproj src/Shared/
COPY src/NotificationService/NotificationService.csproj src/NotificationService/

RUN dotnet restore src/NotificationService/NotificationService.csproj

COPY src/Shared/ src/Shared/
COPY src/NotificationService/ src/NotificationService/

WORKDIR /src/src/NotificationService
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "NotificationService.dll"]