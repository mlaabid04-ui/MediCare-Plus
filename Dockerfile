# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy and restore only the API project (ignore MAUI)
COPY ["HospitalApp.API/HospitalApp.API.csproj", "HospitalApp.API/"]
RUN dotnet restore "HospitalApp.API/HospitalApp.API.csproj"

# Copy all API source and publish
COPY HospitalApp.API/ HospitalApp.API/
WORKDIR "/src/HospitalApp.API"
RUN dotnet publish "HospitalApp.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Railway injects $PORT at runtime — listen on it
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "HospitalApp.API.dll"]
