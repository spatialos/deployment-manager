using Google.Protobuf.WellKnownTypes;
using Improbable.SpatialOS.ServiceAccount.V1Alpha1;
using System;
using System.IO;

namespace ServiceAccountGenerator
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.Out.WriteLine("Usage: <service account token path> <SpatialOS project name> <service account token lifetime in days>");
                Environment.Exit(1);
            }

            var serviceAccountTokenPath = args[0].Trim('"');
            var projectName = args[1];
            var tokenLifeTimeDays = int.Parse(args[2]);

            var perm = new Permission
            {
                Parts = { "prj", projectName, "*" },
                Verbs =
                {
                    Permission.Types.Verb.Read,
                    Permission.Types.Verb.Write
                }
            };

            var perm2 = new Permission
            {
                Parts = { "srv", "bundles" },
                Verbs = { Permission.Types.Verb.Read }
            };

            var resp = ServiceAccountServiceClient.Create().CreateServiceAccount(new CreateServiceAccountRequest
            {
                Name = "dmService",
                ProjectName = projectName,
                Permissions = { perm, perm2 },
                Lifetime = Duration.FromTimeSpan(new TimeSpan(tokenLifeTimeDays, 0, 0, 0))
            });

            using (var writer = new StreamWriter(Path.GetFullPath(Path.Combine(serviceAccountTokenPath, "ServiceAccountToken.txt"))))
            {
                writer.WriteLine(resp.Token);
            }
        }
    }
}
