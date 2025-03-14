# Use the official Tomcat image as a base
FROM tomcat:9.0
 
# Copy the WAR file to the webapps directory of Tomcat
# COPY /src/xchg-2.4.war /usr/local/tomcat/webapps/
COPY xchg-2.4.war /usr/local/tomcat/webapps/ 
# Expose the default port
EXPOSE 8080
 
# Optional: Set environment variables
# ENV some_variable=some_value

#for dot net
# Use the official .NET Core SDK image as the base image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy the .csproj and restore as distinct layers
COPY ../../essfulfilmentservice.builder/*.csproj ./
RUN dotnet restore

# Copy the remaining source code and build the application
COPY . ./
RUN dotnet publish -c Release -o out

# Build the runtime image
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app/out .

# Entry point when the container starts
ENTRYPOINT ["dotnet", "ConsoleAppDockerDemo.dll"]
