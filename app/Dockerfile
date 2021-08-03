# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY . .
WORKDIR /app/SearchWeb
RUN dotnet restore

# Copy everything else and build
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:5.0
WORKDIR /app
COPY --from=build-env /app/SearchWeb/out .

COPY ./init_container.sh .
COPY ./sshd_config .

RUN apt update && apt install -y --no-install-recommends openssh-server && echo "root:Docker!" | chpasswd && chmod 755 ./init_container.sh
COPY sshd_config /etc/ssh/

EXPOSE 8080 80 2222

ENTRYPOINT ["./init_container.sh"]
