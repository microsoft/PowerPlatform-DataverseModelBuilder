using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xrm.Sdk.Metadata;

namespace Microsoft.PowerPlatform.Dataverse.ModelBuilderLib
{
	internal sealed class CodeDomCustomizationService : ICustomizeCodeDomService
	{
		#region Constructors
		internal CodeDomCustomizationService()
		{
		}
		#endregion

		#region ICustomizeCodeDomService Members
		void ICustomizeCodeDomService.CustomizeCodeDom(System.CodeDom.CodeCompileUnit codeUnit, IServiceProvider services)
		{
			return;
		}
		#endregion
	}
}
