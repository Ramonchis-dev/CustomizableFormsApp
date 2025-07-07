# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the solution file first
# This allows dotnet restore to understand the entire solution structure
COPY ["CustomizableFormsApp.sln", "./"]

# Copy project files for both the server and client projects
# This is crucial for dotnet restore to find them
COPY ["CustomizableFormsApp/CustomizableFormsApp.csproj", "CustomizableFormsApp/"]
COPY ["CustomizableFormsApp.Client/CustomizableFormsApp.Client.csproj", "CustomizableFormsApp.Client/"]

# Restore dependencies for the entire solution
# This will download all NuGet packages for both projects
RUN dotnet restore "CustomizableFormsApp.sln"

# Copy the rest of the source code (including all other files and folders)
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
