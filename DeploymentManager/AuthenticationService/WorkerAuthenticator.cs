using Google.Protobuf.WellKnownTypes;
using Improbable.SpatialOS.Platform.Common;
using Improbable.SpatialOS.PlayerAuth.V2Alpha1;
using System;

namespace DeploymentManager
{
    public static class WorkerAuthenticator
    {
        private static PlayerAuthServiceClient PlayerAuthServiceClient;

        public static void Init(string token)
        {
            var credentials = new PlatformRefreshTokenCredential(token);
            PlayerAuthServiceClient = PlayerAuthServiceClient.Create(credentials: credentials);
        }

        public static string GetDevelopmentAuthenticationToken(string projectName, int tokenLifeTimeDays)
        {
            return ExceptionHandler.HandleGrpcCall(() =>
            {
                var request = new CreateDevelopmentAuthenticationTokenRequest
                {
                    Description = "DAT for deployment manager",
                    Lifetime = Duration.FromTimeSpan(new TimeSpan(tokenLifeTimeDays, 0, 0, 0)),
                    ProjectName = projectName,
                };

                return PlayerAuthServiceClient.CreateDevelopmentAuthenticationToken(request).DevelopmentAuthenticationToken.Id;
            });
        }
    }
}
