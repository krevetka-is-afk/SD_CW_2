FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["FileAnalisysService/FileAnalisysService.csproj", "FileAnalisysService/"]
RUN dotnet restore "./FileAnalisysService/FileAnalisysService.csproj"
COPY . .
WORKDIR "/src/FileAnalisysService"
RUN dotnet build "./FileAnalisysService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./FileAnalisysService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

USER root
RUN mkdir -p /app/wordclouds && chmod -R 777 /app/wordclouds
USER app

ENTRYPOINT ["dotnet", "FileAnalisysService.dll"]