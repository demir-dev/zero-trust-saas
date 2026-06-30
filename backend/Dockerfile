FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ZeroTrustSaaS.Api/ZeroTrustSaaS.Api.csproj ZeroTrustSaaS.Api/
COPY ZeroTrustSaaS.Application/ZeroTrustSaaS.Application.csproj ZeroTrustSaaS.Application/
COPY ZeroTrustSaaS.Domain/ZeroTrustSaaS.Domain.csproj ZeroTrustSaaS.Domain/
COPY ZeroTrustSaaS.Infrastructure/ZeroTrustSaaS.Infrastructure.csproj ZeroTrustSaaS.Infrastructure/
RUN dotnet restore ZeroTrustSaaS.Api/ZeroTrustSaaS.Api.csproj
COPY . .
RUN dotnet publish ZeroTrustSaaS.Api/ZeroTrustSaaS.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "ZeroTrustSaaS.Api.dll"]
