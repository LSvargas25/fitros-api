# FitRos.API container image — targets Render (Docker runtime) + Neon Postgres.
# Same topology as Widget Finanzas: one web service, DB reached only through a
# connection-string env var, TLS terminated at the platform edge.
#
# Build context = the Backend/ folder (this file's directory).
#   docker build -t fitros-api -f Dockerfile .

# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore against the csproj graph first so this layer caches until a
# .csproj actually changes.
COPY FitRos.sln ./
COPY FitRos.API/FitRos.API.csproj                 FitRos.API/
COPY FitRos.Application/FitRos.Application.csproj  FitRos.Application/
COPY FitRos.Domain/FitRos.Domain.csproj           FitRos.Domain/
COPY FitRos.Infrastructure/FitRos.Infrastructure.csproj FitRos.Infrastructure/
COPY FitRos.Tests/FitRos.Tests.csproj             FitRos.Tests/
RUN dotnet restore FitRos.API/FitRos.API.csproj

# Now the rest of the source and publish.
COPY . .
RUN dotnet publish FitRos.API/FitRos.API.csproj -c Release -o /app --no-restore

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app ./

# Render routes HTTPS -> container over plain HTTP with X-Forwarded-* headers.
# This switch makes ASP.NET Core honour X-Forwarded-Proto, so Request.Scheme is
# "https" and the app's app.UseHttpsRedirection() does NOT bounce into a loop.
ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
ENV ASPNETCORE_ENVIRONMENT=Production

# Render provides $PORT at runtime (default 10000). Bind Kestrel to it.
ENV PORT=10000
EXPOSE 10000
ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-10000} exec dotnet FitRos.API.dll"]
