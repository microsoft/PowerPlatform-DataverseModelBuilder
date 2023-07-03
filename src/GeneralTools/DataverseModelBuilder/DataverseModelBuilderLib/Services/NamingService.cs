using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk;
using System.Globalization;
using System.Reflection;
using System.ComponentModel.Design;

namespace Microsoft.PowerPlatform.Dataverse.ModelBuilderLib
{
    /// <summary>
    /// Special class to hold hardcoded entity and attribute name mappings.
    /// </summary>
    internal static class StaticNamingService
    {
        #region StaticNamingService Members

        /// <summary>
        /// Retrieves a name for the Entity being generated.
        /// </summary>
        public static string GetNameForEntity(EntityMetadata entityMetadata)
        {
            switch (entityMetadata.LogicalName)
            {
                case "activitymimeattachment":
                    return "ActivityMimeAttachment";
                case "monthlyfiscalcalendar":
                    return "MonthlyFiscalCalendar";
                case "fixedmonthlyfiscalcalendar":
                    return "FixedMonthlyFiscalCalendar";
                case "quarterlyfiscalcalendar":
                    return "QuarterlyFiscalCalendar";
                case "semiannualfiscalcalendar":
                    return "SemiAnnualFiscalCalendar";
                case "annualfiscalcalendar":
                    return "AnnualFiscalCalendar";
                default:
                    return string.Empty;
            }
        }

        public static string GetNameForAttribute(AttributeMetadata attributeMetadata)
        {
            return _attributeNames.ContainsKey(attributeMetadata.LogicalName)
                ? _attributeNames[attributeMetadata.LogicalName]
                : null;
        }

        #endregion

        /// <summary>
        /// Static constructor.
        /// </summary>
        static StaticNamingService()
        {
            InitializeAtributeNames();
        }

        private static void InitializeAtributeNames()
        {
            if (_attributeNames != null) return;

            _attributeNames = new Dictionary<string, string>
            {
                { "month1", "Month1" },
                { "month1_base", "Month1_Base" },
                { "month2", "Month2" },
                { "month2_base", "Month2_Base" },
                { "month3", "Month3" },
                { "month3_base", "Month3_Base" },
                { "month4", "Month4" },
                { "month4_base", "Month4_Base" },
                { "month5", "Month5" },
                { "month5_base", "Month5_Base" },
                { "month6", "Month6" },
                { "month6_base", "Month6_Base" },
                { "month7", "Month7" },
                { "month7_base", "Month7_Base" },
                { "month8", "Month8" },
                { "month8_base", "Month8_Base" },
                { "month9", "Month9" },
                { "month9_base", "Month9_Base" },
                { "month10", "Month10" },
                { "month10_base", "Month10_Base" },
                { "month11", "Month11" },
                { "month11_base", "Month11_Base" },
                { "month12", "Month12" },
                { "month12_base", "Month12_Base" },
                { "quarter1", "Quarter1" },
                { "quarter1_base", "Quarter1_Base" },
                { "quarter2", "Quarter2" },
                { "quarter2_base", "Quarter2_Base" },
                { "quarter3", "Quarter3" },
                { "quarter3_base", "Quarter3_Base" },
                { "quarter4", "Quarter4" },
                { "quarter4_base", "Quarter4_Base" },
                { "firsthalf", "FirstHalf" },
                { "firsthalf_base", "FirstHalf_Base" },
                { "secondhalf", "SecondHalf" },
                { "secondhalf_base", "SecondHalf_Base" },
                { "annual", "Annual" },
                { "annual_base", "Annual_Base" },
                { "requiredattendees", "RequiredAttendees" },
                { "from", "From"},
                { "to", "To"},
                { "cc", "Cc"},
                { "bcc", "Bcc"}
            };
        }

        private static Dictionary<string, string> _attributeNames;
    }

    internal sealed class NamingService : INamingService
    {
        #region Fields

        // TODO: Decide on a way to resolve naming conflicts instead of just appending a "1"
        private const string ConflictResolutionSuffix = "1";
        private const string ReferencingReflexiveRelationshipPrefix = "Referencing";
        private const string ReferencedReflexiveRelationshipPrefix = "Referenced";

        private string _serviceContextName;
        private Dictionary<string, int> _nameMap;
        private Dictionary<string, string> _knowNames;
        private List<string> _reservedAttributeNames;
        private static Regex nameRegex = new Regex(@"[a-z0-9_]*", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        private readonly ModelBuilderInvokeParameters _parameters = null; 

        /// <summary>
        /// List of attribute names that will conflict with system properties.
        /// </summary>
        private static string[] _reservedSystemAttributeNames = new string[] { "EntityLogicalName", "EntitySetName", "EntityTypeCode" };

        #endregion

        #region Constructors
        internal NamingService(ModelBuilderInvokeParameters parameters)
        {
            if (!String.IsNullOrWhiteSpace(parameters.ServiceContextName))
                this._serviceContextName = parameters.ServiceContextName;
            else
                this._serviceContextName = typeof(Microsoft.Xrm.Sdk.Client.OrganizationServiceContext).Name + "1";

            this._nameMap = new Dictionary<string, int>();
            this._knowNames = new Dictionary<string, string>();
            this._reservedAttributeNames = new List<string>();
            foreach (PropertyInfo prop in typeof(Microsoft.Xrm.Sdk.Entity).GetProperties())
            {
                this._reservedAttributeNames.Add(prop.Name);
            }
            // Add additional reserved
            this._reservedAttributeNames.AddRange(_reservedSystemAttributeNames);
            _parameters = parameters; 
        }
        #endregion

        #region INamingService Members
        string INamingService.GetNameForOptionSet(EntityMetadata entityMetadata, OptionSetMetadataBase optionSetMetadata, IServiceProvider services)
        {
            if (this._knowNames.ContainsKey(optionSetMetadata.MetadataId.Value.ToString()))
                return this._knowNames[optionSetMetadata.MetadataId.Value.ToString()];

            string optionSetName = null;
            if (_parameters.LegacyMode)
            {
                if (optionSetMetadata.OptionSetType.Value == OptionSetType.State)
                {
                    optionSetName = this.CreateValidTypeName(entityMetadata.SchemaName + "State");
                }
                else
                {
                    optionSetName = this.CreateValidTypeName(optionSetMetadata.Name);
                }
            }
            else
            {
                optionSetName = this.CreateValidTypeName(optionSetMetadata.Name);
            }
            this._knowNames.Add(optionSetMetadata.MetadataId.Value.ToString(), optionSetName);
            return optionSetName;
        }

        string INamingService.GetNameForOption(OptionSetMetadataBase optionSetMetadata, OptionMetadata optionMetadata, IServiceProvider services)
        {
            if (this._knowNames.ContainsKey(optionSetMetadata.MetadataId.Value.ToString() + optionMetadata.Value.Value.ToString(CultureInfo.InvariantCulture)))
                return this._knowNames[optionSetMetadata.MetadataId.Value.ToString() + optionMetadata.Value.Value.ToString(CultureInfo.InvariantCulture)];

            string name = String.Empty;
            StateOptionMetadata stateOption = optionMetadata as StateOptionMetadata;
            if (stateOption != null)
            {
                name = stateOption.InvariantName;
            }

            if (string.IsNullOrEmpty(name)) // Name still null try this area. 
            {
                _parameters.SystemDefaultLanguageId ??= 1033; // Set default 1033 (EN-US) language if not specified. 

                if (optionMetadata.Label != null && 
                    optionMetadata.Label.LocalizedLabels.Any()) // Counter check for localization miss ( no 1033 label ) 
                {
                    // Need to add get for current system default language. 
                    LocalizedLabel lblToUse = optionMetadata.Label.LocalizedLabels.FirstOrDefault(f => f.LanguageCode== _parameters.SystemDefaultLanguageId.Value);
                    if ( lblToUse != null && !string.IsNullOrEmpty(lblToUse.Label))
                        name = lblToUse.Label;
                }

                // Fail over check. 
                if (string.IsNullOrEmpty(name) &&
                    optionMetadata.Label != null &&
                    optionMetadata.Label.LocalizedLabels.Any())
                {
                    // For whatever reason, the system default language did not return a name, try to get the first label available. 
                    LocalizedLabel lblToUse = optionMetadata.Label.LocalizedLabels.FirstOrDefault();
                    if (lblToUse != null && !string.IsNullOrEmpty(lblToUse.Label))
                        name = lblToUse.Label;
                }
            }

            name = CreateValidName(name);

            if (String.IsNullOrEmpty(name))
                name = String.Format(CultureInfo.InvariantCulture, "UnknownLabel{0}", optionMetadata.Value.Value);

            this._knowNames.Add(optionSetMetadata.MetadataId.Value.ToString() + optionMetadata.Value.Value.ToString(CultureInfo.InvariantCulture), name);
            return name;
        }

        string INamingService.GetNameForEntity(EntityMetadata entityMetadata, IServiceProvider services)
        {
            if (entityMetadata.MetadataId.HasValue && this._knowNames.ContainsKey(entityMetadata.MetadataId.Value.ToString()))
            {
                return this._knowNames[entityMetadata.MetadataId.Value.ToString()];
            }

            string name = (String.IsNullOrEmpty(StaticNamingService.GetNameForEntity(entityMetadata))) ? entityMetadata.SchemaName : StaticNamingService.GetNameForEntity(entityMetadata);
            string typeName = this.CreateValidTypeName(name);

            // Filter out any entity that will create a type that is already present in Microsoft.Xrm.Sdk Namespace.
            var s = System.Reflection.Assembly.GetAssembly(typeof(Microsoft.Xrm.Sdk.AliasedValue)).GetTypes().Where(w => w.FullName.Equals($"Microsoft.Xrm.Sdk.{entityMetadata.LogicalName}", StringComparison.OrdinalIgnoreCase));
            if (s.Any())
            {
                // Updating the name of the Entity,
                _parameters.Logger.TraceWarning($"Adding _Ent to Type to  {s.FirstOrDefault().FullName}, Type exists in supporting assemblies");
                typeName = $"{typeName}_Ent";
            }

            if (entityMetadata.MetadataId.HasValue)
            {
                this._knowNames.Add(entityMetadata.MetadataId.Value.ToString(), typeName);
            }

            return typeName;
        }

        string INamingService.GetNameForAttribute(EntityMetadata entityMetadata, AttributeMetadata attributeMetadata, IServiceProvider services)
        {
            if (this._knowNames.ContainsKey(entityMetadata.MetadataId.Value.ToString() + attributeMetadata.MetadataId.Value))
                return this._knowNames[entityMetadata.MetadataId.Value.ToString() + attributeMetadata.MetadataId.Value];

            string name = StaticNamingService.GetNameForAttribute(attributeMetadata) ?? attributeMetadata.SchemaName;

            // Ensure we have a valid name.
            // Name does not override base member and is not the same as the enclosing type.
            name = CreateValidName(name);
            var namingService = (INamingService)services.GetService(typeof(INamingService));
            if (this._reservedAttributeNames.Contains(name) || name == namingService.GetNameForEntity(entityMetadata, services))
                name = name + ConflictResolutionSuffix;

            this._knowNames.Add(entityMetadata.MetadataId.Value.ToString() + attributeMetadata.MetadataId.Value, name);
            return name;
        }

        string INamingService.GetNameForRelationship(EntityMetadata entityMetadata, RelationshipMetadataBase relationshipMetadata, EntityRole? reflexiveRole, IServiceProvider services)
        {
            var role = reflexiveRole.HasValue ? reflexiveRole.Value.ToString() : string.Empty;
            Dictionary<string, string> existingValues;
            if (this._knowNames.ContainsKey(entityMetadata.MetadataId.Value.ToString() + relationshipMetadata.MetadataId.Value + role))
                return this._knowNames[entityMetadata.MetadataId.Value.ToString() + relationshipMetadata.MetadataId.Value + role];

            var name = reflexiveRole == null
                ? relationshipMetadata.SchemaName
                : reflexiveRole.Value == EntityRole.Referenced
                    ? ReferencedReflexiveRelationshipPrefix + relationshipMetadata.SchemaName
                    : ReferencingReflexiveRelationshipPrefix + relationshipMetadata.SchemaName;

            // Ensure we have a valid name.
            // Name does not override base member and is not the same as the enclosing type.
            name = CreateValidName(name);
            //Ensure we have any duplicate attribute/Relationship values for the entity
            existingValues = this._knowNames.Where(d => d.Key.StartsWith(entityMetadata.MetadataId.Value.ToString())).ToDictionary(d => d.Key, d => d.Value);

            var namingService = (INamingService)services.GetService(typeof(INamingService));
            if (this._reservedAttributeNames.Contains(name) || name == namingService.GetNameForEntity(entityMetadata, services) || (existingValues.ContainsValue(name)))
                name = name + ConflictResolutionSuffix;

            this._knowNames.Add(entityMetadata.MetadataId.Value.ToString() + relationshipMetadata.MetadataId.Value + role, name);
            return name;
        }

        string INamingService.GetNameForServiceContext(IServiceProvider services)
        {
            return _serviceContextName;
        }

        string INamingService.GetNameForEntitySet(EntityMetadata entityMetadata, IServiceProvider services)
        {
            var namingService = (INamingService)services.GetService(typeof(INamingService));
            return namingService.GetNameForEntity(entityMetadata, services) + "Set";
        }

        string INamingService.GetNameForMessagePair(SdkMessagePair messagePair, IServiceProvider services)
        {
            if (this._knowNames.ContainsKey(messagePair.Id.ToString()))
                return this._knowNames[messagePair.Id.ToString()];
            else
            {
                string name = this.CreateValidTypeName(messagePair.Request.Name);
                this._knowNames.Add(messagePair.Id.ToString(), name);
                return name;
            }
        }

        string INamingService.GetNameForRequestField(SdkMessageRequest request, SdkMessageRequestField requestField, IServiceProvider services)
        {
            if (this._knowNames.ContainsKey(request.Id.ToString() + requestField.Index.ToString(CultureInfo.InvariantCulture)))
                return this._knowNames[request.Id.ToString() + requestField.Index.ToString(CultureInfo.InvariantCulture)];

            string name = CreateValidName(requestField.Name);
            this._knowNames.Add(request.Id.ToString() + requestField.Index.ToString(CultureInfo.InvariantCulture), name);
            return name;
        }

        string INamingService.GetNameForResponseField(SdkMessageResponse response, SdkMessageResponseField responseField, IServiceProvider services)
        {
            if (this._knowNames.ContainsKey(response.Id.ToString() + responseField.Index.ToString(CultureInfo.InvariantCulture)))
                return this._knowNames[response.Id.ToString() + responseField.Index.ToString(CultureInfo.InvariantCulture)];

            string name = CreateValidName(responseField.Name);
            this._knowNames.Add(response.Id.ToString() + responseField.Index.ToString(CultureInfo.InvariantCulture), name);
            return name;
        }
        #endregion

        private string CreateValidTypeName(string name)
        {
            string validName = CreateValidName(name);
            if (this._nameMap.ContainsKey(validName))
            {
                int currentIndex = this._nameMap[validName];
                currentIndex++;
                this._nameMap[validName] = currentIndex;
                return String.Format(CultureInfo.InvariantCulture, "{0}{1}", validName, currentIndex);
            }
            else
            {
                this._nameMap.Add(name, 0);
                return validName;
            }
        }

        private static string CreateValidName(string name)
        {
            if (string.IsNullOrEmpty(name))
                name = "Unknown"; 

            string validName = name.Replace("$", "CurrencySymbol_").Replace("(", "_");

            StringBuilder sb = new StringBuilder();
            Match match = nameRegex.Match(validName);
            while (match.Success)
            {
                if (!string.IsNullOrEmpty(match.Value))
                {
                    if (string.IsNullOrEmpty(sb.ToString()) && !char.IsLetter(match.Value[0]))
                    {
                        sb.Append("_"); // Put a _ in front of anything that starts with a number. 
                    }
                }

                sb.Append(match.Value);
                match = match.NextMatch();
            }
            return sb.ToString();
        }
    }
}
