FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["global.json", "./"]
COPY ["blzr.csproj", "./"]
RUN dotnet restore "blzr.csproj"

COPY . .
RUN dotnet publish "blzr.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV RoomStore__Path=/data/rooms.json

COPY --from=build /app/publish .

VOLUME ["/data"]
EXPOSE 8080

ENTRYPOINT ["dotnet", "blzr.dll"]
