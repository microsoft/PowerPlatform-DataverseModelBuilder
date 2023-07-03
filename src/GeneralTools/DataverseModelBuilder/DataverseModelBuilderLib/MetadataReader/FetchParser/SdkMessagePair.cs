using System;
using System.Diagnostics;

namespace Microsoft.PowerPlatform.Dataverse.ModelBuilderLib
{
    /// <summary>
    /// An SDK message pair
    /// </summary>
	public sealed class SdkMessagePair
	{
		#region Fields
		private Guid _id;
		private string _messageNamespace;
		private SdkMessage _message;
		private SdkMessageRequest _sdkMessageRequest;
		private SdkMessageResponse _sdkMessageResponse;
		#endregion

		#region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="message">SDK message</param>
        /// <param name="id">Message pair id</param>
        /// <param name="messageNamespace">Message namespace</param>
		public SdkMessagePair(SdkMessage message, Guid id, string messageNamespace)
		{
			this._message = message;
			this._id = id;
			this._messageNamespace = messageNamespace;
		}
		#endregion

		#region Properties
        /// <summary>
        /// Gets the message pair id
        /// </summary>
		public Guid Id
		{
			get
			{
				return this._id;
			}
		}

        /// <summary>
        /// Gets the message namespace
        /// </summary>
        public string MessageNamespace
		{
			get
			{
				return this._messageNamespace;
			}
		}

        /// <summary>
        /// Gets or sets the message
        /// </summary>
        public SdkMessage Message
		{
			get
			{
				return this._message;
			}

			set
			{
				this._message = value;
			}
		}

        /// <summary>
        /// Gets or sets the message request
        /// </summary>
        public SdkMessageRequest Request
		{
			get
			{
				return this._sdkMessageRequest;
			}
			set
			{
				this._sdkMessageRequest = value;
			}
		}

        /// <summary>
        /// Gets or sets the message response
        /// </summary>
        public SdkMessageResponse Response
		{
			get
			{
				return this._sdkMessageResponse;
			}
			set
			{
				this._sdkMessageResponse = value;
			}
		}
		#endregion

		#region Methods
		internal void Fill(Result result)
		{
			if (result.SdkMessageRequestId != Guid.Empty)
			{
				if (this.Request == null)
				{
					this.Request = new SdkMessageRequest(this, result.SdkMessageRequestId, result.SdkMessageRequestName);
				}

				this.Request.Fill(result);
			}

			if (result.SdkMessageResponseId != Guid.Empty)
			{
				if (this.Response == null)
				{
					this.Response = new SdkMessageResponse(result.SdkMessageResponseId);
				}

				this.Response.Fill(result);
			}

		}
		#endregion
	}
}
