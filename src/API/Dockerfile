FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Superchat.sln ./
COPY src/Core/Domain/Domain.csproj src/Core/Domain/
COPY src/Core/Application/Application.csproj src/Core/Application/
COPY src/Infrastructure/Infrastructure.csproj src/Infrastructure/
COPY src/API/API.csproj src/API/
RUN dotnet restore src/API/API.csproj

COPY . .
RUN dotnet publish src/API/API.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .
EXPOSE 5000
ENTRYPOINT ["dotnet", "API.dll"]
