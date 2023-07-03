using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PowerPlatform.Dataverse.ModelBuilderLib
{
    /// <summary>
    /// Extends the OrganizationServiceMetadata interface to allow for adding optionsets. 
    /// </summary>
    public interface IOrganizationMetadata2 :IOrganizationMetadata
    {
        /// <summary>
        /// Adds an OptionSet Metadata Object to the Global OptionSets array 
        /// </summary>
        /// <param name="optionSet"></param>
        void AddOptionSetInfo(OptionSetMetadata optionSet);

    }
}
