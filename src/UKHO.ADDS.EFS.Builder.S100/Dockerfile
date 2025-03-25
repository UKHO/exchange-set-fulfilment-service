# Visual Studio does not like Dockerfiles that have multiple stages - it only recognises the first stage. So, we have to use a single stage Dockerfile.
# Also note that this dockerfile must be in the same directory as the .csproj file, otherwise Visual Studio will not recognise it as a debug target.
# That's 8 hours of my life I won't get back.

FROM mcr.microsoft.com/dotnet/sdk:9.0

ARG BUILD_CONFIGURATION=Debug
ARG CACHEBUSTER=1

ENV JAVA_HOME=/usr/lib/jvm/java-17-openjdk-amd64
ENV CATALINA_HOME=/usr/local/tomcat
ENV PATH="$JAVA_HOME/bin:$CATALINA_HOME/bin:$PATH"

# Install OpenJDK, wget, and Tomcat
RUN apt-get update && apt-get install -y openjdk-17-jdk wget && \
    wget https://downloads.apache.org/tomcat/tomcat-9/v9.0.102/bin/apache-tomcat-9.0.102.tar.gz && \
    tar -xzvf apache-tomcat-9.0.102.tar.gz && \
    mv apache-tomcat-9.0.102 /usr/local/tomcat && \
    rm -rf /var/lib/apt/lists/*

RUN echo "Tomcat installed in /usr/local/tomcat" && ls /usr/local/tomcat

WORKDIR /src

# Copy and restore projects
COPY ["Directory.Build.props", "."]
COPY ["UKHO.ADDS.EFS.Builder.S100/UKHO.ADDS.EFS.Builder.S100.csproj", "UKHO.ADDS.EFS.Builder.S100/"]
COPY ["UKHO.ADDS.EFS.Common/UKHO.ADDS.EFS.Common.csproj", "UKHO.ADDS.EFS.Common/"]
RUN dotnet restore UKHO.ADDS.EFS.Builder.S100/UKHO.ADDS.EFS.Builder.S100.csproj

# Copy remaining files and build
COPY . .
WORKDIR /src/UKHO.ADDS.EFS.Builder.S100
RUN dotnet build UKHO.ADDS.EFS.Builder.S100.csproj -c $BUILD_CONFIGURATION -o /app/build

# Copy WAR to Tomcat
COPY UKHO.ADDS.EFS.Builder.S100/Tomcat/xchg-2.7.war /usr/local/tomcat/webapps/

# No need to copy anything — the build output is already there
WORKDIR /app/build

# The .NET process orchestrates Tomcat, so just start that. Visual Studio also hijacks the ENTRYPOINT and injects its own
# stuff as PID 1, so any bash shells don't run properly when debugging from Visual Studio...
ENTRYPOINT ["dotnet", "/app/build/UKHO.ADDS.EFS.Builder.S100.dll"]

EXPOSE 8080 5000


