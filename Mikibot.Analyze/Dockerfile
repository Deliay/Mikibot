#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM archlinux/archlinux as run
USER root
RUN pacman -Sy
RUN pacman -S dotnet-runtime
RUN pacman -S ffmpeg
RUN pacman -S opencore-amr

FROM mcr.microsoft.com/dotnet/runtime:8.0-alpine AS base
USER app
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Mikibot.Analyze/Mikibot.Analyze.csproj", "Mikibot.Analyze/"]
COPY ["Mikibot.AutoClipper.Abstract/Mikibot.AutoClipper.Abstract.csproj", "Mikibot.AutoClipper.Abstract/"]
COPY ["Mikibot.BuildingBlocks/Mikibot.BuildingBlocks.csproj", "Mikibot.BuildingBlocks/"]
COPY ["Mikibot.Database/Mikibot.Database.csproj", "Mikibot.Database/"]
COPY ["Mikibot.StableDiffusion.WebUi.Api/Mikibot.StableDiffusion.WebUi.Api.csproj", "Mikibot.StableDiffusion.WebUi.Api/"]
RUN dotnet restore "./Mikibot.Analyze/./Mikibot.Analyze.csproj"
COPY . .
WORKDIR "/src/Mikibot.Analyze"
RUN dotnet build "./Mikibot.Analyze.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Mikibot.Analyze.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM run AS final
USER app
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Mikibot.Analyze.dll"]