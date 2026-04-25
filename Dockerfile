# Базовий образ з .NET 9 Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER root
WORKDIR /app
EXPOSE 8080

# Встановлюємо Chromium та ChromeDriver для Selenium
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
       chromium \
       chromium-driver \
    && rm -rf /var/lib/apt/lists/*

# Вказуємо шлях до ChromeDriver, щоб C# код міг його знайти
ENV CHECKIT_CHROMEDRIVER_PATH=/usr/bin/chromedriver

# Створюємо директорію для логів заздалегідь та даємо права доступу користувачу "app"
RUN mkdir -p /app/Logs \
    && chown -R app:app /app/Logs

# Образ для збірки з .NET 9 SDK
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Копіюємо файли проєктів і відновлюємо залежності
COPY ["CheckIT.Web/CheckIT.Web.csproj", "CheckIT.Web/"]
RUN dotnet restore "CheckIT.Web/CheckIT.Web.csproj"

# Копіюємо весь код та білдимо
COPY . .
WORKDIR "/src/CheckIT.Web"
RUN dotnet build "CheckIT.Web.csproj" -c Release -o /app/build

# Публікація оптимізованої версії
FROM build AS publish
RUN dotnet publish "CheckIT.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Фінальний образ
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Запускаємо під непривілейованим користувачем (стандарт для .NET контейнерів)
USER app

ENTRYPOINT ["dotnet", "CheckIT.Web.dll"]
