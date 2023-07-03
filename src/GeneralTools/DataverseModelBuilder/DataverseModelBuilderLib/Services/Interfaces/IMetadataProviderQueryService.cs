using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;

namespace Microsoft.PowerPlatform.Dataverse.ModelBuilderLib
{
    /// <summary>
    /// Interface for metadata provider query service
    /// </summary>
    public interface IMetadataProviderQueryService
    {
        /// <summary>
        /// Retrieves entities for the given service
        /// </summary>
        /// <param name="service">Service to query</param>
        /// <returns>An EntityMetadata array</returns>
        EntityMetadata[] RetrieveEntities(IOrganizationService service);

        /// <summary>
        /// Retrieves option sets for the given service
        /// </summary>
        /// <param name="service">Service to query</param>
        /// <returns>An OptionSetMetadataBase array</returns>
        OptionSetMetadataBase[] RetrieveOptionSets(IOrganizationService service);

        /// <summary>
        /// Retrieves SDK requests for the given service
        /// </summary>
        /// <param name="service">Service to query</param>
        /// <returns>SdkMessages</returns>
        SdkMessages RetrieveSdkRequests(IOrganizationService service);
    }
}
