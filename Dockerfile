# Estágio único de build: Node (frontend) + .NET (API). Alpine SDK não traz Node; instalamos.
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Instalar Node para o Target do csproj que roda npm run build
RUN apk add --no-cache nodejs npm

COPY CSSistemas.sln ./
COPY CSSistemas.API/CSSistemas.API.csproj CSSistemas.API/
COPY CSSistemas.Application/CSSistemas.Application.csproj CSSistemas.Application/
COPY CSSistemas.Domain/CSSistemas.Domain.csproj CSSistemas.Domain/
COPY CSSistemas.Infrastructure/CSSistemas.Infrastructure.csproj CSSistemas.Infrastructure/
COPY cssistemas-web/package.json cssistemas-web/package-lock.json* cssistemas-web/

RUN dotnet restore "CSSistemas.API/CSSistemas.API.csproj"
RUN cd cssistemas-web && npm ci --legacy-peer-deps 2>/dev/null || npm install --legacy-peer-deps

COPY . .
RUN dotnet publish "CSSistemas.API/CSSistemas.API.csproj" -c Release -o /app/publish

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "CSSistemas.API.dll"]
