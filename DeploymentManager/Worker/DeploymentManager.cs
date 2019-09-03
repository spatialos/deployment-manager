using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Improbable.SpatialOS.Deployment.V1Alpha1;
using Improbable.Worker;
using Deployment = Improbable.SpatialOS.Deployment.V1Alpha1.Deployment;

namespace DeploymentManager
{
    internal class DeploymentManager
    {
        public const string WorkerType = "DeploymentManager";

        private const string ServiceAccountTokenFile = "ServiceAccountToken.txt";
        private const string launchConfigFile = "launch_config.json";
        private static TimeSpan TaskDelay = TimeSpan.FromSeconds(5);
        private static TimeSpan OpsDelay = TimeSpan.FromSeconds(1);

        private readonly WorkerConfig workerConfig;
        private readonly SessionConfig sessionConfig;
        private readonly string devAuthToken;

        private readonly Connection serviceConnection;

        public DeploymentManager(WorkerConfig workerConfig, SessionConfig sessionConfig)
        {
            this.workerConfig = workerConfig;
            this.sessionConfig = sessionConfig;

            var serviceAccountToken = Utils.GetFileContent(WorkerType, ServiceAccountTokenFile);
            DeploymentModifier.Init(serviceAccountToken);
            WorkerAuthenticator.Init(serviceAccountToken);

            // Connect to service deployment.            
            var connector = new Connector(WorkerType);
            bool hasConnected;
            short attempts = 0;
            do
            {
                hasConnected = connector.TryConnect(workerConfig.ReceptionistHost, workerConfig.ReceptionistPort, workerConfig.WorkerId);
                attempts++;
            }
            while (attempts < Utils.MaxRetries && !hasConnected);

            if (!hasConnected)
            {
                throw new Exception("Unable to connect to service deployment.");
            }

            serviceConnection = connector.Connection;
            devAuthToken = WorkerAuthenticator.GetDevelopmentAuthenticationToken(workerConfig.ProjectName, sessionConfig.TokenLifeTimeDays);
        }

        public void ObserveDeployments()
        {
            for (var i = 0; i < sessionConfig.NumberOfDeployments; i++)
            {
                var delay = TimeSpan.FromSeconds(i * sessionConfig.DeploymentIntervalSeconds);
                var deploymentName = $"{sessionConfig.DeploymentPrefix}_{i}";
                Task.Run(() =>
                {
                    Task.Delay(delay).Wait();
                    try
                    {
                        var request = DeploymentModifier.GetListDeploymentRequest(deploymentName, workerConfig.ProjectName);
                        var deployments = DeploymentModifier.ListDeployments(request);
                        var deployment = deployments.FirstOrDefault(d => d.Status != Deployment.Types.Status.Stopped);
                        if (deployment == null)
                        {
                            Log.Print(LogLevel.Info, $"Could not find deployment {deploymentName}, starting a new one.");
                            StartDeployment(deploymentName);
                        }
                        else
                        {
                            StartOrConnectToDeployment(deployment);                            
                        }
                    }
                    catch (Exception e)
                    {
                        Log.PrintException(e, serviceConnection);
                    }
                });
            }

            using (var dispatcher = new Dispatcher())
            {
                while (serviceConnection.GetConnectionStatusCode() == ConnectionStatusCode.Success)
                {
                    using (var opList = serviceConnection.GetOpList(0))
                    {
                        dispatcher.Process(opList);
                    }
                }
            }

            Log.Print(LogLevel.Debug, "Disconnected from SpatialOS");
        }

        private void StartOrConnectToDeployment(Deployment deployment)
        {
            Log.Print(LogLevel.Info, $"Deployment {deployment.Name}, Status {deployment.Status}.");

            var request = DeploymentModifier.GetGetDeploymentRequest(deployment.Id, workerConfig.ProjectName);
            while (deployment.Status == Deployment.Types.Status.Starting || deployment.Status == Deployment.Types.Status.Stopping)
            {
                // Wait before checking the deployment status again.
                Task.Delay(TaskDelay).Wait();
                deployment = DeploymentModifier.GetDeployment(request);
            }

            switch (deployment.Status)
            {
                case Deployment.Types.Status.Running:
                    ConnectToDeployment(deployment);
                    break;
                case Deployment.Types.Status.Stopped:
                    StartDeployment(deployment.Name);
                    break;
                case Deployment.Types.Status.Unknown:
                case Deployment.Types.Status.Error:
                    Log.Print(LogLevel.Warn, $"The deployment {deployment.Name} is in an unrecoverable state ({deployment.Status}). Trying to restart it.", serviceConnection);
                    DeploymentModifier.StopDeployment(deployment);
                    Task.Delay(TaskDelay).Wait();
                    StartOrConnectToDeployment(deployment);
                    break;
            }
        }

        private void StartDeployment(string deploymentName, int retries = 0)
        {
            var launchConfig = new LaunchConfig
            {
                ConfigJson = Utils.GetFileContent(WorkerType, launchConfigFile),
            };

            var snapshotId = DeploymentModifier.UploadSnapshot(workerConfig.ProjectName, deploymentName);
            var template = new DeploymentTemplate
            {
                AssemblyId = sessionConfig.AssemblyName,
                DeploymentName = deploymentName,
                LaunchConfig = launchConfig,
                ProjectName = workerConfig.ProjectName,
                SnapshotId = snapshotId,
                RegionCode = sessionConfig.RegionCode,
            };

            Log.Print(LogLevel.Info, $"Creating deployment {deploymentName}.", serviceConnection);
            var deploymentOperation = DeploymentModifier.CreateDeployment(template).PollUntilCompleted();

            if (deploymentOperation.IsCompleted)
            {
                Log.Print(LogLevel.Info, $"Successfully created deployment {deploymentName}.", serviceConnection);
                ConnectToDeployment(deploymentOperation.Result);
            }
            else if (deploymentOperation.IsFaulted)
            {
                Log.Print(LogLevel.Error, $"Failed to create deployment {deploymentName} with exception {deploymentOperation.Exception}. " +
                    $"Trying again.", serviceConnection);

                if (retries < Utils.MaxRetries)
                {
                    Task.Run(() => StartDeployment(deploymentName, retries + 1));
                }
            }
        }

        private void ConnectToDeployment(Deployment deployment)
        {
            var request = DeploymentModifier.GetGetDeploymentRequest(deployment.Id, workerConfig.ProjectName);
            DeploymentModifier.AddDeploymentTags(request, sessionConfig.DeploymentTags.Append($"{Tags.MaxPlayers}_{sessionConfig.MaxNumberOfClients}"));
            DeploymentModifier.SetMaxWorkerCapacity(request, sessionConfig.ClientType, sessionConfig.MaxNumberOfClients);

            var connector = new Connector(WorkerType, deployment.Name, serviceConnection);
            var connectionParameters = new ConnectionParameters
            {
                WorkerType = WorkerType,
                Network =
                    {
                        UseExternalIp = true,
                        ConnectionType = NetworkConnectionType.Tcp,
                        ConnectionTimeoutMillis = 10000,
                    },
            };
            
            bool hasConnected;
            short attempts = 0;
            do
            {
                hasConnected = connector.TryConnect(connectionParameters, devAuthToken);
                attempts++;
            }
            while (attempts < Utils.MaxRetries && !hasConnected);

            if (!hasConnected)
            {
                throw new Exception($"Unable to connect to deployment {deployment.Name}.");
            }

            var connection = connector.Connection;
            var handler = new SpatialOSReceiveHandler(connection, request);
            var deploymentInformation = new DeploymentInformation
            {
                DeploymentId = deployment.Id,
                ProjectName = deployment.ProjectName,
                Connection = connection,
                Handler =  handler,
            };

            new Thread(ProcessingOps).Start(deploymentInformation);
        }

        private void ProcessingOps(Object deploymentInfoObject)
        {
            var deploymentInfo = (DeploymentInformation) deploymentInfoObject;
            while (deploymentInfo.Connection.GetConnectionStatusCode() == ConnectionStatusCode.Success)
            {
                UpdatePlayerCount(deploymentInfo.ToGetDeploymentRequest());
                deploymentInfo.Handler.ProcessOps();
                Task.Delay(OpsDelay).Wait();
            }

            Task.Run(() =>
            {
                try
                {
                    var deployment = DeploymentModifier.GetDeployment(deploymentInfo.ToGetDeploymentRequest());
                    StartOrConnectToDeployment(deployment);
                }
                catch (Exception e)
                {
                    Log.Print(LogLevel.Error, $"{e.Message}\n{e.StackTrace}");
                }
            });
        }

        private void UpdatePlayerCount(GetDeploymentRequest getDeploymentRequest)
        {
            var playerCount = DeploymentModifier.GetCurrentWorkerCapacity(getDeploymentRequest, sessionConfig.ClientType);
            DeploymentModifier.UpdateDeploymentTag(getDeploymentRequest, Tags.Players, playerCount.ToString());
        }
    }
}
