using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PowerPlatform.Dataverse.ModelBuilderLib.Utility
{
    internal static class Utilites
    {
        private static Dictionary<string, object> _locatedKeys = new Dictionary<string, object>();
        public static List<string> GetItemListFromString(IDictionary<string, string> parameters, string separator, string keyName)
        {
            // Pull it from cache if already loaded.
            if (_locatedKeys != null && _locatedKeys.ContainsKey(keyName))
                return (List<string>)_locatedKeys[keyName];

            // Prefer the cmdline if its present.
            if (parameters.ContainsKey(keyName))
            {
                // Points to a list of Action names to use that are separated by ;
                return GetItemList(separator, parameters[keyName], keyName);
            }

            //if (_parameters.ContainsKey(_CONFIGFILEKEY))
            //{
            //    return GetItemListFromConfigFile(_parameters, separator, keyName);
            //}


            return null;

        }


        private static List<string> GetItemList(string seperator, string itemsList, string keyName)
        {
            List<string> itemLst = new List<string>();
            if (!string.IsNullOrEmpty(itemsList))
            {
                // Split list on ;
                List<string> splitPro = new List<string>() { seperator };
                var NameFilterArray = itemsList.Split(splitPro.ToArray(), StringSplitOptions.RemoveEmptyEntries);
                foreach (var itm in NameFilterArray)
                {
                    itemLst.Add(itm);
                }
            }
            _locatedKeys.Add(keyName, itemLst);
            return itemLst;
        }


        internal static string DebugGenerateCodeFromMember(CodeTypeMember co)
        {
            CodeGeneratorOptions options = new CodeGeneratorOptions();
            options.BlankLinesBetweenMembers = true;
            options.BracingStyle = "C";
            options.IndentString = "\t";
            options.VerbatimOrder = true;

            StringBuilder sb = new StringBuilder(); 
            using (StringWriter textWriter = new StringWriter(sb))
            {
                using (CodeDomProvider provider = CodeDomProvider.CreateProvider("CSharp"))
                {
                    provider.GenerateCodeFromMember(co, textWriter, options);
                }
            }
            return sb.ToString();
        }


        internal static string DebugGenerateCodeFromCompileUnit(CodeCompileUnit co)
        {
            CodeGeneratorOptions options = new CodeGeneratorOptions();
            options.BlankLinesBetweenMembers = true;
            options.BracingStyle = "C";
            options.IndentString = "\t";
            options.VerbatimOrder = true;

            StringBuilder sb = new StringBuilder();
            using (StringWriter textWriter = new StringWriter(sb))
            {
                using (CodeDomProvider provider = CodeDomProvider.CreateProvider("CSharp"))
                {
                    provider.GenerateCodeFromCompileUnit(co, textWriter, options);
                }
            }
            return sb.ToString();
        }

        internal static string DebugGenerateCodeFromNameSpace(CodeNamespace co)
        {
            CodeGeneratorOptions options = new CodeGeneratorOptions();
            options.BlankLinesBetweenMembers = true;
            options.BracingStyle = "C";
            options.IndentString = "\t";
            options.VerbatimOrder = true;

            StringBuilder sb = new StringBuilder();
            using (StringWriter textWriter = new StringWriter(sb))
            {
                using (CodeDomProvider provider = CodeDomProvider.CreateProvider("CSharp"))
                {
                    provider.GenerateCodeFromNamespace(co, textWriter, options);
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// If true a child attribute cannot be published or externally consumed.
        /// </summary>

        public static bool IsNotExposedChildAttribute(AttributeMetadata attributeMetadata, ModelBuilderInvokeParameters builderInvokeParameters)
        {
            bool rslt = false; 
            if (builderInvokeParameters.EmitVirtualAttributes)
            {
                rslt = !String.IsNullOrEmpty(attributeMetadata.AttributeOf) &&
                        !(attributeMetadata is ImageAttributeMetadata) &&
                        !attributeMetadata.LogicalName.EndsWith("_url", StringComparison.OrdinalIgnoreCase) &&
                        !attributeMetadata.LogicalName.EndsWith("_timestamp", StringComparison.OrdinalIgnoreCase) &&
                        !((attributeMetadata.LogicalName.Length > 4) && attributeMetadata.LogicalName.EndsWith("name", StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                rslt = !String.IsNullOrEmpty(attributeMetadata.AttributeOf) &&
                    !(attributeMetadata is ImageAttributeMetadata) &&
                    !attributeMetadata.LogicalName.EndsWith("_url", StringComparison.OrdinalIgnoreCase) &&
                    !attributeMetadata.LogicalName.EndsWith("_timestamp", StringComparison.OrdinalIgnoreCase);
            }
            return rslt;
        }

        public static bool IsReadFromFormatedValues(AttributeMetadata attributeMetadata, ModelBuilderInvokeParameters builderInvokeParameters)
        {
            bool rslt = false;
            if (builderInvokeParameters.EmitVirtualAttributes)
            {
                rslt = !String.IsNullOrEmpty(attributeMetadata.AttributeOf) &&
                        ((attributeMetadata.LogicalName.Length > 4) && attributeMetadata.LogicalName.EndsWith("name", StringComparison.OrdinalIgnoreCase));
            }
            return rslt;
        }
    }
}
