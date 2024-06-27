# create the build container
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
LABEL stage=build
WORKDIR /app
COPY shared shared
COPY bot bot
COPY proto proto
WORKDIR /app/bot
RUN dotnet publish -c Release -o out

# create the runtime container
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/bot/out .
ENTRYPOINT ["dotnet", "bot.dll"]
