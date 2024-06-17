# Use the official .NET SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

# Copy the project file and restore dependencies
COPY ProductService/ProductService.csproj ProductService/
RUN dotnet restore ProductService/ProductService.csproj

# Copy the rest of the application and build it
COPY ProductService/. ./ProductService
WORKDIR /src/ProductService
RUN dotnet build ProductService.csproj -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish ProductService.csproj -c Release -o /app/publish

# Use the official .NET runtime image for the final image
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ProductService.dll"]
