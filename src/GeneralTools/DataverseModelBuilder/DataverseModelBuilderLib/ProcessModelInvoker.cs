using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Dataverse.ModelBuilderLib;
using Microsoft.PowerPlatform.Dataverse.ModelBuilderLib.Utility;
using Microsoft.Xrm.Sdk;
using System;
using System.Diagnostics;
using System.Globalization;
using System.ServiceModel;
using System.ServiceModel.Security;

namespace Microsoft.PowerPlatform.Dataverse.ModelBuilderLib
{
    /// <summary>
    /// Dataverse Model Builder Execution entry point.
    /// </summary>
    public class ModelBuilder
    {
        #region Vars
        /// <summary>
        /// Service Provider to contain all services needed.
        /// </summary>
        private ServiceProvider _serviceProvider;

        /// <summary>
        /// Organization Metadata object.
        /// </summary>
        private IOrganizationMetadata organizationMetadata = null;

        /// <summary>
        /// Passed in IOrganizationService Interface.
        /// </summary>
        internal IOrganizationService dataverseService = null;

        /// <summary>
        /// Processed Parameter collection for processing.
        /// </summary>
        private ModelBuilderInvokeParameters _parameters;

        #endregion

        #region Properties

        /// <summary>
        /// Trace log hander
        /// </summary>
        public ModeBuilderLoggerService ModelBuilderLogger { get; private set; }

        /// <summary>
        /// Parameter Parser module.
        /// </summary>
        public ModelBuilderInvokeParameters Parameters
        {
            get
            {
                return _parameters;
            }
        }


        /// <summary>
        /// Service Provider Access Property
        /// </summary>
        private ServiceProvider ServiceProvider
        {
            get
            {
                if (_serviceProvider == null)
                {
                    // Init on first use.
                    _serviceProvider = new ServiceProvider();
                    _serviceProvider.InitializeServices(_parameters);
                }
                return _serviceProvider;
            }
        }
        #endregion



        /// <summary>
        /// ModelBuilder Processor class. 
        /// </summary>
        public ModelBuilder(ILogger logger = null)
        {
            ModelBuilderLogger = new ModeBuilderLoggerService(string.Empty, logger);
            _parameters = new ModelBuilderInvokeParameters(ModelBuilderLogger);
        }

        //private void InitializeProcessModelInvoker(string[] args, ILogger logger)
        //{
        //    ModelBuilderLogger = new ModeBuilderLoggerService("", logger); 
        //    try
        //    {
        //        Parameters.LoadArguments(args);
        //        if (Parameters.ShowHelp || !Parameters.VerifyArguments())
        //        {
        //            ShowHelp();
        //            _parameters._modeBuilderLogger.TraceWarning("Exiting {0} with exit code 1");
        //            return;
        //        }
        //    }
        //    catch (InvalidOperationException)
        //    {
        //        ShowHelp();
        //        _parameters._modeBuilderLogger.TraceWarning("Exiting {0} with exit code 1");
        //        return;
        //    }
        //}


        /// <summary>
        /// If the current metadata service provider requires a live connection,  will return true and a valid organization service connection will be required for processing. if false, Null can be passed for the organization service when invoking the code generation.
        /// </summary>
        public bool IsLiveConnectionRequired => ServiceProvider.MetadataProviderService.IsLiveConnectionRequired;

        /// <summary>
        /// Invokes Runtime, processing arguments other then IOrganizationService
        /// </summary>
        /// <param name="serviceClient"></param>
        /// /// <returns></returns>
        public int Invoke(IOrganizationService serviceClient)
        {
            // Do a check for Parameter validation here. 
            if (!Parameters.VerifyArguments() )
            {
                // do error here. 
            }

            dataverseService = serviceClient;
            try
            {
                Run();
                return 0;
            }
            catch (FaultException<OrganizationServiceFault> e)
            {
                Console.Error.WriteLine();
                Console.Error.WriteLine("Exiting program with exception: {0}", e.Detail.Message);
                if (ModelBuilderLogger.CurrentTraceLevel == SourceLevels.Off)
                    Console.Error.WriteLine("Enable tracing and view the trace files for more information.");
                ModelBuilderLogger.TraceError("Exiting program with exit code 2 due to exception : {0}", e.Detail);
                ModelBuilderLogger.TraceError("===== DETAIL ======");
                ModelBuilderLogger.TraceError(e);
                return 2;
            }
            catch (MessageSecurityException e)
            {
                Console.Error.WriteLine();
                Console.Error.WriteLine("Exiting program with exception: {0}", e.InnerException.Message);
                if (ModelBuilderLogger.CurrentTraceLevel == SourceLevels.Off)
                    Console.Error.WriteLine("Enable tracing and view the trace files for more information.");
                ModelBuilderLogger.TraceError("Exiting program with exit code 2 due to exception : {0}", e.InnerException);
                ModelBuilderLogger.TraceError("===== DETAIL ======");
                ModelBuilderLogger.TraceError(e);
                return 2;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine();
                Console.Error.WriteLine("Exiting program with exception: {0}", e.Message);
                if (ModelBuilderLogger.CurrentTraceLevel == SourceLevels.Off)
                    Console.Error.WriteLine("Enable tracing and view the trace files for more information.");
                ModelBuilderLogger.TraceError("Exiting program with exit code 2 due to exception : {0}", e);
                ModelBuilderLogger.TraceError("===== DETAIL ======");
                ModelBuilderLogger.TraceError(e);
                return 2;
            }
        }

        private int Run()
        {
            ModelBuilderLogger.TraceMethodStart();;
            Stopwatch procSw = Stopwatch.StartNew();
            if (!Parameters.NoLogo)
                Parameters.ShowLogo();

            if (Parameters.LegacyMode)
            {
                ModelBuilderLogger.WriteConsole("LegacyMode enabled", false, Status.ProcessStage.ParseParamaters);
            }

            Stopwatch operationSW = Stopwatch.StartNew();
            ModelBuilderLogger.WriteConsole("Begin reading metadata from MetadataProviderService", false, Status.ProcessStage.ReadMetadata);
            ServiceProvider.MetadataProviderService.ServiceConnection = dataverseService;
            organizationMetadata = ServiceProvider.MetadataProviderService.LoadMetadata(ServiceProvider);
            operationSW.Stop();
            ModelBuilderLogger.WriteConsole($"Completed reading metadata from MetadataProviderService - {operationSW.Elapsed.ToDurationString()}", false, Status.ProcessStage.ReadMetadata);

            if (organizationMetadata == null)
            {
                ModelBuilderLogger.TraceError("{0} returned null metadata", typeof(IMetadataProviderService).Name);
                return 1;
            }

            operationSW.Restart();
            ModelBuilderLogger.WriteConsole("Begin Writing Code Files", false, Status.ProcessStage.ClassGeneration);

            this.WriteCode(organizationMetadata);

            operationSW.Stop();
            ModelBuilderLogger.WriteConsole($"Completed Writing Code Files - {operationSW.Elapsed.ToDurationString()}", false, Status.ProcessStage.ClassGeneration);
            ModelBuilderLogger.WriteConsole($"Generation Complete - {procSw.Elapsed.ToDurationString()}", false, Status.ProcessStage.ClassGeneration);
            procSw.Stop();
            operationSW.Stop();
            return 0;
        }

        private void WriteCode(IOrganizationMetadata organizationMetadata)
        {
            ModelBuilderLogger.TraceMethodStart();;
            ICodeGenerationService writer = this.ServiceProvider.CodeGenerationService;
            writer.Write(organizationMetadata, this.Parameters.Language, this.Parameters.OutputFile, this.Parameters.Namespace, this.ServiceProvider);
            ModelBuilderLogger.TraceMethodStop();
        }

        private void ShowHelp()
        {
            ModelBuilderLogger.TraceMethodStart();;
            Parameters.ShowLogo();
            Parameters.ShowUsage();
            ModelBuilderLogger.TraceMethodStop();
        }

    }
}
