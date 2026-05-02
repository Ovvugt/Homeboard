# Stage 1: Vue build
FROM node:24-alpine AS frontend
WORKDIR /app
COPY Homeboard.Frontend/package.json Homeboard.Frontend/package-lock.json ./
RUN npm ci
COPY Homeboard.Frontend/ .
RUN npm run build
# output: /app/dist

# Stage 2: .NET build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend
WORKDIR /src
COPY Homeboard.Backend/Directory.Build.props Homeboard.Backend/Directory.Packages.props ./
COPY Homeboard.Backend/Homeboard.*/*.csproj ./
RUN for f in Homeboard.*.csproj; do mkdir -p "${f%.csproj}" && mv "$f" "${f%.csproj}/"; done
RUN dotnet restore Homeboard.API/Homeboard.API.csproj
COPY Homeboard.Backend/ .
ARG VERSION=1.0.0
RUN dotnet publish Homeboard.API/Homeboard.API.csproj -c Release -o /app/publish --no-restore /p:Version=$VERSION

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=backend /app/publish .
COPY --from=frontend /app/dist ./wwwroot
RUN mkdir -p /data && chown -R $APP_UID:$APP_UID /data
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
ENV ConnectionStrings__DefaultConnection="Data Source=/data/homeboard.db;Cache=Shared;Foreign Keys=True"
EXPOSE 8080
VOLUME ["/data"]
USER $APP_UID
ENTRYPOINT ["dotnet", "Homeboard.API.dll"]
