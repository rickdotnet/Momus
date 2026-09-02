FROM mcr.microsoft.com/dotnet/sdk:10.0 AS dotnet-build

COPY ./src /src
RUN dotnet publish "/src/Momus.Host/Momus.Host.csproj" -c Release -r linux-x64 -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
COPY --from=dotnet-build /app .
ENTRYPOINT ["dotnet", "Momus.Host.dll"]
