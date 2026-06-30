FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["AutoZapSaaS.API/AutoZapSaaS.API.csproj", "AutoZapSaaS.API/"]
COPY ["AutoZapSaaS.Domain/AutoZapSaaS.Domain.csproj", "AutoZapSaaS.Domain/"]
COPY ["AutoZapSaaS.Application/AutoZapSaaS.Application.csproj", "AutoZapSaaS.Application/"]
COPY ["AutoZapSaaS.Infrastructure/AutoZapSaaS.Infrastructure.csproj", "AutoZapSaaS.Infrastructure/"]
RUN dotnet restore "AutoZapSaaS.API/AutoZapSaaS.API.csproj"
COPY . .
WORKDIR "/src/AutoZapSaaS.API"
RUN dotnet build "AutoZapSaaS.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AutoZapSaaS.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AutoZapSaaS.API.dll"]
