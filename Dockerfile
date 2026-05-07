FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY NuGet.Config ./
COPY RestApiDdd.slnx ./
COPY RestApiDdd.Api/RestApiDdd.Api.csproj RestApiDdd.Api/
COPY RestApiDdd.Domain/RestApiDdd.Domain.csproj RestApiDdd.Domain/
COPY RestApiDdd.Infrastructure/RestApiDdd.Infrastructure.csproj RestApiDdd.Infrastructure/
COPY RestApiDdd.Service/RestApiDdd.Service.csproj RestApiDdd.Service/
RUN dotnet restore RestApiDdd.Api/RestApiDdd.Api.csproj

COPY . .
RUN dotnet publish RestApiDdd.Api/RestApiDdd.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM runtime AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "RestApiDdd.Api.dll"]
