using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.PowerPlatform.Dataverse.ModelBuilderLib
{
    /// <summary>
    /// An SDK message request
    /// </summary>
    public sealed class SdkMessageRequest
	{
		#region Fields
		private Guid _id;
		private SdkMessagePair _messagePair;
		private string _name;
		private Dictionary<int, SdkMessageRequestField> _requestFields;
		#endregion

		#region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="message">SDK Message</param>
        /// <param name="id">Message request id</param>
        /// <param name="name">Message request name</param>
		public SdkMessageRequest(SdkMessagePair message, Guid id, string name)
		{
			this._id = id;
			this._name = name;
			this._messagePair = message;
			this._requestFields = new Dictionary<int, SdkMessageRequestField>();
		}
		#endregion

		#region Properties

        /// <summary>
        /// Gets the message request id
        /// </summary>
		public Guid Id
		{
			get
			{
				return this._id;
			}
		}

        /// <summary>
        /// Gets the message pair of the request
        /// </summary>
        public SdkMessagePair MessagePair
		{
			get
			{
				return this._messagePair;
			}
		}

        /// <summary>
        /// Gets the message request name
        /// </summary>
        public string Name
		{
			get
			{
				return this._name;
			}
		}

        /// <summary>
        /// Gets a dictionary of message request fields
        /// </summary>
        public Dictionary<int, SdkMessageRequestField> RequestFields
		{
			get
			{
				return this._requestFields;
			}
		}
		#endregion

		#region Methods
		internal void Fill(Result result)
		{
			if (!result.SdkMessageRequestFieldPosition.HasValue)
				return;

			if (!this.RequestFields.ContainsKey(result.SdkMessageRequestFieldPosition.Value))
			{
				SdkMessageRequestField field = new SdkMessageRequestField(
					this,
					result.SdkMessageRequestFieldPosition.Value, result.SdkMessageRequestFieldName,
					result.SdkMessageRequestFieldClrParser, result.SdkMessageRequestFieldIsOptional);
				this.RequestFields.Add(result.SdkMessageRequestFieldPosition.Value, field);
			}

			SdkMessageRequestField f = this.RequestFields[result.SdkMessageRequestFieldPosition.Value];
		}
		#endregion
	}
}
