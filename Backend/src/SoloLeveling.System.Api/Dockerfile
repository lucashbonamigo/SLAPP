# Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/SoloLeveling.System.Api/SoloLeveling.System.Api.csproj", "SoloLeveling.System.Api/"]
COPY ["src/SoloLeveling.System.Core/SoloLeveling.System.Core.csproj", "SoloLeveling.System.Core/"]
RUN dotnet restore "SoloLeveling.System.Api/SoloLeveling.System.Api.csproj"

COPY src/SoloLeveling.System.Api/ SoloLeveling.System.Api/
COPY src/SoloLeveling.System.Core/ SoloLeveling.System.Core/
RUN dotnet publish "SoloLeveling.System.Api/SoloLeveling.System.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "SoloLeveling.System.Api.dll"]
