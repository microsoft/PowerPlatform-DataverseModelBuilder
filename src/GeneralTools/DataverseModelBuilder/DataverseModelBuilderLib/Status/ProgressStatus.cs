using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PowerPlatform.Dataverse.ModelBuilderLib.Status
{
    public class ProgressStatus : EventArgs
    {
        public ProgressType StatusType { get; set; }
        /// <summary>
        /// indicates if the message should be indented. 
        /// </summary>
        public bool Indent { get; set; }
        /// <summary>
        /// Gets or Sets message status.
        /// </summary>
        public string StatusMessage { get; set; }
        /// <summary>
        /// Gets or Sets Progress stage.
        /// </summary>
        public ProcessStage Stage { get; set; }
    }
}
