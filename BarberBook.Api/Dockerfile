# ===== build =====
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# (opcional) copie csproj em camadas para cache de restore
# COPY BarberBook.sln ./
# COPY BarberBook.Domain/*.csproj BarberBook.Domain/
# COPY BarberBook.Application/*.csproj BarberBook.Application/
# COPY BarberBook.Infrastructure/*.csproj BarberBook.Infrastructure/
# COPY BarberBook.Api/*.csproj BarberBook.Api/
# RUN dotnet restore BarberBook.Api/BarberBook.Api.csproj

# caminho simples (funciona, só perde um pouco de cache):
COPY . .
RUN dotnet restore "BarberBook.Api/BarberBook.Api.csproj"
RUN dotnet publish "BarberBook.Api/BarberBook.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ===== runtime =====
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet","BarberBook.Api.dll"]