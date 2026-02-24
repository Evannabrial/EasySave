FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
#USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["EasyLogServer/EasyLogServer.csproj", "EasyLogServer/"]
RUN dotnet restore "EasyLogServer/EasyLogServer.csproj"
COPY . .
WORKDIR "/src/EasyLogServer"
RUN dotnet build "./EasyLogServer.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./EasyLogServer.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 4242
ENTRYPOINT ["dotnet", "EasyLogServer.dll"]

