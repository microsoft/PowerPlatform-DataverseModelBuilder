using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xrm.Sdk.Metadata;

namespace Microsoft.PowerPlatform.Dataverse.ModelBuilderLib
{
	/// <summary>
	/// Interface that can be used to filter out specific pieces of metadata from having code generated for it.
	/// </summary>
	public interface ICodeWriterFilterService
	{
		/// <summary>
		/// Returns true to generate code for the OptionSet and false otherwise.
		/// </summary>
		bool GenerateOptionSet(OptionSetMetadataBase optionSetMetadata, IServiceProvider services);
		/// <summary>
		/// Returns true to generate code for the Option and false otherwise.
		/// </summary>
		bool GenerateOption(OptionMetadata optionMetadata, IServiceProvider services);
		/// <summary>
		/// Returns true to generate code for the Entity and false otherwise.
		/// </summary>
		bool GenerateEntity(EntityMetadata entityMetadata, IServiceProvider services);
		/// <summary>
		/// Returns true to generate code for the Attribute and false otherwise.
		/// </summary>
		bool GenerateAttribute(AttributeMetadata attributeMetadata, IServiceProvider services);
		/// <summary>
		/// Returns true to generate code for the 1:N, N:N, or N:1 relationship and false otherwise.
		/// </summary>
		bool GenerateRelationship(RelationshipMetadataBase relationshipMetadata, EntityMetadata otherEntityMetadata, IServiceProvider services);
		/// <summary>
		/// Returns true to generate code for the data context and false otherwise.
		/// </summary>
		bool GenerateServiceContext(IServiceProvider services);

	}

    /// <summary>
    /// Interface for code writer message filter service
    /// </summary>
	[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
	public interface ICodeWriterMessageFilterService
	{
		/// <summary>
		/// Returns true to generate code for the SDK Message and false otherwise.
		/// </summary>
		bool GenerateSdkMessage(SdkMessage sdkMessage, IServiceProvider services);
		/// <summary>
		/// Returns true to generate code for the SDK Message Pair and false otherwise.
		/// </summary>
		bool GenerateSdkMessagePair(SdkMessagePair sdkMessagePair, IServiceProvider services);
	}
}
