#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY mexcbot.Api/mexcbot.Api.csproj mexcbot.Api/
COPY sp.Core/sp.Core.csproj sp.Core/
COPY sp.Core.Mailer/sp.Core.Mailer.csproj sp.Core.Mailer/
COPY sp.Core.Token/sp.Core.Token.csproj sp.Core.Token/
RUN dotnet restore "mexcbot.Api/mexcbot.Api.csproj"
COPY . .
WORKDIR "/src/mexcbot.Api"
RUN dotnet build "mexcbot.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "mexcbot.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY mexcbot.Api/JsonData ./
ENTRYPOINT ["dotnet", "mexcbot.Api.dll"]
