FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/TgBooking/TgBooking.csproj src/TgBooking/
RUN dotnet restore src/TgBooking/TgBooking.csproj
COPY src/TgBooking/ src/TgBooking/
RUN dotnet publish src/TgBooking/TgBooking.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TgBooking.dll"]
