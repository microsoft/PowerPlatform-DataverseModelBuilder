using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;

namespace Microsoft.PowerPlatform.Dataverse.ModelBuilderLib
{
	/// <summary>
	/// Used by the ICodeGenerationService to retrieve names for the CodeDOM objects being created.
	/// </summary>
	public interface INamingService
	{
		/// <summary>
		/// Returns a name for the OptionSet being generated.
		/// </summary>
		string GetNameForOptionSet(EntityMetadata entityMetadata, OptionSetMetadataBase optionSetMetadata, IServiceProvider services);
		/// <summary>
		/// Retrieves a name for the Option being generated.
		/// </summary>
		string GetNameForOption(OptionSetMetadataBase optionSetMetadata, OptionMetadata optionMetadata, IServiceProvider services);
		/// <summary>
		/// Retrieves a name for the Entity being generated.
		/// </summary>
		string GetNameForEntity(EntityMetadata entityMetadata, IServiceProvider services);
		/// <summary>
		/// Retrieves a name for the Attribute being generated.
		/// </summary>
		string GetNameForAttribute(EntityMetadata entityMetadata, AttributeMetadata attributeMetadata, IServiceProvider services);
        /// <summary>
        /// Retrieves a name for the 1:N, N:N, or N:1 relationship being generated.
        /// </summary>
        string GetNameForRelationship(EntityMetadata entityMetadata, RelationshipMetadataBase relationshipMetadata, EntityRole? reflexiveRole, IServiceProvider services);
		/// <summary>
		/// Retrieves a name for the data context being generated.
		/// </summary>
		string GetNameForServiceContext(IServiceProvider services);
		/// <summary>
		/// Retrieves a name for a set of entities.
		/// </summary>
		string GetNameForEntitySet(EntityMetadata entityMetadata, IServiceProvider services);
		/// <summary>
		/// Retrieves a name for the MessagePair being generated.
		/// </summary>
		string GetNameForMessagePair(SdkMessagePair messagePair, IServiceProvider services);
		/// <summary>
		/// Retrieves a name for the Request Field being generated.
		/// </summary>
		string GetNameForRequestField(SdkMessageRequest request, SdkMessageRequestField requestField, IServiceProvider services);
		/// <summary>
		/// Retrieves a name for the Response Field being generated.
		/// </summary>
		string GetNameForResponseField(SdkMessageResponse response, SdkMessageResponseField responseField, IServiceProvider services);
	}
}
