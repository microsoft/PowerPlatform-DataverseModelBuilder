using Microsoft.Xrm.Sdk.Metadata;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.PowerPlatform.Dataverse.ModelBuilderLib
{
    /// <summary>
    /// Interface for IOrganization metadata
    /// </summary>
	public interface IOrganizationMetadata
	{
		/// <summary>
		/// Array of complete EntityMetadata for the Organization.
		/// </summary>
		[SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
		EntityMetadata[] Entities { get; }
		/// <summary>
		/// Array of complete OptionSetMetadata for the Organization.
		/// </summary>
		[SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
		OptionSetMetadataBase[] OptionSets { get; }
		/// <summary>
		/// All SdkMessages for the Organization.
		/// </summary>
		SdkMessages Messages { get; }
	}
}
