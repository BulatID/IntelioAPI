#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443
EXPOSE 4001


FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["IntelioAPI/IntelioAPI.csproj", "IntelioAPI/"]
RUN dotnet restore "IntelioAPI/IntelioAPI.csproj"
COPY . .
WORKDIR "/src/IntelioAPI"
RUN dotnet build "IntelioAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "IntelioAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "IntelioAPI.dll"]