using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.PowerPlatform.Dataverse.ModelBuilderLib
{
    /// <summary>
    /// An SDK message response
    /// </summary>
	public sealed class SdkMessageResponse
	{
		#region Fields
		private Guid _id;
		private Dictionary<int, SdkMessageResponseField> _responseFields;
		#endregion

		#region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="id">Message response id</param>
		public SdkMessageResponse(Guid id)
		{
			this._id = id;
			this._responseFields = new Dictionary<int, SdkMessageResponseField>();
		}
		#endregion

		#region Properties

        /// <summary>
        /// Gets the message response id
        /// </summary>
		public Guid Id
		{
			get
			{
				return this._id;
			}
		}

        /// <summary>
        /// Gets the message response fields
        /// </summary>
        public Dictionary<int, SdkMessageResponseField> ResponseFields
		{
			get
			{
				return this._responseFields;
			}
		}
		#endregion

		#region Methods
		internal void Fill(Result result)
		{
			if (!result.SdkMessageResponseFieldPosition.HasValue)
				return;

			if (!this.ResponseFields.ContainsKey(result.SdkMessageResponseFieldPosition.Value))
			{
				SdkMessageResponseField field = new SdkMessageResponseField(
					result.SdkMessageResponseFieldPosition.Value, result.SdkMessageResponseFieldName,
					result.SdkMessageResponseFieldClrFormatter, result.SdkMessageResponseFieldValue);
				this.ResponseFields.Add(result.SdkMessageResponseFieldPosition.Value, field);
			}

			SdkMessageResponseField f = this.ResponseFields[result.SdkMessageResponseFieldPosition.Value];
		}
		#endregion
	}
}
