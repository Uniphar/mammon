FROM mcr.microsoft.com/dotnet/sdk:8.0 AS sdk
WORKDIR /src
COPY ["Mammon/Mammon.csproj", "Mammon/"]
RUN dotnet restore "./Mammon/Mammon.csproj"
COPY . .
WORKDIR "/src/Mammon"
RUN dotnet publish "./Mammon.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0
ENV COST_CENTRE_DEF_FILE=costCentreRules.json
USER app
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8082
EXPOSE 8082
COPY --from=sdk /app/publish .
COPY $COST_CENTRE_DEF_FILE /app
ENTRYPOINT ["dotnet", "Mammon.dll"]