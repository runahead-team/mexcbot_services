#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY multexbot.Api/multexbot.Api.csproj multexbot.Api/
COPY sp.Core/sp.Core.csproj sp.Core/
COPY sp.Core.Mailer/sp.Core.Mailer.csproj sp.Core.Mailer/
COPY sp.Core.Token/sp.Core.Token.csproj sp.Core.Token/
RUN dotnet restore "multexBot.Api/multexBot.Api.csproj"
COPY . .
WORKDIR "/src/multexBot.Api"
RUN dotnet build "multexBot.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "multexBot.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY multexBot.Api/mails ./
COPY multexBot.Api/mails/mail_subjects.json ./
ENTRYPOINT ["dotnet", "multexBot.Api.dll"]
