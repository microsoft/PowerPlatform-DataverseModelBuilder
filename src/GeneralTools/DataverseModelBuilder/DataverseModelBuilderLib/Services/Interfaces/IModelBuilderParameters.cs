namespace Microsoft.PowerPlatform.Dataverse.ModelBuilderLib
{

    /// <summary>
    /// Defines parameters for Dataverse Model builder. 
    /// </summary>
    public interface IModelBuilderParameters
    {
        /************* Services ***************/

        /// <summary>
        /// Full name of the type to use as the ICustomizeCodeDomService
        /// </summary>
        string CodeCustomizationService { get; set; }

        /// <summary>
        /// Full name of the type to use as the ICodeGenerationService
        /// </summary>
        string CodeGenerationService { get; set; }

        /// <summary>
        /// Full name of the type to use as the ICodeWriterFilterService
        /// </summary>
        string CodeWriterFilterService { get; set; }

        /// <summary>
        /// Full name of the type to use as the ICodeWriterMessageFilterService
        /// </summary>
        string CodeWriterMessageFilterService { get; set; }

        /// <summary>
        /// Full name of the type to use as the IMetadataProviderService
        /// </summary>
        string MetadataProviderService { get; set; }

        /// <summary>
        /// Full name of the type to use as the IMetaDataProviderQueryService
        /// </summary>
        string MetadataQueryProviderService { get; set; }

        /// <summary>
        /// Full name of the type to use as the INamingService
        /// </summary>
        string NamingService { get; set; }


        /************* END Services ***************/

        /// <summary>
        /// Generate Field Constants for entities
        /// </summary>
        bool EmitFieldClasses { get; set; }

        /// <summary>
        /// Sets the folder name for Entity files.
        /// </summary>
        string EntityFolderName { get; set; }

        /// <summary>
        /// Provides a means to provide a filtered list of entities.
        /// </summary>
        string EntityNamesFilter { get; set; }
        
        /// <summary>
        /// Generate wrapper classes for custom actions
        /// </summary>
        bool GenerateSdkMessages { get; set; }

        /// <summary>
        /// If set, Causes Global optionsets to be read from the server and emitted.
        /// </summary>
        bool GenerateGlobalOptionSets { get; set; }
       

        /// <summary>
        /// The language to use for the generated proxy code.  This can be either 'CS' or 'VB'.  The default language is 'CS'.
        /// </summary>
        string Language { get; set; }

        /// <summary>
        /// if Set, Causes the system to behave as the CrmServiceUtil did pre Feb/2022.
        /// </summary>
        bool LegacyMode { get; set; }

        /// <summary>
        /// Provides a means to provide a filtered list of entities.
        /// </summary>
        string MessageNamesFilter { get; set; }
        
        /// <summary>
        /// Namespace of messages to generate.
        /// </summary>
        string MessageNamespace { get; set; }

        /// <summary>
        /// Sets the folder name for Messages files.
        /// </summary>
        string MessagesFolderName { get; set; }
        
        /// <summary>
        /// The namespace for the generated proxy code.  The default namespace is the global namespace.
        /// </summary>
        string Namespace { get; set; }

        /// <summary>
        /// Sets the folder name for OptionSets files.
        /// </summary>
        string OptionSetFolderName { get; set; }

        /// <summary>
        /// Sets the output directory for files when split files are setup
        /// </summary>
        string OutDirectory { get; set; }
        
        /// <summary>
        /// The filename for the generated proxy code
        /// </summary>
        string OutputFile { get; set; }
        
        /// <summary>
        /// The name for the generated service context. If a value is passed in, it will be used for the Service Context.  If not, no Service Context will be generated
        /// </summary>
        string ServiceContextName { get; set; }

        /// <summary>
        /// Askes the system to emit a file per type.
        /// </summary>
        bool SplitFilesByObject { get; set; }
        /// <summary>
        /// Suppresses the GeneratedCodeAttribute on all classes.
        /// </summary>
        bool SuppressGenVersionAttribute { get; set; }

        /// <summary>
        /// Includes the ETC code in the generated code if true. ( default is false ).
        /// </summary>
        bool EmitEntityETC { get; set; }

        //--- Connection related ---
        string AccessToken { get; set; }
        string ConnectionAppName { get; set; }
        string ConnectionProfileName { get; set; }
        string ConnectionString { get; set; }
        string Domain { get; set; }

        bool ShowHelp { get; set; }
        bool NoLogo { get; set; }

        string Password { get; set; }
        string Url { get; set; }
        bool UseInteractiveLogin { get; set; }
        string UserName { get; set; }
    }
}