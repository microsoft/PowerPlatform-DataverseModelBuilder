using Microsoft.Xrm.Sdk.Metadata;
using System;

namespace Microsoft.PowerPlatform.Dataverse.ModelBuilderLib
{
    /// <summary>
    /// Type of code to generate
    /// </summary>
	public enum CodeGenerationType
    {
        /// <summary>
        /// Type Class
        /// </summary>
		Class,
        /// <summary>
        /// Type Enum
        /// </summary>
		Enum,
        /// <summary>
        /// Type Field
        /// </summary>
		Field,
        /// <summary>
        /// Type Method
        /// </summary>
		Method,
        /// <summary>
        /// Type Property
        /// </summary>
		Property,
        /// <summary>
        /// Type Struct
        /// </summary>
		Struct,
        /// <summary>
        /// Type Parameter
        /// </summary>
		Parameter
    }

    /// <summary>
    /// Interface that provides the ability to generate code based on organization metadata.
    /// </summary>
    public interface ICodeGenerationService
    {
        /// <summary>
        /// Writes code based on the organization metadata.
        /// </summary>
        /// <param name="organizationMetadata">Organization metadata to generate the code for.</param>
        /// <param name="language">Laguage to generate</param>
        /// <param name="outputFile">Output file to write the generated code to.</param>
        /// <param name="targetNamespace">Target namespace for the generated code.</param>
        /// <param name="services">ServiceProvider to query for additional services that can be used during code generation.</param>
        void Write(IOrganizationMetadata organizationMetadata, string language, string outputFile, string targetNamespace, IServiceProvider services);
        /// <summary>
        /// Returns the type that gets generated for the OptionSetMetadata
        /// </summary>
        CodeGenerationType GetTypeForOptionSet(EntityMetadata entityMetadata, OptionSetMetadataBase optionSetMetadata, IServiceProvider services);
        /// <summary>
        /// Returns the type that gets generated for the Option
        /// </summary>
        CodeGenerationType GetTypeForOption(OptionSetMetadataBase optionSetMetadata, OptionMetadata optionMetadata, IServiceProvider services);
        /// <summary>
        /// Returns the type that gets generated for the EntityMetadata
        /// </summary>
        CodeGenerationType GetTypeForEntity(EntityMetadata entityMetadata, IServiceProvider services);
        /// <summary>
        /// Returns the type that gets generated for the AttributeMetadata
        /// </summary>
        CodeGenerationType GetTypeForAttribute(EntityMetadata entityMetadata, AttributeMetadata attributeMetadata, IServiceProvider services);
        /// <summary>
        /// Returns the type that gets generated for the SdkMessagePair
        /// </summary>
        CodeGenerationType GetTypeForMessagePair(SdkMessagePair messagePair, IServiceProvider services);
        /// <summary>
        /// Returns the type that gets generated for the SdkMessageRequestField
        /// </summary>
        CodeGenerationType GetTypeForRequestField(SdkMessageRequest request, SdkMessageRequestField requestField, IServiceProvider services);
        /// <summary>
        /// Returns the type that gets generated for the SdkMessageResponseField
        /// </summary>
        CodeGenerationType GetTypeForResponseField(SdkMessageResponse response, SdkMessageResponseField responseField, IServiceProvider services);
    }
}
