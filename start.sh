#!/bin/bash
gulp clean min
dotnet restore
ASPNETCORE_ENVIRONMENT=Production ASPNETCORE_URLS=http://0.0.0.0:8050 dotnet run
