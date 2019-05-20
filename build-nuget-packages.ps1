Remove-Item -Recurse -Force -ErrorAction SilentlyContinue "$HOME\.nuget\packages\improbable.coresdk"
Remove-Item -Recurse -Force -ErrorAction SilentlyContinue "$HOME\.nuget\packages\improbable.coresdk.tools"
Remove-Item -Recurse -Force -ErrorAction SilentlyContinue nugetpackages

New-Item -ItemType directory -Path nugetpackages

dotnet pack Improbable.CoreSdk.Tools/Improbable.CoreSdk.Tools.csproj -o "$(pwd)/nugetpackages"
dotnet pack Improbable.CoreSdk/Improbable.CoreSdk.csproj -o "$(pwd)/nugetpackages"

dotnet restore
