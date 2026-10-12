FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["ApiApplication.csproj", "."]
RUN dotnet restore "./ApiApplication.csproj"

COPY . .
RUN dotnet publish "ApiApplication.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

# .NET 8+ images default HTTP_PORTS=8080; Railway proxies to $PORT instead
ENV ASPNETCORE_HTTP_PORTS=
ENV ASPNETCORE_URLS=

# Bind to Railway's assigned PORT (fallback 8080 for local runs)
CMD ["sh", "-c", "echo \"Listening on PORT=${PORT:-8080}\" && exec dotnet ApiApplication.dll --urls http://0.0.0.0:${PORT:-8080}"]
