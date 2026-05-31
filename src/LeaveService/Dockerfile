FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/Shared/Shared.csproj src/Shared/
COPY src/LeaveService/LeaveService.csproj src/LeaveService/

RUN dotnet restore src/LeaveService/LeaveService.csproj

COPY src/Shared/ src/Shared/
COPY src/LeaveService/ src/LeaveService/

WORKDIR /src/src/LeaveService
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "LeaveService.dll"]