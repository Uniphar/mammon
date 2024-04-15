FROM mcr.microsoft.com/dotnet/sdk:8.0 AS sdk
WORKDIR /src
COPY ["Mammon/Mammon.csproj", "Mammon/"]
RUN dotnet restore "./Mammon/Mammon.csproj"
COPY . .
WORKDIR "/src/Mammon"
RUN dotnet publish "./Mammon.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081
COPY --from=sdk /app/publish .
ENTRYPOINT ["dotnet", "Mammon.dll"]