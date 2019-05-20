Param
(
	[Parameter(Mandatory=$True,Position=1)]
    [string]$assembly_name,
    [Parameter(Mandatory=$True,Position=2)]
    [string]$deployment_name
)

spatial alpha cloud upload --assembly_name=${assembly_name} --main_config=config/spatialos.json --force
spatial alpha cloud launch --assembly_name=${assembly_name} --deployment_name=${deployment_name} --main_config=config/spatialos.json --launch_config=config/deployment.json --snapshot=config/empty.snapshot
