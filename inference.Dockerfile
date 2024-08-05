# create the build container
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG TARGETARCH
LABEL stage=build
WORKDIR /app
COPY shared shared
COPY inference inference
COPY proto proto
WORKDIR /app/inference
RUN dotnet publish -c Release -o out -a $TARGETARCH

# create the runtime container
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/inference/out .
COPY inference/templates templates
ENTRYPOINT ["dotnet", "inference.dll"]
