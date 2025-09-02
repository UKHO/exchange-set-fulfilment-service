# Use the .NET 9 SDK image for build *and* run
FROM mcr.microsoft.com/dotnet/sdk:9.0

# Optional build configuration argument (default to Release)
ARG BUILD_CONFIGURATION=Release

# Create work directories
WORKDIR /app

# Copy csproj files first to leverage layer caching
COPY ["UKHO.ADDS.EFS.Builder.S63/UKHO.ADDS.EFS.Builder.S63.csproj", "UKHO.ADDS.EFS.Builder.S63/"]
COPY ["UKHO.ADDS.EFS.Domain.Infrastructure.Builders/UKHO.ADDS.EFS.Domain.Infrastructure.Builders.csproj", "UKHO.ADDS.EFS.Domain.Infrastructure.Builders/"]
COPY ["UKHO.ADDS.EFS.Domain/UKHO.ADDS.EFS.Domain.csproj", "UKHO.ADDS.EFS.Domain/"]
COPY ["UKHO.ADDS.EFS.Domain.Services/UKHO.ADDS.EFS.Domain.Services.csproj", "UKHO.ADDS.EFS.Domain.Services/"]
COPY ["UKHO.ADDS.EFS.Domain.Infrastructure/UKHO.ADDS.EFS.Domain.Infrastructure.csproj", "UKHO.ADDS.EFS.Domain.Infrastructure/"]
COPY ["UKHO.ADDS.Clients.Common/UKHO.ADDS.Clients.Common.csproj", "UKHO.ADDS.Clients.Common/"]
COPY ["UKHO.ADDS.Clients.FileShareService.ReadOnly/UKHO.ADDS.Clients.FileShareService.ReadOnly.csproj", "UKHO.ADDS.Clients.FileShareService.ReadOnly/"]
COPY ["UKHO.ADDS.Clients.FileShareService.ReadWrite/UKHO.ADDS.Clients.FileShareService.ReadWrite.csproj", "UKHO.ADDS.Clients.FileShareService.ReadWrite/"]

# Restore only
RUN dotnet restore "UKHO.ADDS.EFS.Builder.S63/UKHO.ADDS.EFS.Builder.S63.csproj"

# Copy all source
COPY . .

# Build and publish to a folder
    RUN dotnet publish "UKHO.ADDS.EFS.Builder.S63/UKHO.ADDS.EFS.Builder.S63.csproj" \
-c $BUILD_CONFIGURATION \
-o /app/publish

# Set the working directory to the publish folder
WORKDIR /app/publish

# Define default entrypoint
ENTRYPOINT ["dotnet", "UKHO.ADDS.EFS.Builder.S63.dll"]
