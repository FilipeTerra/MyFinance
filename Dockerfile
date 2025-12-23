# Use a imagem do SDK do .NET 9 para compilar
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copia os arquivos de projeto (ajuste se os nomes das pastas forem diferentes)
COPY ["MyFinance.Api/MyFinance.Api.csproj", "MyFinance.Api/"]
COPY ["MyFinance.Application/MyFinance.Application.csproj", "MyFinance.Application/"]
COPY ["MyFinance.Domain/MyFinance.Domain.csproj", "MyFinance.Domain/"]
COPY ["MyFinance.Infrastructure/MyFinance.Infrastructure.csproj", "MyFinance.Infrastructure/"]

# Restaura as dependências
RUN dotnet restore "MyFinance.Api/MyFinance.Api.csproj"

# Copia todo o resto do código
COPY . .

# Compila e publica a API
WORKDIR "/src/MyFinance.Api"
RUN dotnet publish "MyFinance.Api.csproj" -c Release -o /app/publish

# Imagem final de execução
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .

# Define a porta que o Render espera (geralmente a variável PORT)
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "MyFinance.Api.dll"]