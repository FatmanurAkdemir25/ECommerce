FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ECommerce.slnx ./
COPY src/ECommerce.Domain/*.csproj src/ECommerce.Domain/
COPY src/ECommerce.Application/*.csproj src/ECommerce.Application/
COPY src/ECommerce.Infrastructure/*.csproj src/ECommerce.Infrastructure/
COPY src/ECommerce.API/*.csproj src/ECommerce.API/
RUN dotnet restore src/ECommerce.API/ECommerce.API.csproj

COPY . .
RUN dotnet publish src/ECommerce.API -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "ECommerce.API.dll"]
