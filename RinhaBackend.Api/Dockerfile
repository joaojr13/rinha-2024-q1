FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

ENV ASPNETCORE_URLS=http://+:80

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG configuration=Release
WORKDIR /src
COPY ["RinhaBackend.Api/RinhaBackend.Api.csproj", "RinhaBackend.Api/"]
RUN dotnet restore "RinhaBackend.Api/RinhaBackend.Api.csproj"
COPY . .
WORKDIR "/src/RinhaBackend.Api"
RUN dotnet build "RinhaBackend.Api.csproj" -c $configuration -o /app/build

FROM build AS publish
ARG configuration=Release
RUN dotnet publish "RinhaBackend.Api.csproj" -c $configuration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RinhaBackend.Api.dll"]
