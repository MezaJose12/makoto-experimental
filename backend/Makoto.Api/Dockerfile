# Imagen de la API de Makoto. Se construye desde la RAÍZ del repositorio:
#   docker build -f backend/Makoto.Api/Dockerfile -t makoto-api .
# (la raíz hace falta para copiar datos/audios dentro de la imagen, como necesita Render)

# ── Etapa 1: compilar ────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY backend/Makoto.Api/Makoto.Api.csproj backend/Makoto.Api/
RUN dotnet restore backend/Makoto.Api/Makoto.Api.csproj
COPY backend/Makoto.Api/ backend/Makoto.Api/
RUN dotnet publish backend/Makoto.Api/Makoto.Api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ── Etapa 2: ejecutar ────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
# Librería GSSAPI: evita advertencias de Npgsql al negociar TLS en Linux
RUN apt-get update \
 && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
 && rm -rf /var/lib/apt/lists/*
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080 \
    Audios__Ruta=/app/datos/audios
COPY --from=build /app/publish .
COPY datos/audios/ /app/datos/audios/
# Usuario sin privilegios que trae la imagen oficial de .NET
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Makoto.Api.dll"]
