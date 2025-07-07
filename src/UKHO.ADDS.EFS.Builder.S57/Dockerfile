# Use the .NET 9 SDK image for build *and* run
FROM mcr.microsoft.com/dotnet/sdk:9.0

# Optional build configuration argument (default to Release)
ARG BUILD_CONFIGURATION=Release

# Create work directories
WORKDIR /app

# Copy csproj files first to leverage layer caching
COPY ["UKHO.ADDS.EFS.Builder.S57/UKHO.ADDS.EFS.Builder.S57.csproj", "UKHO.ADDS.EFS.Builder.S57/"]
COPY ["UKHO.ADDS.EFS.Builder.Common/UKHO.ADDS.EFS.Builder.Common.csproj", "UKHO.ADDS.EFS.Builder.Common/"]
COPY ["UKHO.ADDS.EFS.Domain/UKHO.ADDS.EFS.Domain.csproj", "UKHO.ADDS.EFS.Domain/"]

# Restore only
RUN dotnet restore "UKHO.ADDS.EFS.Builder.S57/UKHO.ADDS.EFS.Builder.S57.csproj"

# Copy all source
COPY . .

# Build and publish to a folder
    RUN dotnet publish "UKHO.ADDS.EFS.Builder.S57/UKHO.ADDS.EFS.Builder.S57.csproj" \
-c $BUILD_CONFIGURATION \
-o /app/publish

# Set the working directory to the publish folder
WORKDIR /app/publish

# Define default entrypoint
ENTRYPOINT ["dotnet", "UKHO.ADDS.EFS.Builder.S57.dll"]
