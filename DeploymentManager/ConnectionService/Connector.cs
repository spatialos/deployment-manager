using Improbable.Worker;
using Improbable.Worker.Alpha;
using System.Collections.Generic;

namespace DeploymentManager
{
    public class Connector
    {
        /// <summary>
        ///     The host to connect to the locator.
        /// </summary>
        public const string LocatorHost = "locator.improbable.io";

        /// <summary>
        ///     The port to connect to the locator via the development authentication flow.
        /// </summary>
        public const ushort LocatorPort = 444;

        /// <summary>
        ///     The connection to the deployment as specified in the constructor.
        /// </summary>
        public Connection Connection { get; private set; }

        /// <summary>
        ///     The type of worker we want to connect.
        /// </summary>
        private readonly string workerType;

        /// <summary>
        ///     The connection to log any errors or warning to. This is the connection to the deployment that runs the worker as a managed worker.
        /// </summary>
        private readonly Connection serviceConnection;

        /// <summary>
        ///     The name of the deployment the worker wants to connect to as a client.
        /// </summary>
        private readonly string deploymentName;

        public Connector(string workerType)
        {
            this.workerType = workerType;
        }

        public Connector(string workerType, string deploymentName, Connection serviceConnection = null)
        {
            this.serviceConnection = serviceConnection;
            this.workerType = workerType;
            this.deploymentName = deploymentName;
        }

        private string GetPlayerIdentityToken(string developmentAuthToken, int retries = 0)
        {
            var playerIdentityTokenResponse = DevelopmentAuthentication.CreateDevelopmentPlayerIdentityTokenAsync(
                LocatorHost,
                LocatorPort,
                new PlayerIdentityTokenRequest
                {
                    DevelopmentAuthenticationTokenId = developmentAuthToken,
                    DisplayName = workerType,
                    PlayerId =  workerType,
                    UseInsecureConnection = false,
                }).Get();

            if (playerIdentityTokenResponse.Status == ConnectionStatusCode.Success)
            {
                return playerIdentityTokenResponse.PlayerIdentityToken;
            }
            
            if (retries < Utils.MaxRetries &&
                (playerIdentityTokenResponse.Status == ConnectionStatusCode.Timeout || playerIdentityTokenResponse.Status == ConnectionStatusCode.NetworkError))
            {
                return GetPlayerIdentityToken(developmentAuthToken, retries + 1);
            }

            throw new System.Exception($"Failed to retrieve player identity token: {playerIdentityTokenResponse.Status}\n{playerIdentityTokenResponse.Error}");
        }

        private List<LoginTokenDetails> GetLoginTokenDetails(string playerIdentityToken, int retries = 0)
        {
            var loginTokenDetailsResponse = DevelopmentAuthentication.CreateDevelopmentLoginTokensAsync(
                LocatorHost,
                LocatorPort,
                new LoginTokensRequest
                {
                    PlayerIdentityToken = playerIdentityToken,
                    UseInsecureConnection = false,
                    WorkerType = workerType,
                }).Get();

            if (loginTokenDetailsResponse.Status == ConnectionStatusCode.Success)
            {
                return loginTokenDetailsResponse.LoginTokens;
            }
            else if (retries < Utils.MaxRetries &&
                (loginTokenDetailsResponse.Status == ConnectionStatusCode.Timeout || loginTokenDetailsResponse.Status == ConnectionStatusCode.NetworkError))
            {
                return GetLoginTokenDetails(playerIdentityToken, retries + 1);
            }

            throw new System.Exception($"Failed to retrieve player identity token: {loginTokenDetailsResponse.Status}\n{loginTokenDetailsResponse.Error}");
        }

        private string GetLoginToken(List<LoginTokenDetails> loginTokenDetails)
        {
            var loginToken = string.Empty;
            foreach (var detail in loginTokenDetails)
            {
                if (detail.DeploymentName == deploymentName)
                {
                    loginToken = detail.LoginToken;
                }
            }

            if (string.IsNullOrEmpty(loginToken))
            {
                throw new System.Exception($"Failed to find deployment {deploymentName}. Was it tagged with 'dev_login'?");
            }

            return loginToken;
        }

        public bool TryConnect(ConnectionParameters connectionParameters, string developmentAuthToken)
        {
            var playerIdentityToken = GetPlayerIdentityToken(developmentAuthToken);
            var loginTokenDetails = GetLoginTokenDetails(playerIdentityToken);
            var loginToken = GetLoginToken(loginTokenDetails);

            var locatorParameters = new Improbable.Worker.Alpha.LocatorParameters
            {
                PlayerIdentity = new PlayerIdentityCredentials
                {
                    LoginToken = loginToken,
                    PlayerIdentityToken = playerIdentityToken,
                }
            };

            using (var locator = new Improbable.Worker.Alpha.Locator(LocatorHost, LocatorPort, locatorParameters))
            using (var connectionFuture = locator.ConnectAsync(connectionParameters))
            {
                Connection = connectionFuture.Get();

                if (Connection.GetConnectionStatusCode() != ConnectionStatusCode.Success)
                {
                    Log.Print(LogLevel.Error, $"Failed to connect to deployment {deploymentName}. Reason: {Connection.GetConnectionStatusDetailString()}", serviceConnection);
                    return false;
                }

                return true;
            }
        }

        public bool TryConnect(string host, ushort port, string workerName)
        {
            var connectionParameters = new ConnectionParameters
            {
                WorkerType = workerType,
            };

            using (var connectionFuture = Connection.ConnectAsync(host, port, workerName, connectionParameters))
            {
                Connection = connectionFuture.Get();

                if (Connection.GetConnectionStatusCode() != ConnectionStatusCode.Success)
                {
                    Log.Print(LogLevel.Error, $"Failed to connect to deployment {deploymentName}. Reason: {Connection.GetConnectionStatusDetailString()}", serviceConnection);
                    return false;
                }

                return true;
            }
        }
    }
}
