using Improbable.SpatialOS.Deployment.V1Alpha1;
using Improbable.Worker;

namespace DeploymentManager
{
    public class DeploymentInformation
    {
        public string DeploymentId;
        public string ProjectName;
        public Connection Connection;
        public SpatialOSReceiveHandler Handler;


        public GetDeploymentRequest ToGetDeploymentRequest()
        {
            return new GetDeploymentRequest
            {
                Id = DeploymentId,
                ProjectName = ProjectName,
            };
        }
    }
}