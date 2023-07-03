using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PowerPlatform.Dataverse.ModelBuilderLib.Status
{
    public enum ProcessStage
    {
        Unknown =  0, 
        ReadMetadata = 1,
        ClassGeneration = 2,
        FileGeneration = 3,
        ParseParamaters = 4,
        Help = 5
    }
}
