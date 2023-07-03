using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.PowerPlatform.Dataverse.ModelBuilderLib
{
	/// <summary>
	/// An SDK Message
	/// </summary>
	[DebuggerDisplay("{Name} cust:{IsCustomAction} prv:{IsPrivate} pairs:{SdkMessagePairs?.Count} filters:{SdkMessageFilters?.Count}")]
    public sealed class SdkMessage
	{
		#region Fields
		private string _name;
		private Guid _id;
		private bool _isPrivate;
		private bool _isCustomAction;
		private Dictionary<Guid, SdkMessagePair> _sdkMessagePairs;
		private Dictionary<Guid, SdkMessageFilter> _sdkMessageFilters;
		#endregion

		#region Constructors
        /// <summary>
        /// Default constructor
        /// </summary>
        public SdkMessage(Guid id, string name, bool isPrivate)
			: this(id, name, isPrivate, 0)
		{
		}
		internal SdkMessage(Guid id, string name, bool isPrivate, byte customizationLevel)
		{
			this._id = id;
			this._isPrivate = isPrivate;
			this._name = name;
			this._isCustomAction = (customizationLevel != 0);
			this._sdkMessagePairs = new Dictionary<Guid, SdkMessagePair>();
			this._sdkMessageFilters = new Dictionary<Guid, SdkMessageFilter>();
		}
		#endregion

		#region Properties

        /// <summary>
        /// Gets the SDK message name
        /// </summary>
		public string Name
		{
			get
			{
				return this._name;
			}
		}

        /// <summary>
        /// Gets the SDK message id
        /// </summary>
        public Guid Id
		{
			get
			{
				return this._id;
			}
		}

        /// <summary>
        /// Gets whether the SDK message is private
        /// </summary>
		public bool IsPrivate
		{
			get
			{
				return this._isPrivate;
			}
		}

        /// <summary>
        /// Gets whether the SDK message is a custom action
        /// </summary>
		public bool IsCustomAction
		{
			get
			{
				return this._isCustomAction;
			}
		}

        /// <summary>
        /// Gets a dictionary of message pairs
        /// </summary>
		public Dictionary<Guid, SdkMessagePair> SdkMessagePairs
		{
			get
			{
				return this._sdkMessagePairs;
			}
		}

        /// <summary>
        /// Gets a dictionary of message filters
        /// </summary>
		public Dictionary<Guid, SdkMessageFilter> SdkMessageFilters
		{
			get
			{
				return this._sdkMessageFilters;
			}
		}
        #endregion

        #region Methods
        /// <summary>
        /// Fills an SDK message from a given result
        /// </summary>
        internal void Fill(Result result)
		{
			SdkMessagePair messagePair = null;
			if (result.SdkMessagePairId != Guid.Empty)
			{
				if (!this.SdkMessagePairs.ContainsKey(result.SdkMessagePairId))
				{
					messagePair = new SdkMessagePair(this, result.SdkMessagePairId, result.SdkMessagePairNamespace);
					this._sdkMessagePairs.Add(messagePair.Id, messagePair);
				}

				messagePair = this.SdkMessagePairs[result.SdkMessagePairId];
				messagePair.Fill(result);
			}

			SdkMessageFilter messageFilter = null;
			if (result.SdkMessageFilterId != Guid.Empty)
			{
				if (!this.SdkMessageFilters.ContainsKey(result.SdkMessageFilterId))
				{
					messageFilter = new SdkMessageFilter(result.SdkMessageFilterId);
					this.SdkMessageFilters.Add(result.SdkMessageFilterId, messageFilter);
				}

				messageFilter = this.SdkMessageFilters[result.SdkMessageFilterId];
				messageFilter.Fill(result);
			}
		}
		#endregion
	}
}
