using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PowerPlatform.Dataverse.ModelBuilderLib
{
    internal static class Extensions
    {
        private const string XrmAttributeLogicalName = "Microsoft.Xrm.Sdk.AttributeLogicalNameAttribute";
        private const string XrmRelationshipSchemaName = "Microsoft.Xrm.Sdk.RelationshipSchemaNameAttribute";

        public static bool IsContextType(this CodeTypeDeclaration type)
        {
            var baseType = type.BaseTypes[0].BaseType;
            return baseType == "Microsoft.Xrm.Client.CrmOrganizationServiceContext"
                   || baseType == "Microsoft.Xrm.Sdk.Client.OrganizationServiceContext";
        }

        public static bool IsBaseEntityType(this CodeTypeDeclaration type)
        {
            var name = type.Name;
            return name == "OrganizationOwnedEntity"
                   || name == "UserOwnedEntity";
        }

        public static string GetFieldInitalizedValue(this CodeTypeDeclaration type, string fieldName)
        {
            var field = type.Members.OfType<CodeMemberField>().FirstOrDefault(f => f.Name == fieldName);
            if (field != null)
            {
                return ((CodePrimitiveExpression)field.InitExpression).Value.ToString();
            }
            return null;
            //throw new Exception("Field " + fieldName + " was not found for type " + type.Name);
        }

        public static string GetLogicalName(this CodeMemberProperty property)
        {
            return
                (from CodeAttributeDeclaration att in property.CustomAttributes
                 where att.AttributeType.BaseType == XrmAttributeLogicalName
                 select ((CodePrimitiveExpression)att.Arguments[0].Value).Value.ToString()).FirstOrDefault();
        }

        /// <summary>
        /// Returns a duration in the format hh:mm:ss:fff
        /// </summary>
        /// <param name="timspan"></param>
        /// <returns></returns>
        internal static string ToDurationString(this TimeSpan timspan)
        {
            return timspan.ToString(@"hh\:mm\:ss\.fff");
        }
    }
}
