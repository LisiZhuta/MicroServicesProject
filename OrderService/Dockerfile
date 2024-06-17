# Use the official .NET SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

# Copy the project file and restore dependencies
COPY OrderService/OrderService.csproj OrderService/
RUN dotnet restore OrderService/OrderService.csproj

# Copy the rest of the application and build it
COPY OrderService/. ./OrderService
WORKDIR /src/OrderService
RUN dotnet build OrderService.csproj -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish OrderService.csproj -c Release -o /app/publish

# Use the official .NET runtime image for the final image
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OrderService.dll"]
