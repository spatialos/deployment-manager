using System;
using System.IO;
using System.Reflection;
using Improbable.Worker;
using Newtonsoft.Json;

namespace DeploymentManager
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            try
            {
                var json = Utils.GetFileContent(DeploymentManager.WorkerType, "config.json");
                var sessionConfig = JsonConvert.DeserializeObject<SessionConfig>(json);
                var workerConfig = WorkerConfig.ParseArguments(args);

                // Prepare Logger
                var logFileName = workerConfig.LogFile;
                if (string.IsNullOrEmpty(logFileName))
                {
                    logFileName = Path.Combine(Environment.CurrentDirectory, $"{workerConfig.WorkerId}.log");
                }

                Log.Init(logFileName);
                Log.Print(LogLevel.Debug, $"Opened logfile {logFileName}");

                AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
                {
                    Log.Print(LogLevel.Fatal, eventArgs.ExceptionObject.ToString());

                    if (eventArgs.IsTerminating)
                    {
                        Log.Shutdown();
                    }
                };

                // Load GeneratedCode assembly.
                var generatedCode = Assembly.Load("GeneratedCode");
                Log.Print(LogLevel.Debug, $"Loaded generated code from {generatedCode.Location}");
                
                // Start deployment manager.
                var deploymentManager = new DeploymentManager(workerConfig, sessionConfig);
                deploymentManager.ObserveDeployments();
            }
            catch (Exception e)
            {
                Log.Print(LogLevel.Debug, $"Failed to run deployment manager with exception: {e.Message}\n{e.StackTrace}");
            }
            finally
            {
                Log.Shutdown();
            }
        }

    }
}
