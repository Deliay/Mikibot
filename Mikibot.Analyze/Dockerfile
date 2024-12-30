FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Mikibot.Analyze/Mikibot.Analyze.csproj", "Mikibot.Analyze/"]
COPY ["Mikibot.AutoClipper.Abstract/Mikibot.AutoClipper.Abstract.csproj", "Mikibot.AutoClipper.Abstract/"]
COPY ["Mikibot.BuildingBlocks/Mikibot.BuildingBlocks.csproj", "Mikibot.BuildingBlocks/"]
COPY ["Mikibot.Database/Mikibot.Database.csproj", "Mikibot.Database/"]
COPY ["Mikibot.StableDiffusion.WebUi.Api/Mikibot.StableDiffusion.WebUi.Api.csproj", "Mikibot.StableDiffusion.WebUi.Api/"]
RUN dotnet restore "Mikibot.Analyze/Mikibot.Analyze.csproj"
COPY . .
WORKDIR "/src/Mikibot.Analyze"
RUN dotnet build "Mikibot.Analyze.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Mikibot.Analyze.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Mikibot.Analyze.dll"]
