#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["GetworkStratumProxy.ConsoleApp/GetworkStratumProxy.ConsoleApp.csproj", "GetworkStratumProxy.ConsoleApp/"]
COPY ["GetworkStratumProxy/GetworkStratumProxy.csproj", "GetworkStratumProxy/"]
RUN dotnet restore "GetworkStratumProxy.ConsoleApp/GetworkStratumProxy.ConsoleApp.csproj"
COPY . .
WORKDIR "/src/GetworkStratumProxy.ConsoleApp"
RUN dotnet build "GetworkStratumProxy.ConsoleApp.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "GetworkStratumProxy.ConsoleApp.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GetworkStratumProxy.ConsoleApp.dll"]