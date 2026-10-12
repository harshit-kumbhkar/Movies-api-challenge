FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["ApiApplication.csproj", "."]
RUN dotnet restore "./ApiApplication.csproj"

COPY . .
RUN dotnet publish "ApiApplication.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .

# Railway injects PORT; default 8080 for local docker runs
CMD ["sh", "-c", "dotnet ApiApplication.dll --urls http://0.0.0.0:${PORT:-8080}"]
