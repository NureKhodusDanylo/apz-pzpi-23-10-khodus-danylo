# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["RobDeliveryAPI.sln", "./"]
COPY ["RobDeliveryAPI/RobDeliveryAPI.csproj", "RobDeliveryAPI/"]
COPY ["Application/Application.csproj", "Application/"]
COPY ["Infrastructure/Infrastructure.csproj", "Infrastructure/"]
COPY ["Entities/Entities.csproj", "Entities/"]

# Restore dependencies
RUN dotnet restore "RobDeliveryAPI.sln"

# Copy all source code
COPY . .

# Build the application
WORKDIR "/src/RobDeliveryAPI"
RUN dotnet build "RobDeliveryAPI.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "RobDeliveryAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install base64 for database restoration
RUN apt-get update && apt-get install -y coreutils && rm -rf /var/lib/apt/lists/*

# Setup Persistent Storage for Hugging Face Spaces
# Setup Persistent Storage for Hugging Face Spaces
RUN mkdir -p /data/Uploads /data/Backups && \
    chown -R 1000:1000 /data && \
    ln -sf /data/Uploads /app/Uploads && \
    ln -sf /data/Backups /app/Backups && \
    chown -R 1000:1000 /app

# Copy published application
COPY --from=publish --chown=1000:1000 /app/publish .

# Copy seeding data and entrypoint
COPY db_seed.txt .
COPY entrypoint.sh .
RUN chmod +x entrypoint.sh && chown 1000:1000 entrypoint.sh db_seed.txt

# Switch to the Hugging Face non-root user
USER 1000

# Expose port required by Hugging Face Spaces
EXPOSE 7860

# Set environment variables
ENV ASPNETCORE_URLS=http://+:7860
ENV ASPNETCORE_ENVIRONMENT=Development

# Set entrypoint to our script
ENTRYPOINT ["./entrypoint.sh"]
