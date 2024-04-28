FROM mcr.microsoft.com/dotnet/sdk:8.0 as dotnet-build
COPY /src .
RUN dotnet publish Momus/Momus.csproj -c Release -r linux-x64 --self-contained -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
COPY --from=dotnet-build /app .

ENTRYPOINT ["dotnet", "Momus.dll"]