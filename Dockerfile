FROM mcr.microsoft.com/dotnet/sdk:10.0 as dotnet-build
COPY /src .
RUN dotnet publish Momus.Host/Momus.Host.csproj -c Release -r linux-x64 -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
COPY --from=dotnet-build /app .

ENTRYPOINT ["dotnet", "Momus.Host.dll"]