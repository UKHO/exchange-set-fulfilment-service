FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ESSFulfilmentService.Builder.csproj .
RUN dotnet restore ./ESSFulfilmentService.Builder.csproj
COPY . .
RUN dotnet build ./ESSFulfilmentService.Builder.csproj -c $BUILD_CONFIGURATION  -o /app/build

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
RUN apt-get update && apt-get install -y openjdk-17-jdk
RUN apt-get update && apt-get install -y wget
RUN wget https://downloads.apache.org/tomcat/tomcat-10/v10.1.39/bin/apache-tomcat-10.1.39.tar.gz && tar -xzvf apache-tomcat-10.1.39.tar.gz && \ 
mv apache-tomcat-10.1.39 /usr/local/tomcat && rm -rf apache-tomcat-10.1.39.tar.gz && rm -rf /var/lib/apt/lists/*
COPY xchg-2.4.war /usr/local/tomcat/webapps/

WORKDIR /app
COPY --from=build /app/build .

RUN echo '#!/bin/bash\n' > /startup.sh && \
    echo '/usr/local/tomcat/bin/startup.sh\n' >> /startup.sh && \
    echo 'dotnet /app/ESSFulfilmentService.Builder.dll\n' >> /startup.sh && \
    chmod +x /startup.sh

ENTRYPOINT ["bash","/startup.sh"]
EXPOSE 8080 5000
