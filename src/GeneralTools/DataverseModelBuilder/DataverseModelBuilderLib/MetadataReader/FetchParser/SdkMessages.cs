using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml.Serialization;

namespace Microsoft.PowerPlatform.Dataverse.ModelBuilderLib
{
    /// <summary>
    /// SDK Messages
    /// </summary>
	public sealed class SdkMessages
	{
		#region Fields
		private Dictionary<Guid, SdkMessage> _messages;
		#endregion

		#region Constructors
		private SdkMessages()
			: this(null)
		{
		}

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="messageCollection">SDK message collection</param>
		public SdkMessages(Dictionary<Guid, SdkMessage> messageCollection)
		{
			this._messages = messageCollection ?? new Dictionary<Guid, SdkMessage>();
		}
		#endregion

		#region Properties

        /// <summary>
        /// Gets the message collection
        /// </summary>
		public Dictionary<Guid, SdkMessage> MessageCollection
		{
			get
			{
				return this._messages;
			}
		}
		#endregion

		#region Methods
		private void Fill(ResultSet resultSet)
		{
			if (resultSet.Results == null)
			{
				return;
			}

			foreach (Result result in resultSet.Results)
			{
				SdkMessage message = null;
				if ((result.SdkMessageId != Guid.Empty) && !this.MessageCollection.ContainsKey(result.SdkMessageId))
				{
					message = new SdkMessage(result.SdkMessageId, result.Name, result.IsPrivate, result.CustomizationLevel);
					this.MessageCollection.Add(result.SdkMessageId, message);
				}

				message = this.MessageCollection[result.SdkMessageId];
				message.Fill(result);
			}
		}

        /// <summary>
        /// Gets the MessagePagingInfo for a given collection SDK messages
        /// </summary>
        public static MessagePagingInfo FromFetchResult(SdkMessages messages, string xml)
		{
			ResultSet resultSet = null;
			using (System.IO.StringReader reader = new System.IO.StringReader(xml))
			{
				XmlSerializer serializer = new XmlSerializer(typeof(ResultSet), String.Empty);
				resultSet = serializer.Deserialize(reader) as ResultSet;
			}

			messages.Fill(resultSet);
			return MessagePagingInfo.FromResultSet(resultSet);
		}
		#endregion
	}

    /// <summary>
    /// Message paging info
    /// </summary>
	public sealed class MessagePagingInfo
	{
        /// <summary>
        /// Gets or sets the paging cookie
        /// </summary>
		public string PagingCookig { get; set; }

        /// <summary>
        /// Gets or sets whether the paging info has more records
        /// </summary>
		public bool HasMoreRecords { get; set; }

        /// <summary>
        /// Gets the message paging info from a set of message results
        /// </summary>
        public static MessagePagingInfo FromResultSet(ResultSet resultSet)
		{
			MessagePagingInfo info = new MessagePagingInfo();
			info.PagingCookig = resultSet.PagingCookie;
			info.HasMoreRecords = Convert.ToBoolean(resultSet.MoreRecords, CultureInfo.InvariantCulture);
			return info;
		}
	}
}
