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

# ВИПРАВЛЕННЯ #1: Додаємо параметри Chrome для headless режиму
ENV CHROME_FLAGS="--headless --no-sandbox --disable-dev-shm-usage --disable-gpu"

# Створюємо директорію для логів
RUN mkdir -p /app/Logs \
    && chown -R app:app /app/Logs

# ВИПРАВЛЕННЯ #2: Створюємо тимчасову директорію для Chrome з правами
RUN mkdir -p /tmp/chrome-data \
    && chown -R app:app /tmp/chrome-data \
    && chmod 1777 /tmp /dev/shm

# Образ для збірки з .NET 9 SDK
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Копіюємо файли проєктів і відновлюємо залежності
COPY ["Application/CheckIT.Web/CheckIT.Web.csproj", "Application/CheckIT.Web/"]
RUN dotnet restore "Application/CheckIT.Web/CheckIT.Web.csproj"

# Копіюємо весь код та білдимо
COPY . .
WORKDIR "/src/Application/CheckIT.Web"
RUN dotnet build "CheckIT.Web.csproj" -c Release -o /app/build

# Публікація оптимізованої версії
FROM build AS publish
RUN dotnet publish "CheckIT.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

RUN mkdir -p /home/DataProtection-Keys && chmod 777 /home/DataProtection-Keys

# Фінальний образ
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# ВИПРАВЛЕННЯ #3: Копіюємо права на DataProtection директорію
RUN mkdir -p /home/DataProtection-Keys && chown -R app:app /home/DataProtection-Keys

# Запускаємо під непривілейованим користувачем
USER app

ENTRYPOINT ["dotnet", "CheckIT.Web.dll"]
