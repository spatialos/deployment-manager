Param
(
	[Parameter(Mandatory=$True,Position=1)]
    [string]$project_name,
    [Parameter(Mandatory=$True,Position=2)]
    [int]$token_lifetime
)


dotnet build -c Release -p:Platform=x64 ServiceAccountGenerator/ServiceAccountGenerator.csproj
dotnet run --project ServiceAccountGenerator/ "$(pwd)/DeploymentManager" ${project_name} ${token_lifetime}
