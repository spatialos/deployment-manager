Param
(
	[Parameter(Mandatory=$True,Position=1)]
    [string]$launch_config_path,
    [Parameter(Mandatory=$True,Position=2)]
    [string]$snapshot_path
)

Copy-Item ${launch_config_path} -Destination "$(pwd)/DeploymentManager/launch_config.json"
Copy-Item ${snapshot_path} -Destination "$(pwd)/DeploymentManager/default.snapshot"

dotnet build -c Release -p:Platform=x64 GeneratedCode/GeneratedCode.csproj
dotnet publish DeploymentManager/DeploymentManager.csproj -r win10-x64 -c Release -p:Platform=x64 --self-contained
