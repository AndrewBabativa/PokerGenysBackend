# Etapa 1: Build
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /app

# Copiar csproj y restaurar dependencias
COPY *.csproj ./
RUN dotnet restore

# Copiar el resto del código y publicar
COPY . ./
RUN dotnet publish -c Release -o /app/publish

# Etapa 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app
COPY --from=build /app/publish .

# Puerto que escuchará Render
ENV ASPNETCORE_URLS=http://+:$PORT
EXPOSE $PORT

# Comando para ejecutar tu app
ENTRYPOINT ["dotnet", "PokerGenysBackend.dll"]
