# create the build container
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG TARGETARCH
LABEL stage=build
WORKDIR /app
COPY shared shared
COPY memory memory
WORKDIR /app/memory
RUN dotnet publish -c Release -o out -a $TARGETARCH

# create the runtime container
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/memory/out .
ENTRYPOINT ["dotnet", "memory.dll"]
