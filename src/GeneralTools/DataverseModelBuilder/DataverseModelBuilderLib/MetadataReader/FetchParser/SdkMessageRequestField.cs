using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.PowerPlatform.Dataverse.ModelBuilderLib
{
    /// <summary>
    /// An SDK message request field
    /// </summary>
	public sealed class SdkMessageRequestField
	{
		#region Fields
		private const string EntityTypeName = "Microsoft.Xrm.Sdk.Entity,Microsoft.Xrm.Sdk";

		private SdkMessageRequest _request;
		private int _index;
		private string _name;
		private string _clrFormatter;
		private bool _isOptional;
		#endregion

		#region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="request">SDK message request</param>
        /// <param name="index">Request field index</param>
        /// <param name="name">Request field name</param>
        /// <param name="clrFormatter">Request field CLR formatter</param>
        /// <param name="isOptional">Whether the request field is optional</param>
		public SdkMessageRequestField(SdkMessageRequest request, int index, string name, string clrFormatter, bool isOptional)
		{
			this._request = request;
			this._clrFormatter = clrFormatter;
			this._name = name;
			this._index = index;
			this._isOptional = isOptional;
		}
		#endregion

		#region Properties
        /// <summary>
        /// Gets the SDK message request
        /// </summary>
		public SdkMessageRequest Request
		{
			get
			{
				return this._request;
			}
		}

        /// <summary>
        /// Gets the message request field index
        /// </summary>
        public int Index
		{
			get
			{
				return this._index;
			}
		}

        /// <summary>
        /// Gets the message request field name
        /// </summary>
        public string Name
		{
			get
			{
				return this._name;
			}
		}

        /// <summary>
        /// Gets the message request field CLR formatter
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "CLR")]
		public string CLRFormatter
		{
			get
			{
				return this._clrFormatter;
			}
		}

        /// <summary>
        /// Gets whether the message field is optional
        /// </summary>
        public bool IsOptional
		{
			get
			{
				return this._isOptional;
			}
		}

        /// <summary>
        /// Gets whether the message request field is generic
        /// </summary>
        public bool IsGeneric
		{
			get
			{
				return String.Equals(this.CLRFormatter, EntityTypeName, StringComparison.Ordinal) &&
					this.Request.MessagePair.Message.SdkMessageFilters.Count > 1;
			}
		}
		#endregion
	}
}
