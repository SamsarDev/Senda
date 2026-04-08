#!/bin/bash

# Senda Dotnet Helper Tools

function build() {
    dotnet build Senda.slnx
}

function run_api() {
    dotnet run --project src/Senda.Api
}

function add_migration() {
    if [ -z "$1" ]; then
        echo "Usage: add_migration <MigrationName>"
        return 1
    fi
    dotnet ef migrations add "$1" --project src/Senda.Infrastructure --startup-project src/Senda.Api
}

function update_db() {
    dotnet ef database update --project src/Senda.Infrastructure --startup-project src/Senda.Api
}

case "$1" in
    build) build ;;
    run) run_api ;;
    migrate) add_migration "$2" ;;
    update-db) update_db ;;
    *) echo "Unknown command: $1" ;;
esac
