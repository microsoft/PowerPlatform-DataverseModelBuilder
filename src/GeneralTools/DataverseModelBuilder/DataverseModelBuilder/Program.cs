using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Dataverse.DataverseModelBuilder;
using Microsoft.PowerPlatform.Dataverse.DataverseModelBuilder.ConnectionManagement;
using Microsoft.PowerPlatform.Dataverse.ModelBuilderLib.Status;
using Microsoft.PowerPlatform.Dataverse.ModelBuilderLib;
using Microsoft.Xrm.Sdk;
using System;
using System.Diagnostics;
using System.ServiceModel;
using System.ServiceModel.Security;

namespace DataverseModelBuilder
{
    public class Program
    {
        internal static ConsoleWriter consoleWriter = new ConsoleWriter();

        private void ExecuteProcess (string[] args , ILogger logger)
        {
            // Read arguments,
            // if connectionstring is present create connection
            // if /il is present show login dialog.
            // if /accesstoken is present create connection using URL.

            ModelBuilder runner = new ModelBuilder(logger);

            // Load arguments that where passed in.
            try
            {
                runner.Parameters.Logger.OnWriteProgressItem += _modeBuilderLogger_OnWriteProgressItem;

                runner.Parameters.LoadArguments(args);
                if (runner.Parameters.ShowHelp || !runner.Parameters.VerifyArguments())
                {
                    ShowHelp(runner);
                    return;
                }
            }
            catch (InvalidOperationException)
            {
                ShowHelp(runner);
                return;
            }

            IOrganizationService orgSvc = null;
            // Check to see if a connection is required
            if (runner.IsLiveConnectionRequired)
            {
                // create connection
                Connection connMgr = new Connection(runner.Parameters);
                orgSvc = connMgr.CreateConnection();
            }

            runner.Invoke(orgSvc);
        }

        private void _modeBuilderLogger_OnWriteProgressItem(object sender, Microsoft.PowerPlatform.Dataverse.ModelBuilderLib.Status.ProgressStatus e)
        {
            if (consoleWriter != null)
            {
                if (e.Indent)
                    e.StatusMessage = "\t" + e.StatusMessage;
                switch (e.StatusType)
                {
                    case ProgressType.Information:
                        consoleWriter.WriteLine(e.StatusMessage);
                        break;
                    case ProgressType.Warning:
                        consoleWriter.WriteWarning(e.StatusMessage);
                        break;
                    case ProgressType.Error:
                        consoleWriter.WriteError(e.StatusMessage);
                        break;
                    default:
                        break;
                }
            }
        }

        [STAThread]
        static void Main(string[] args)
        {

            // Setup logging:
            // Run test. .
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // Create TraceListner
            var FileLogTraceListenr = new System.Diagnostics.TextWriterTraceListener("DataverseModleBuilder11.log");
            SourceSwitch dvmbSwitch = new SourceSwitch("DataverseModelBuilderLib")
            {
                Level = SourceLevels.Verbose
            };
            ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
                                builder.AddTraceSource(dvmbSwitch, FileLogTraceListenr)
                                .AddConfiguration(config.GetSection("Logging"))
                                );
            var logger = loggerFactory.CreateLogger<Program>();
            try
            {
                Program prg = new Program();
                prg.ExecuteProcess(args , logger);
            }
            catch (FaultException<OrganizationServiceFault> e)
            {
                Console.Error.WriteLine();
                Console.Error.WriteLine("Exiting program with exception: {0}", e.Detail.Message);
                return;
            }
            catch (MessageSecurityException e)
            {
                Console.Error.WriteLine();
                Console.Error.WriteLine("Exiting program with exception: {0}", e.InnerException.Message);
                return;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine();
                Console.Error.WriteLine("Exiting program with exception: {0}", e.Message);
                return;
            }
        }

        private void ShowHelp(ModelBuilder runner)
        {
            runner.Parameters.ShowLogo();
            runner.Parameters.ShowUsage();
        }
    }
}
