using Improbable.SpatialOS.Deployment.V1Alpha1;

namespace DeploymentManager
{
    public struct DeploymentTemplate
    {
        public string AssemblyId;
        public string DeploymentName;
        public string ProjectName;
        public string SnapshotId;
        public string RegionCode;
        public LaunchConfig LaunchConfig;

        public Deployment ToDeployment()
        {
            return new Deployment
            {
                AssemblyId = AssemblyId,
                LaunchConfig = LaunchConfig,
                Name = DeploymentName,
                ProjectName = ProjectName,
                StartingSnapshotId = SnapshotId,
                RegionCode = RegionCode,
            };
        }
    }
}
