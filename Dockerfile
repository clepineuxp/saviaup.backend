FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY saviaup.backend.sln ./
COPY saviaup.backend.Shared/*.csproj saviaup.backend.Shared/
COPY saviaup.backend.Domain/*.csproj saviaup.backend.Domain/
COPY saviaup.backend.Core/*.csproj saviaup.backend.Core/
COPY saviaup.backend.Infrastructure/*.csproj saviaup.backend.Infrastructure/
COPY saviaup.backend.Api/*.csproj saviaup.backend.Api/
COPY tests/saviaup.backend.Core.Tests/*.csproj tests/saviaup.backend.Core.Tests/
COPY tests/saviaup.backend.IntegrationTests/*.csproj tests/saviaup.backend.IntegrationTests/
RUN dotnet restore saviaup.backend.sln
COPY . .
RUN dotnet publish saviaup.backend.Api/saviaup.backend.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app .
ENTRYPOINT ["dotnet", "saviaup.backend.Api.dll"]
