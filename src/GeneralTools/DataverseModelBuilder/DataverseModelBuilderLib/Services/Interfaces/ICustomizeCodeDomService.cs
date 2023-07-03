using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xrm.Sdk.Metadata;

namespace Microsoft.PowerPlatform.Dataverse.ModelBuilderLib
{
	/// <summary>
	/// Interface that can be used to customize the CodeDom before it generates code.
	/// </summary>
	public interface ICustomizeCodeDomService
	{
		/// <summary>
		/// Customize the generated types before code is generated
		/// </summary>
		void CustomizeCodeDom(System.CodeDom.CodeCompileUnit codeUnit, IServiceProvider services);
	}
}
