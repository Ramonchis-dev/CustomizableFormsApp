# -------- STAGE 1: Build --------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

# Copy project files
COPY . .

# Install workloads (for WASM support if needed)
RUN dotnet workload install wasm-tools

# Restore dependencies
RUN dotnet restore "./CustomizableFormsApp.csproj"

# Publish the project
RUN dotnet publish "./CustomizableFormsApp.csproj" -c Release -o /app/publish

# -------- STAGE 2: Run --------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

WORKDIR /app

# Copy published output
COPY --from=build /app/publish .

# Expose port 80 for HTTP
EXPOSE 80

# Start the app
ENTRYPOINT ["dotnet", "CustomizableFormsApp.dll"]
