using Microsoft.Xrm.Sdk;
using System;

namespace Microsoft.PowerPlatform.Dataverse.ModelBuilderLib
{
    /// <summary>
    /// Interface that provides metadata for a given organization.
    /// </summary>
    public interface IMetadataProviderService
	{
		/// <summary>
		/// Returns the metadata for a given organization.  Subsequent calls to the method should
		/// return the same set of information on the IOrganizationMetadata object.
		/// </summary>
		IOrganizationMetadata LoadMetadata(IServiceProvider service);

		/// <summary>
		/// Get or set the IOrganization Service to use to make the connection.
		/// </summary>
		IOrganizationService ServiceConnection { get; set; }

		/// <summary>
		/// If set to true, the host program is expected to set the ServiceConnection before calling ProcessModelInvoker
		/// </summary>
		bool IsLiveConnectionRequired { get; }
	}
}
