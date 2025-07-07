# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the solution file (assuming it's at the root of your repo)
COPY ["CustomizableFormsApp.sln", "./"]

# Copy the main server project file
# The path is relative to the Dockerfile's location (the repo root)
COPY ["CustomizableFormsApp/CustomizableFormsApp.csproj", "CustomizableFormsApp/"]

# Restore dependencies for the entire solution
# This will restore packages for CustomizableFormsApp.csproj
RUN dotnet restore "CustomizableFormsApp.sln"

# Copy the rest of the source code (including all other files and folders for CustomizableFormsApp)
# The '.' refers to the root of your repository (the build context)
COPY . .

# Change working directory to the server project
WORKDIR /src/CustomizableFormsApp

# Publish the server project in Release configuration
# The output will be placed in /app/publish inside the build container
RUN dotnet publish "CustomizableFormsApp.csproj" -c Release -o /app/publish

# Stage 2: Create the final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copy the published application from the build stage to the final image
COPY --from=build /app/publish .

# Expose the port your ASP.NET Core application listens on
# By default, ASP.NET Core often listens on 80 and 443 in production containers.
# You might need to configure your app to listen on 8080 if Render expects that.
EXPOSE 8080

# Define the entrypoint for the application
# This tells Docker how to start your Blazor server application
ENTRYPOINT ["dotnet", "CustomizableFormsApp.dll"]
