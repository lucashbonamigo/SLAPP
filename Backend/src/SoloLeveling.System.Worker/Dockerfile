# Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/SoloLeveling.System.Worker/SoloLeveling.System.Worker.csproj", "SoloLeveling.System.Worker/"]
COPY ["src/SoloLeveling.System.Core/SoloLeveling.System.Core.csproj", "SoloLeveling.System.Core/"]
RUN dotnet restore "SoloLeveling.System.Worker/SoloLeveling.System.Worker.csproj"

COPY src/SoloLeveling.System.Worker/ SoloLeveling.System.Worker/
COPY src/SoloLeveling.System.Core/ SoloLeveling.System.Core/
RUN dotnet publish "SoloLeveling.System.Worker/SoloLeveling.System.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "SoloLeveling.System.Worker.dll"]
