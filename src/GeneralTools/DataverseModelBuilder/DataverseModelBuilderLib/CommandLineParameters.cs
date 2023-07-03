using System;
using System.Collections.Generic;
using System.Text;
using System.Security;
using System.Diagnostics;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Microsoft.PowerPlatform.Dataverse.ModelBuilderLib
{
    /// <summary>
    /// Command Line Properties for the ModelBuilderInvokeParameters Instance.
    /// </summary>
    public sealed class ModelBuilderInvokeParameters : ICommandLineArgumentSource, IModelBuilderParameters
    {
        #region Fields
        private CommandLineParser _parser;
        private string _sdkUrl;
        private string _language;
        private string _outputFile;
        private string _namespace;
        private string _serviceContextName;
        private string _messageNamespace;

        private bool _noLogo;
        private bool _noGenVersionAttribute;
        private bool _showHelp;

        private string _codeCustomizationService;
        private string _codeGenerationService;
        private string _codeWriterFilterService;
        private string _codeWriterMessageFilterService;
        private string _metadataProviderService;
        private string _metadataQueryProvider;

        // TODO: Make these SecureString?
        private string _userName;
        private string _password;
        private string _domain;

        private Dictionary<string, string> _unknownParameters;


        private Dictionary<string, string> _parametersAsDictionary = null;

        /// <summary>
        /// Logging interface for Dataverse Model Builder
        /// </summary>
        public ModeBuilderLoggerService Logger { get; private set; } = null; 
        #endregion

        #region Constructors
        public ModelBuilderInvokeParameters(ModeBuilderLoggerService modeBuilderLogger)
        {
            Logger = modeBuilderLogger;
            Logger.TraceMethodStart();
            this._parser = new CommandLineParser(this,Logger);
            this._unknownParameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            this.Language = "CS";
            Logger.TraceMethodStop(); 
        }
        #endregion

        #region Properties

        /// <summary>
        /// Suppresses the GeneratedCodeAttribute on all classes.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Called via reflection")]
        [CommandLineArgument(ArgumentType.Optional | ArgumentType.Binary, "suppressGeneratedCodeAttribute", Shortcut = "sgca",
            Description = "Suppresses the GeneratedCodeAttribute on all classes.")]
        public bool SuppressGenVersionAttribute
        {
            get
            {
                return this._noGenVersionAttribute;
            }
            set
            {
                this._noGenVersionAttribute = value;
            }
        }
        /// <summary>
        /// Suppresses the banner.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Called via reflection")]
        [CommandLineArgument(ArgumentType.Optional | ArgumentType.Binary, "nologo",
            Description = "Suppresses the banner.")]
        public bool NoLogo
        {
            get
            {
                return this._noLogo;
            }
            set
            {
                this._noLogo = value;
            }
        }
        /// <summary>
        /// The language to use for the generated proxy code.  This can be either 'CS' or 'VB'.  The default language is 'CS'.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Called via reflection")]
        [CommandLineArgument(ArgumentType.Optional, "language", Shortcut = "l",
            Description = "The language to use for the generated proxy code.  This can be either 'CS' or 'VB'.  The default language is 'CS'.",
            ParameterDescription = "<language>")]
        public string Language
        {
            get
            {
                return this._language;
            }
            set
            {
                if (!System.CodeDom.Compiler.CodeDomProvider.IsDefinedLanguage(value))
                    throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture,
                        "Language {0} is not a support CodeDom Language.", value));
                this._language = value;
            }
        }

        /// <summary>
        /// A url or path to the SDK endpoint to contact for metadata
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Called via reflection")]
        [CommandLineArgument(ArgumentType.Required, "url",
            Description = "A url or path to the SDK endpoint to contact for metadata.",
            ParameterDescription = "<url>", SampleUsageValue = "http://localhost/Organization1/XRMServices/2011/Organization.svc")]
        public string Url
        {
            get
            {
                return this._sdkUrl;
            }
            set
            {
                this._sdkUrl = value;
            }
        }
        /// <summary>
        /// The filename for the generated proxy code
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Called via reflection")]
        [CommandLineArgument(ArgumentType.Required, "out",
            Description = "The filename for the generated proxy code.",
            ParameterDescription = "<filename>",
            Shortcut = "o", SampleUsageValue = "GeneratedCode.cs")]
        public string OutputFile
        {
            get
            {
                return this._outputFile;
            }
            set
            {
                this._outputFile = value;
            }
        }
        /// <summary>
        /// The namespace for the generated proxy code.  The default namespace is the global namespace.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Called via reflection")]
        [CommandLineArgument(ArgumentType.Optional, "namespace",
            Description = "The namespace for the generated proxy code.  The default namespace is the global namespace.",
            ParameterDescription = "<namespace>",
            Shortcut = "n")]
        public string Namespace
        {
            get
            {
                return this._namespace;
            }
            set
            {
                this._namespace = value;
            }
        }

        /// <summary>
        /// Used to raise the interactive dialog to login.
        /// </summary>
        [CommandLineArgument(ArgumentType.Binary | ArgumentType.Optional, "interactivelogin", Shortcut = "il", Description = "Presents a login dialog to log into the service with, if passed all other connect info is ignored.")]
        public bool UseInteractiveLogin { get; set; }


        /// <summary>
        /// Used to create a connection utilizing a passed in connection string.
        /// </summary>
        [CommandLineArgument(ArgumentType.Optional, "connectionstring", Shortcut = "connstr", Description = "Connection String to use when connecting to Dataverse. If provided, all other connect info is ignored.")]
        public string ConnectionString { get; set; }

        /// <summary>
        /// Used to login via OAuth to Dataverse, Hidden for initial ship... but here to allow for complex auth situations.
        /// </summary>
        [CommandLineArgument(ArgumentType.Optional | ArgumentType.Hidden, "accesstokentouse", Shortcut = "at", Description = "when set, use this accesstoken to talk to Dataverse")]
        public string AccessToken { get; set; }


        /// <summary>
        /// Username to use when connecting to the server for authentication.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Called via reflection")]
        [CommandLineArgument(ArgumentType.Optional, "username",
            Description = "Username to use when connecting to the server for authentication.",
            ParameterDescription = "<username>",
            Shortcut = "u")]
        public string UserName
        {
            get
            {
                return this._userName;
            }
            set
            {
                this._userName = value;
            }
        }

        /// <summary>
        /// Password to use when connecting to the server for authentication.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Called via reflection")]
        [CommandLineArgument(ArgumentType.Optional, "password",
            Description = "Password to use when connecting to the server for authentication.",
            ParameterDescription = "<password>",
            Shortcut = "p")]
        public string Password
        {
            get
            {
                return this._password;
            }
            set
            {
                this._password = value;
            }
        }

        /// <summary>
        /// Domain to authenticate against when connecting to the server.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Called via reflection")]
        [CommandLineArgument(ArgumentType.Optional, "domain",
            Description = "Domain to authenticate against when connecting to the server.",
            ParameterDescription = "<domain>",
            Shortcut = "d")]
        public string Domain
        {
            get
            {
                return this._domain;
            }
            set
            {
                this._domain = value;
            }
        }

        /// <summary>
        /// The name for the generated service context. If a value is passed in, it will be used for the Service Context.  If not, no Service Context will be generated
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Called via reflection")]
        [CommandLineArgument(ArgumentType.Optional, "serviceContextName", Shortcut = "svcctx",
            Description = "The name for the generated service context. If a value is passed in, it will be used for the Service Context.  If not, no Service Context will be generated",
            ParameterDescription = "<service context name>")]
        public string ServiceContextName
        {
            get
            {
                return this._serviceContextName;
            }
            set
            {
                this._serviceContextName = value;
            }
        }

        /// <summary>
        /// Namespace of messages to generate.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Called via reflection")]
        [CommandLineArgument(ArgumentType.Optional | ArgumentType.Hidden, "messageNamespace",
            Description = "Namespace of messages to generate.",
            ParameterDescription = "<message namespace>",
            Shortcut = "m")]
        public string MessageNamespace
        {
            get
            {
                return this._messageNamespace;
            }
            set
            {
                this._messageNamespace = value;
            }
        }

        /// <summary>
        /// Show this usage message.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Called via reflection")]
        [CommandLineArgument(ArgumentType.Optional | ArgumentType.Binary, "help",
            Shortcut = "?",
            Description = "Show this usage message.")]
        public bool ShowHelp
        {
            get
            {
                return this._showHelp;
            }
            set
            {
                this._showHelp = value;
            }
        }

        /// <summary>
        /// Full name of the type to use as the ICustomizeCodeDomService
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Called via reflection")]
        [CommandLineArgument(ArgumentType.Optional | ArgumentType.Hidden, "codecustomization",
            Description = "Full name of the type to use as the ICustomizeCodeDomService",
            ParameterDescription = "<typename>")]
        public string CodeCustomizationService
        {
            get
            {
                return this._codeCustomizationService;
            }
            set
            {
                this._codeCustomizationService = value;
            }
        }

        /// <summary>
        /// Full name of the type to use as the ICodeWriterFilterService
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Called via reflection")]
        [CommandLineArgument(ArgumentType.Optional | ArgumentType.Hidden, "codewriterfilter",
            Description = "Full name of the type to use as the ICodeWriterFilterService",
            ParameterDescription = "<typename>")]
        public string CodeWriterFilterService
        {
            get
            {
                return this._codeWriterFilterService;
            }
            set
            {
                this._codeWriterFilterService = value;
            }
        }

        /// <summary>
        /// Full name of the type to use as the ICodeWriterMessageFilterService
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Called via reflection")]
        [CommandLineArgument(ArgumentType.Optional | ArgumentType.Hidden, "codewritermessagefilter",
            Description = "Full name of the type to use as the ICodeWriterMessageFilterService",
            ParameterDescription = "<typename>")]
        public string CodeWriterMessageFilterService
        {
            get
            {
                return this._codeWriterMessageFilterService;
            }
            set
            {
                this._codeWriterMessageFilterService = value;
            }
        }

        /// <summary>
        /// Full name of the type to use as the IMetadataProviderService
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Called via reflection")]
        [CommandLineArgument(ArgumentType.Optional | ArgumentType.Hidden, "metadataproviderservice",
            Description = "Full name of the type to use as the IMetadataProviderService",
            ParameterDescription = "<typename>")]
        public string MetadataProviderService
        {
            get
            {
                return this._metadataProviderService;
            }
            set
            {
                this._metadataProviderService = value;
            }
        }

        /// <summary>
        /// Full name of the type to use as the IMetaDataProviderQueryService
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Called via reflection")]
        [CommandLineArgument(ArgumentType.Optional | ArgumentType.Hidden, "metadataproviderqueryservice",
            Description = "Full name of the type to use as the IMetaDataProviderQueryService",
            ParameterDescription = "<typename>")]
        public string MetadataQueryProviderService
        {
            get
            {
                return this._metadataQueryProvider;
            }
            set
            {
                this._metadataQueryProvider = value;
            }
        }

        /// <summary>
        /// Full name of the type to use as the ICodeGenerationService
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Called via reflection")]
        [CommandLineArgument(ArgumentType.Optional | ArgumentType.Hidden, "codegenerationservice",
            Description = "Full name of the type to use as the ICodeGenerationService",
            ParameterDescription = "<typename>")]
        public string CodeGenerationService
        {
            get
            {
                return this._codeGenerationService;
            }
            set
            {
                this._codeGenerationService = value;
            }
        }

        /// <summary>
        /// Full name of the type to use as the INamingService
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Called via reflection")]
        [CommandLineArgument(ArgumentType.Optional | ArgumentType.Hidden, "namingservice",
            Description = "Full name of the type to use as the INamingService",
            ParameterDescription = "<typename>")]
        public string NamingService { get; set; }


        /// <summary>
        /// Generate unsupported classes
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Called via reflection")]
        [CommandLineArgument(ArgumentType.Optional | ArgumentType.Hidden | ArgumentType.Binary, "private",
            Description = "Generate unsupported classes",
            ParameterDescription = "<private>")]
        internal bool Private { get; set; } = false;


        /// <summary>
        /// Generate wrapper classes for custom actions
        /// </summary>
        [CommandLineArgument(ArgumentType.Optional | ArgumentType.Binary, "generatesdkmessages",
            Description = "Generate sdk message classes for messages that are not in Microsoft.Xrm.Sdk.Messages or Microsoft.Crm.Sdk.Messages",
            Shortcut = "a")]
        public bool GenerateSdkMessages { get; set; } = false;


        /// <summary>
        /// Generate Field Constants for entities
        /// </summary>
        [CommandLineArgument(ArgumentType.Optional | ArgumentType.Optional | ArgumentType.Binary, "emitfieldsclasses", Shortcut = "emitfc",
            Description = "Generate a Fields Class per Entity that contains all of the field names at the time of code generation")]
        public bool EmitFieldClasses { get; set; } = false;

        /// <summary>
        /// Provides a means to provide a filtered list of entities.
        /// </summary>
        [CommandLineArgument(ArgumentType.Optional | ArgumentType.Optional, "entitynamesfilter",
            Description = "Filters the list of entities are retrieved when reading data from Dataverse. Passed in as a semicolon separated list.  Using the form <entitylogicalname>;<entitylogicalname>",
            SampleUsageValue = "/entitynamesfilter:account;contact")]
        public string EntityNamesFilter { get; set; } = string.Empty;

        /// <summary>
        /// Provides a means to provide a filtered list of entities.
        /// </summary>
        [CommandLineArgument(ArgumentType.Optional | ArgumentType.Optional, "messagenamesfilter",
            Description = "Filters the list of messages that are retrieved when reading data from Dataverse. Passed in as a semicolon separated list, required messages ( Create, Update, Delete, Retrieve, RetrieveMultiple, Associate and Disassociate) are always included. An * can be used to proceed or trail an message allowing for all messages starting with or ending with a string.  Using the form <messagename>;<messagename>",
            SampleUsageValue = "/messagenamesfilter:msdyn_mymessage;new_*;*update")]
        public string MessageNamesFilter { get; set; } = string.Empty;

        /// <summary>
        /// Askes the system to emit a file per type.
        /// </summary>
        [CommandLineArgument(ArgumentType.Optional | ArgumentType.Optional | ArgumentType.Binary, "splitfiles",
            Description = "Splits the output into files by type, organized by entity, message, optionsets.  when enabled the /out property is ignored and /outdirectory is required instead")]
        public bool SplitFilesByObject { get; set; } = false;

        /// <summary>
        /// Sets the output directory for files when split files are setup
        /// </summary>
        [CommandLineArgument(ArgumentType.Optional | ArgumentType.Optional, "outdirectory", Shortcut = "outdir",
            Description = "Valid only with /SplitToFiles Option.  Write Directory for entity, message and optionset files")]
        public string OutDirectory { get; set; } = string.Empty;

        /// <summary>
        /// Sets the folder name for Entity files.
        /// </summary>
        [CommandLineArgument(ArgumentType.Optional, "entitytypesfolder",
            Description = "Valid only with /SplitToFiles Option.  Folder name that will contain entities. default is Entities")]
        public string EntityFolderName { get; set; } = "Entities";

        /// <summary>
        /// Sets the folder name for Messages files.
        /// </summary>
        [CommandLineArgument(ArgumentType.Optional, "messagestypesfolder",
            Description = "Valid only with /SplitToFiles Option.  Folder name that will contain messages.  default is Messages")]
        public string MessagesFolderName { get; set; } = "Messages";

        /// <summary>
        /// Sets the folder name for OptionSets files.
        /// </summary>
        [CommandLineArgument(ArgumentType.Optional, "optionsetstypesfolder",
            Description = "Valid only with /SplitToFiles Option.  Folder name that will contain messages.  default is OptionSets")]
        public string OptionSetFolderName { get; set; } = "OptionSets";


        /// <summary>
        /// If set, Causes Global optionsets to be read from the server and emitted.
        /// </summary>
        [CommandLineArgument(ArgumentType.Optional | ArgumentType.Binary, "generateGlobalOptionSets", Description = "Emit all Global OptionSets, note: if an entity contains a reference to a global optionset, it will be emitted even if this switch is not present. ",
            SampleUsageValue = "/generateGlobalOptionSets")]
        public bool GenerateGlobalOptionSets { get; set; }

        /// <summary>
        /// if Set, Causes the system to behave as the CrmServiceUtil did pre Feb/2022.
        /// </summary>
        [CommandLineArgument(ArgumentType.Optional | ArgumentType.Binary, "legacyMode", Description = "disabled emitting optionsets and many newer code features to support compatibility with older extensions. ",
            SampleUsageValue = "/legacyMode")]
        public bool LegacyMode { get; set; }

        /// <summary>
        /// if Set, Causes the system to behave as the CrmServiceUtil did pre Feb/2022.
        /// </summary>
        [CommandLineArgument(ArgumentType.Optional | ArgumentType.Binary, "emitEntityETC", Description = "when set, includes the entity ETC ( entity type code ) in the generated code.",
            SampleUsageValue = "/emitEntityETC", Shortcut = "etc")]
        public bool EmitEntityETC { get; set; } = false;

        [CommandLineArgument(ArgumentType.Optional | ArgumentType.Binary, "emitVirtualAttributes", Description = "when set, includes the Virtual Attributes of entities in the generated code.",
            SampleUsageValue = "/emitVirtualAttributes", Shortcut = "eva")]
        public bool EmitVirtualAttributes { get; set; }

        /// <summary>
        /// Used by devToolkit to set the Connection profile to use for this call.
        /// </summary>
        [CommandLineArgument(ArgumentType.Optional | ArgumentType.Hidden, "connectionprofilename", Description = "connection profile name used")]
        public string ConnectionProfileName { get; set; }

        /// <summary>
        /// Used by the devToolkit to set the appName whos connection is being used.
        /// </summary>
        [CommandLineArgument(ArgumentType.Optional | ArgumentType.Hidden, "connectionname", Description = "Application Name whose connection to use")]
        public string ConnectionAppName { get; set; }

        /// <summary>
        /// Used to create a settings file based on the current settings being passed in to the command line.
        /// </summary>
        [CommandLineArgument(ArgumentType.Optional | ArgumentType.Binary, "writesettingsTemplateFile", Description = "When Set, writes a settings file out to the output directory with the current passed settings or default settings" , Shortcut ="wstf")]
        public bool WriteSettingsTemplate { get; set; }

        /// <summary>
        /// Settings File created by WriteSettingsTemplate, populated with the setting to be used.  Cannot be used with writeSettingsTemplate. 
        /// </summary>
        [CommandLineArgument(ArgumentType.Optional , "settingsTemplateFile", Description = "Contains Settings to be used for this run of the Dataverse Model Builder, overrides any duplicate parameters on command line.  Cannot be set when /writesettingstemplate is used.", Shortcut ="stf")]
        public string SettingsTemplateFile { get; set; }

        /// <summary>
        /// Settings File created by WriteSettingsTemplate, populated with the setting to be used.  Cannot be used with writeSettingsTemplate. 
        /// </summary>
        [CommandLineArgument(ArgumentType.Optional | ArgumentType.Hidden | ArgumentType.Binary, "suppressINotifyPattern", Description = "When enabled, does not write the INotify wrappers for properties and classes.")]
        public bool SuppressINotifyPattern { get; set; } = false;

        /// <summary>
        /// Contains the default system language to use
        /// this is set when using the default meta data readers and is utilized by the naming service to support missing LCID info for the default 1033 lookup.
        /// </summary>
        public int? SystemDefaultLanguageId { get; set; } = null;

        /// <summary>
        /// Gets Dictionary of parameters
        /// </summary>
        /// <returns></returns>
        public IDictionary<string, string> ToDictionary()
        {
            if (_parametersAsDictionary == null)
            {
                _parametersAsDictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                foreach (PropertyInfo property in typeof(ModelBuilderInvokeParameters).GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    object[] attributes = property.GetCustomAttributes(typeof(CommandLineArgumentAttribute), false);
                    if (attributes != null && attributes.Length > 0)
                    {
                        CommandLineArgumentAttribute argumentAttribute = (CommandLineArgumentAttribute)attributes[0];
                        object value = property.GetValue(this, null);
                        if (value != null)
                        {
                            _parametersAsDictionary.Add(argumentAttribute.Name, value.ToString());
                        }
                    }
                }

                foreach (string key in _unknownParameters.Keys)
                {
                    _parametersAsDictionary.Add(key, _unknownParameters[key]);
                }
            }
            return _parametersAsDictionary;
        }

        private CommandLineParser Parser
        {
            get
            {
                return this._parser;
            }
        }

        private bool ContainsUnknownParameters
        {
            get
            {
                if (_unknownParameters.Count == 0)
                    return false;

                if (!String.IsNullOrWhiteSpace(this.CodeCustomizationService))
                    return false;
                if (!String.IsNullOrWhiteSpace(this.CodeGenerationService))
                    return false;
                if (!String.IsNullOrWhiteSpace(this.MetadataQueryProviderService))
                    return false;
                if (!String.IsNullOrWhiteSpace(this.CodeWriterFilterService))
                    return false;
                if (!String.IsNullOrWhiteSpace(this.CodeWriterMessageFilterService))
                    return false;
                if (!String.IsNullOrWhiteSpace(this.MetadataProviderService))
                    return false;
                if (!String.IsNullOrWhiteSpace(this.NamingService))
                    return false;

                return true;
            }
        }
        #endregion

        #region Methods
        public void LoadArguments(string[] args)
        {
            Logger.TraceMethodStart();
            // Read initial Settings and populate args array
            args = BuilderSettings.SettingsParser.ReadSettingsFileIntoArgs(args);

            this.Parser.ParseArguments(args);

            // write the template out if requested to do so. 
            if (WriteSettingsTemplate)
                BuilderSettings.SettingsParser.WriteSettingsFile(this);

            Logger.TraceMethodStop();
        }

        public bool VerifyArguments()
        {
            //TODO : Need to update verify to detect updated args.
            Logger.TraceMethodStart();
            if (!this.Parser.VerifyArguments())
            {
                Logger.TraceWarning("Exiting {0} with false return value due to the parser finding invalid arguments");
                return false;
            }

            //if (this.ContainsUnknownParameters)
            //{
            //    _modeBuilderLogger.TraceWarning("Exiting {0} with false return value due to finding unknown parameters");
            //    return false;
            //}

            if (!String.IsNullOrEmpty(this.UserName))
            {
                if (String.IsNullOrEmpty(this.Password))
                {
                    Logger.TraceWarning("Exiting {0} with false return value due to invalid credentials");
                    return false;
                }
            }
            Logger.TraceMethodStop();
            return true;
        }

        public void ShowUsage()
        {
            Logger.TraceMethodStart();
            this.Parser.WriteUsage();
            Logger.TraceMethodStop();
        }

        public void ShowLogo()
        {
            Logger.TraceMethodStart();
            Logger.WriteConsole(String.Format(CultureInfo.InvariantCulture,
                "{0} : {1} [Version {2}]",
                StaticUtils.ApplicationName,
                StaticUtils.ApplicationDescription,
                StaticUtils.ApplicationVersion), false, Status.ProcessStage.Help);
            Logger.WriteConsole(StaticUtils.ApplicationCopyright, false, Status.ProcessStage.Help);
            Logger.WriteConsole("", false, Status.ProcessStage.Help);
            Logger.TraceMethodStop();
        }

        #endregion

        #region ICommandLineArgumentSource Implementation

        void ICommandLineArgumentSource.OnUnknownArgument(string argumentName, string argumentValue)
        {
            Logger.WriteConsoleWarning($"Found unknown argument {CommandLineArgument.ArgumentStartChar}{argumentName}.", false, Status.ProcessStage.ParseParamaters);
            Logger.TraceWarning("{0}: Found unknown argument {1}{2}.",
                MethodBase.GetCurrentMethod().Name, CommandLineArgument.ArgumentStartChar, argumentName);
            this._unknownParameters[argumentName] = argumentValue;
        }

        void ICommandLineArgumentSource.OnInvalidArgument(string argument)
        {
            Logger.TraceError("Exiting {0}: Found string {1} in arguments array that could not be parsed.",
                MethodBase.GetCurrentMethod().Name, argument);
            throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture,
                "Argument '{0}' could not be parsed.", argument));
        }
        #endregion
    }
}
