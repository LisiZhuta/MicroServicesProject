# Use the official .NET SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

# Copy the project file and restore dependencies
COPY TransactionService/TransactionService.csproj TransactionService/
RUN dotnet restore TransactionService/TransactionService.csproj

# Copy the rest of the application and build it
COPY TransactionService/. ./TransactionService
WORKDIR /src/TransactionService
RUN dotnet build TransactionService.csproj -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish TransactionService.csproj -c Release -o /app/publish

# Use the official .NET runtime image for the final image
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TransactionService.dll"]
