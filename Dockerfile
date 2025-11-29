# --- Build Stage ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /app

# Copiar la solución
COPY *.sln ./

# Copiar proyectos
COPY PokerGenys.API/*.csproj ./PokerGenys.API/
COPY PokerGenys.Domain/*.csproj ./PokerGenys.Domain/
COPY PokerGenys.Infrastructure/*.csproj ./PokerGenys.Infrastructure/
COPY PokerGenys.Services/*.csproj ./PokerGenys.Services/
COPY PokerGenys.Shared/*.csproj ./PokerGenys.Shared/

# Restaurar dependencias
RUN dotnet restore

# Copiar todo el código
COPY . .

# Publicar en Release
RUN dotnet publish PokerGenys.API/PokerGenys.API.csproj -c Release -o /app/publish

# --- Runtime Stage ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 10000

ENTRYPOINT ["dotnet", "PokerGenys.API.dll", "--urls", "http://0.0.0.0:10000"]
