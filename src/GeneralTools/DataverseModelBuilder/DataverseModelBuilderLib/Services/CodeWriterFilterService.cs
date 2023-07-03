using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerPlatform.Dataverse.ModelBuilderLib.Utility;
using Microsoft.Xrm.Sdk.Metadata;

namespace Microsoft.PowerPlatform.Dataverse.ModelBuilderLib
{
	internal sealed class CodeWriterFilterService : ICodeWriterFilterService, ICodeWriterMessageFilterService
	{
		#region Fields
		private static List<string> _excludedNamespaces;
		private bool _generateServiceContext;
		private ModelBuilderInvokeParameters _builderInvokeParameters;
        #endregion

        #region Constructors
        static CodeWriterFilterService()
		{
			_excludedNamespaces = new List<string>();
			_excludedNamespaces.Add("http://schemas.microsoft.com/xrm/2011/contracts");
		}

		internal CodeWriterFilterService(ModelBuilderInvokeParameters parameters)
		{
			_generateServiceContext = !String.IsNullOrWhiteSpace(parameters.ServiceContextName);
			_builderInvokeParameters = parameters;
		}
		#endregion

		#region ICodeWriterFilterService Members
		bool ICodeWriterFilterService.GenerateOptionSet(OptionSetMetadataBase optionSetMetadata, IServiceProvider services)
		{
			if (_builderInvokeParameters.LegacyMode)
			{
				//Legacy mode:
				if (optionSetMetadata.OptionSetType.Value == OptionSetType.State)
					return true;

				return false;
			}
			else
			{
				return true;
			}
		}

		bool ICodeWriterFilterService.GenerateOption(OptionMetadata option, IServiceProvider services)
		{
			return true;
		}

		bool ICodeWriterFilterService.GenerateEntity(EntityMetadata entityMetadata, IServiceProvider services)
		{
			if (entityMetadata == null)
				return false;

			if (entityMetadata.IsIntersect.GetValueOrDefault())
				return true;

			if (String.Equals(entityMetadata.LogicalName, "activityparty", StringComparison.Ordinal))
				return true;

			if (String.Equals(entityMetadata.LogicalName, "calendarrule", StringComparison.Ordinal))
				return true;

//			if (entityMetadata.LogicalName.Equals("rollupjob", StringComparison.OrdinalIgnoreCase))
//				return false;
//

			var metadataProvider = (IMetadataProviderService)services.GetService(typeof(IMetadataProviderService));
            var metadata = metadataProvider.LoadMetadata(services);

			if (!_builderInvokeParameters.GenerateSdkMessages && !_builderInvokeParameters.LegacyMode)
				return true; 
			
			foreach (SdkMessage message in metadata.Messages.MessageCollection.Values)
			{
				if (message.IsPrivate)
					continue;

				foreach (SdkMessageFilter filter in message.SdkMessageFilters.Values)
				{
					if (entityMetadata.ObjectTypeCode != null &&
						filter.PrimaryObjectTypeCode == entityMetadata.ObjectTypeCode.Value)
						return true;
					if (entityMetadata.ObjectTypeCode != null &&
						filter.SecondaryObjectTypeCode == entityMetadata.ObjectTypeCode.Value)
						return true;
				}
			}

			return false;
		}

		bool ICodeWriterFilterService.GenerateAttribute(AttributeMetadata attributeMetadata, IServiceProvider services)
		{
			if (Utilites.IsNotExposedChildAttribute(attributeMetadata, _builderInvokeParameters))
			{
				return false;
			}

			if (!attributeMetadata.IsValidForCreate.GetValueOrDefault() &&
				!attributeMetadata.IsValidForRead.GetValueOrDefault() &&
				!attributeMetadata.IsValidForUpdate.GetValueOrDefault())
				return false;

			if (attributeMetadata.AttributeType != null && attributeMetadata.AttributeType == AttributeTypeCode.Picklist && ((PicklistAttributeMetadata)attributeMetadata).OptionSet.Options.Count == 0)
				return false;

			if (attributeMetadata.AttributeType != null && attributeMetadata.AttributeType == AttributeTypeCode.State && ((StateAttributeMetadata)attributeMetadata).OptionSet.Options.Count == 0)
				return false;

			if (attributeMetadata.AttributeType != null && attributeMetadata.AttributeType == AttributeTypeCode.Status && ((StatusAttributeMetadata)attributeMetadata).OptionSet.Options.Count == 0)
				return false;


			return true;
		}

		bool ICodeWriterFilterService.GenerateRelationship(RelationshipMetadataBase relationshipMetadata, EntityMetadata otherEntityMetadata, IServiceProvider services)
		{
			var filterService = (ICodeWriterFilterService)services.GetService(typeof(ICodeWriterFilterService));

			if (otherEntityMetadata == null)
				return false;

			// special case for calendar rules; we don't want them to be generated.
			if (String.Equals(otherEntityMetadata.LogicalName, "calendarrule", StringComparison.Ordinal))
				return false;

			return filterService.GenerateEntity(otherEntityMetadata, services);
		}

		bool ICodeWriterFilterService.GenerateServiceContext(IServiceProvider services)
		{
			return _generateServiceContext;
		}

		bool ICodeWriterMessageFilterService.GenerateSdkMessage(SdkMessage message, IServiceProvider services)
		{
			if (!_builderInvokeParameters.GenerateSdkMessages)
			{
				return false;
			}
            
            if (!_builderInvokeParameters.Private && message.IsPrivate)
            	return false;


            if (message.SdkMessageFilters.Count == 0)
				return false;

            var s = System.Reflection.Assembly.GetAssembly(typeof(Microsoft.Xrm.Sdk.AliasedValue)).GetTypes().Where(w => w.FullName.StartsWith($"Microsoft.Xrm.Sdk.Messages.{message.Name}", StringComparison.OrdinalIgnoreCase));
            if (s.Any())
            {
                return false; // do not generate messages for reserved namespace
            }

            s = System.Reflection.Assembly.GetAssembly(typeof(Microsoft.Crm.Sdk.SdkMessageAvailability)).GetTypes().Where(w => w.FullName.StartsWith($"Microsoft.Crm.Sdk.Messages.{message.Name}", StringComparison.OrdinalIgnoreCase));
            if (s.Any())
            {
                return false; // do not generate messages for reserved namespace
            }

            return true;
		}

		bool ICodeWriterMessageFilterService.GenerateSdkMessagePair(SdkMessagePair messagePair, IServiceProvider services)
		{
			if (!_builderInvokeParameters.GenerateSdkMessages)
			{
				return false;
			}

			if (_builderInvokeParameters.GenerateSdkMessages && (!_builderInvokeParameters.Private && !messagePair.Message.IsCustomAction))
			{
				return false;
			}

            var s = System.Reflection.Assembly.GetAssembly(typeof(Microsoft.Xrm.Sdk.AliasedValue)).GetTypes().Where(w => w.FullName.StartsWith($"Microsoft.Xrm.Sdk.Messages.{messagePair.Message.Name}", StringComparison.OrdinalIgnoreCase));
			if (s.Any())
			{
				return false; // do not generate messages for reserved namespace
			}

            s = System.Reflection.Assembly.GetAssembly(typeof(Microsoft.Crm.Sdk.SdkMessageAvailability)).GetTypes().Where(w => w.FullName.StartsWith($"Microsoft.Crm.Sdk.Messages.{messagePair.Message.Name}", StringComparison.OrdinalIgnoreCase));
            if (s.Any())
            {
                return false; // do not generate messages for reserved namespace
            }

            if (String.IsNullOrEmpty(_builderInvokeParameters.MessageNamespace))
				return true;

			return String.Equals(_builderInvokeParameters.MessageNamespace, messagePair.MessageNamespace, StringComparison.OrdinalIgnoreCase);
		}
		#endregion
	}
}
