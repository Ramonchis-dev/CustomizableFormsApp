# Use official .NET SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy everything and restore dependencies
COPY . ./

# Publish the application
RUN dotnet publish CustomizableFormsApp.csproj -c Release -o /app/publish

# Use a lightweight runtime image for hosting the app
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Expose port (default for ASP.NET Core)
EXPOSE 80
ENV ASPNETCORE_URLS=http://+:80

# Run the app
ENTRYPOINT ["dotnet", "CustomizableFormsApp.dll"]
