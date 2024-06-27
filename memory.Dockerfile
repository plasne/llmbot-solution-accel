# create the build container
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
LABEL stage=build
WORKDIR /app
COPY shared shared
COPY memory memory
WORKDIR /app/memory
RUN dotnet publish -c Release -o out

# create the runtime container
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/memory/out .
ENTRYPOINT ["dotnet", "memory.dll"]
