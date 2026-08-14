# ---------------------------------------------------------------- build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first so `restore` is cached independently of source changes.
COPY Ams.sln ./
COPY src/Ams.Domain/Ams.Domain.csproj src/Ams.Domain/
COPY src/Ams.Application/Ams.Application.csproj src/Ams.Application/
COPY src/Ams.Infrastructure/Ams.Infrastructure.csproj src/Ams.Infrastructure/
COPY src/Ams.Api/Ams.Api.csproj src/Ams.Api/
COPY tests/Ams.UnitTests/Ams.UnitTests.csproj tests/Ams.UnitTests/
RUN dotnet restore Ams.sln

COPY . .
RUN dotnet publish src/Ams.Api/Ams.Api.csproj -c Release -o /app/publish --no-restore

# --------------------------------------------------------------- runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Attachment storage. This has to be created *while still root* and handed to the
# app user: Docker seeds a fresh named volume from the image's directory, mount
# point ownership included. Without it the volume arrives owned by root, the
# non-root app cannot write into it, and the first upload — which is the seeder's
# own worksheet — fails with "Permission denied".
RUN mkdir -p /app/uploads && chown -R $APP_UID:$APP_UID /app/uploads

# Run as the non-root user the base image already provides.
USER $APP_UID

COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "Ams.Api.dll"]
