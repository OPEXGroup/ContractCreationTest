#!/usr/bin/env bash

build_directory="build"
project="ContractCreationTest"

set -e

if [ ! -f "ContractCreationTest.sln" ]; then
    echo "Script must be started from solution root directory"
    exit 1
fi

 dotnet_path=$(which dotnet)
 if [ ! -x "$dotnet_path" ] ; then
    echo "dotnet SDK >= 2.1.300 is required"
 fi

if [ ! -d "$build_directory" ]; then
    echo "Creating build directory $build_directory"
    mkdir $build_directory
fi

dotnet publish "src/$project/${project}.csproj" --configuration Release --force --output "../../${build_directory}"

echo "Project built"

solc_path=$(which solc)
if [ ! -x "$solc_path" ]
    echo "Using solidity compiler from repo (Win32 only)"
    dotnet "$build_directory/$project.dll"
else
    echo "Using solidity compiler from $solc_path"
    dotnet "$build_directory/$project.dll" "$solc_path"
fi
