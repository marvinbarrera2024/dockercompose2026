# 1. Etapa de compilación utilizando el SDK de .NET 10
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# Copiar el archivo de proyecto y restaurar dependencias
COPY *.csproj .
RUN dotnet restore

# Copiar el código fuente y compilar la aplicación
COPY . .
RUN dotnet publish -c Release -o /app

# 2. Etapa de ejecución utilizando el Runtime ligero de .NET 10
FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app .
CMD ["dotnet", "worker-service.dll"]