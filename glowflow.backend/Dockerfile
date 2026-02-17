# 1. Build Stage
# Using the latest .NET 10 SDK
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# Copy and restore dependencies
COPY *.csproj .
RUN dotnet restore

# Copy everything else and publish the app
COPY . .
RUN dotnet publish -c release -o /app --no-restore

# 2. Runtime Stage
# Using the latest .NET 10 ASP.NET Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

# Copy the published app from the build stage
COPY --from=build /app ./

# Copy your SQLite database into the container root
# Render resets storage on restart, so including it in the image 
# ensures your initial product data is always there.
COPY glowflow.db .

# Render provides the port via the PORT environment variable.
# We bind the ASP.NET Core listener to that port.
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

# Replace 'GlowflowBackend.dll' with your actual project's DLL name
ENTRYPOINT ["dotnet", "glowflow.backend.dll"]