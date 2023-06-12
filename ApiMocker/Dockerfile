
FROM mcr.microsoft.com/dotnet/sdk:7.0-alpine AS build
WORKDIR /src
ARG VERSION
COPY ["ApiMocker/ApiMocker.csproj", "ApiMocker/"]
RUN dotnet restore "ApiMocker/ApiMocker.csproj"

COPY . .
WORKDIR /src/ApiMocker
RUN dotnet build "ApiMocker.csproj" -c Release /p:Version="$VERSION" -o /app/build \
    && dotnet publish "ApiMocker.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:7.0-alpine AS final
ARG PORT=8080
ARG VERSION
LABEL name="ApiMocker" \
      version=$VERSION
EXPOSE $PORT
WORKDIR /service
RUN addgroup --gid 2000 -S servicegroup \
    && adduser --uid 1000 -G servicegroup --system --disabled-password --home /service --shell /bin/bash --no-create-home service \
    && chown -R service:servicegroup /service
USER service:servicegroup

COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://*:$PORT
ENTRYPOINT ["dotnet", "ApiMocker.dll"]
