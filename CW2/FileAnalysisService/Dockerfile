FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["FileAnalysisService/FileAnalysisService.csproj", "FileAnalysisService/"]
RUN dotnet restore "FileAnalysisService/FileAnalysisService.csproj"
COPY . .
WORKDIR "/src/FileAnalysisService"
RUN dotnet build "FileAnalysisService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FileAnalysisService.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 80
ENV ASPNETCORE_URLS=http://+:80
ENTRYPOINT ["dotnet", "FileAnalysisService.dll"] 