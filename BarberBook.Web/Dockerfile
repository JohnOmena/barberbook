# ===== build =====
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "BarberBook.Web/BarberBook.Web.csproj"
RUN dotnet publish "BarberBook.Web/BarberBook.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ===== runtime =====
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5161
ENV ASPNETCORE_URLS=http://+:5161
ENTRYPOINT ["dotnet","BarberBook.Web.dll"]