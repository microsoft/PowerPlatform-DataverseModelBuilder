using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xrm.Sdk.Metadata;
using System.CodeDom;

namespace Microsoft.PowerPlatform.Dataverse.ModelBuilderLib
{
	/// <summary>
	/// Used by the ICodeGenerationService to retrieve types for the CodeDOM objects being created.
	/// </summary>
	internal interface ITypeMappingService
	{
		/// <summary>
		/// Retrieves a CodeTypeReference for the entity set being generated.
		/// </summary>
		CodeTypeReference GetTypeForEntity(EntityMetadata entityMetadata, IServiceProvider services);
		/// <summary>
		/// Retrieves a CodeTypeReference for the attribute being generated.
		/// </summary>
		CodeTypeReference GetTypeForAttributeType(EntityMetadata entityMetadata, AttributeMetadata attributeMetadata, IServiceProvider services);
		/// <summary>
		/// Retrieves a CodeTypeReference for the 1:N, N:N, or N:1 relationship being generated.
		/// </summary>
		CodeTypeReference GetTypeForRelationship(RelationshipMetadataBase relationshipMetadata, EntityMetadata otherEntityMetadata, IServiceProvider services);
		/// <summary>
		/// Retrieves a CodeTypeReference for the Request Field being generated.
		/// </summary>
		CodeTypeReference GetTypeForRequestField(SdkMessageRequestField requestField, IServiceProvider services);
		/// <summary>
		/// Retrieves a CodeTypeReference for the Response Field being generated.
		/// </summary>
		CodeTypeReference GetTypeForResponseField(SdkMessageResponseField responseField, IServiceProvider services);
	}
}
