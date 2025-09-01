FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
# Copia todo o repositório (projetos têm referências cruzadas)
COPY . .
RUN dotnet restore "BarberBook.Web/BarberBook.Web.csproj"
RUN dotnet publish "BarberBook.Web/BarberBook.Web.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:5161
EXPOSE 5161
ENTRYPOINT ["dotnet","BarberBook.Web.dll"]
