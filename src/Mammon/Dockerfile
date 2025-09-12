FROM mcr.microsoft.com/dotnet/sdk:8.0 AS restore
WORKDIR /src
COPY ["Mammon/Mammon.csproj", "Mammon/"]
RUN dotnet restore "Mammon/Mammon.csproj"

FROM restore AS build
ARG BUILD_CONFIGURATION=Release
COPY . .
WORKDIR "/src/Mammon"
RUN dotnet build "Mammon.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Mammon.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0
ENV COST_CENTRE_DEF_FILE=costCentreRules.json
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8082
EXPOSE 8082
COPY --from=publish /app/publish .
COPY $COST_CENTRE_DEF_FILE /app
USER app
ENTRYPOINT ["dotnet", "Mammon.dll"]
