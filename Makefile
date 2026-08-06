.PHONY: restore build test clean

restore:
	dotnet restore

build: restore
	dotnet build

test: build
	dotnet test

clean:
	dotnet clean
