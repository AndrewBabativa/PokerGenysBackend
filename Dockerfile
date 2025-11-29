# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Set working directory
WORKDIR /app

# Copiar archivos de solución y proyectos
COPY *.sln ./
COPY PokerGenysBackend/*.csproj ./PokerGenysBackend/

# Restaurar dependencias
RUN dotnet restore

# Copiar todo el código
COPY . ./

# Publicar la app en Release
RUN dotnet publish PokerGenysBackend/PokerGenysBackend.csproj -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

WORKDIR /app

COPY --from=build /app/publish .

# Exponer puerto (Render usa 10000 por defecto)
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_USE_POLLING_FILE_WATCHER=1
ENV DOTNET_HOST_PATH=/usr/share/dotnet/dotnet
EXPOSE 10000

# Comando para ejecutar
ENTRYPOINT ["dotnet", "PokerGenysBackend.dll"]
