FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY . ./
RUN dotnet restore "release-pilot.sln"
RUN dotnet publish "ReleasePilot.OutboxPublisher/ReleasePilot.OutboxPublisher.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./

ENTRYPOINT ["dotnet", "ReleasePilot.OutboxPublisher.dll"]
