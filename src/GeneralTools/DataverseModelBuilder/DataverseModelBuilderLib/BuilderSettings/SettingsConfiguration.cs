using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Microsoft.PowerPlatform.Dataverse.ModelBuilderLib.BuilderSettings
{
    /// <summary>
    /// Settings Model class to support configuring runs of the Dataverse Model Builder. 
    /// </summary>
    internal class SettingsConfiguration
    {
        public bool? SuppressINotifyPattern { get; set; }
        public bool? SuppressGeneratedCodeAttribute { get; set; }
        public string Language { get; set; } = "CS";
        public string Namespace { get; set; } = string.Empty;
        public string ServiceContextName { get; set; } = string.Empty;
        public bool? GenerateSdkMessages { get; set; }
        public bool? GenerateActions { get; set; } // Holder for backward compatibility to 1.0 of DVMB
        public bool? GenerateGlobalOptionSets { get; set; }
        public bool? EmitFieldsClasses { get; set; }
        public string EntityTypesFolder { get; set; } = "Entities";
        public string MessagesTypesFolder { get; set; } = "Messages";
        public string OptionSetsTypesFolder { get; set; } = "OptionSets";
        public List<string> EntityNamesFilter { get; set; }
        public List<string> MessageNamesFilter { get; set; }
        public bool? EmitEntityETC { get; set; }
        public bool? EmitVirtualAttributes { get; set; }

        //Generator Extensibility Features: 
        public string NamingService { get; set; } = null;
        public string CodeGenerationService { get; set; } = null;
        public string MetadataQueryProviderService { get; set; } = null;
        public string MetadataProviderService { get; set; } = null;
        public string CodeWriterMessageFilterService { get; set; } = null;
        public string CodeWriterFilterService { get; set; } = null;
        public string CodeCustomizationService { get; set; } = null;


        /// <summary>
        /// Returns the current class as a JSON document. 
        /// </summary>
        /// <returns></returns>
        public string ToJson()
        {
            var serializeOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = true
            };
            return JsonSerializer.Serialize<SettingsConfiguration>(this, serializeOptions);
        }

        /// <summary>
        /// Returns an a SettingsConfiguration Object from a Json Document. 
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        public static SettingsConfiguration FromJson(string document)
        {
            var serializeOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = true
            };
#pragma warning disable CS8603 // Possible null reference return.
            return JsonSerializer.Deserialize<SettingsConfiguration>(document, serializeOptions);
#pragma warning restore CS8603 // Possible null reference return.
        }
    }
}
