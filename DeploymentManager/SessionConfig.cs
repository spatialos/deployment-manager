namespace DeploymentManager
{
    public class SessionConfig
    {
        public int TokenLifeTimeDays;
        public string ClientType;
        public int MaxNumberOfClients;
        public string DeploymentPrefix;
        public int NumberOfDeployments;
        public string AssemblyName;
        public string[] DeploymentTags;
        public string RegionCode;
        public int DeploymentIntervalSeconds;
    }
}
