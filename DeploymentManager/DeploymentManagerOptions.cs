using System.Collections.Generic;
using System.IO;
using Mono.Options;

namespace DeploymentManager
{
    /// <summary>
    ///     Runtime options for the CodeGenerator.
    /// </summary>
    public class WorkerConfig
    {
        public string ReceptionistHost { get; private set; }
        public ushort ReceptionistPort { get; private set; }
        public string WorkerId { get; private set; }
        public string LogFile { get; private set; }
        public string ProjectName { get; private set; }
        public string AssemblyName { get; private set; }
        public bool ShouldShowHelp { get; private set; }
        public string HelpText { get; private set; }

        public static WorkerConfig ParseArguments(ICollection<string> args)
        {
            var options = new WorkerConfig();
            var optionSet = new OptionSet
            {
                {
                    "receptionist-host=", "REQUIRED: The host of the receptionist that the deployment manager connects to",
                    r => options.ReceptionistHost = r
                },
                {
                    "receptionist-port=", "REQUIRED: The port of the receptionist that the deployment manager connects to",
                    r => options.ReceptionistPort = ushort.Parse(r)
                },
                {
                    "worker-id=", "REQUIRED: The id of the deployment manager",
                    w => options.WorkerId = w
                },
                {
                    "log-file=", "REQUIRED: Path to the log file",
                    l => options.LogFile = l
                },
                {
                    "project-name=", "REQUIRED: The name of the SpatialOS project",
                    p => options.ProjectName = p
                },
                {
                    "assembly-name=", "REQUIRED: The name of the assembly",
                    a => options.AssemblyName = a
                },
                {
                    "h|help", "show help",
                    h => options.ShouldShowHelp = h != null
                }
            };

            optionSet.Parse(args);

            using (var sw = new StringWriter())
            {
                optionSet.WriteOptionDescriptions(sw);
                options.HelpText = sw.ToString();
            }

            return options;
        }
    }
}
