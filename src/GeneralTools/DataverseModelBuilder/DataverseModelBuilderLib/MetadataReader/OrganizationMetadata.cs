using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xrm.Sdk.Metadata;
using System.Diagnostics;
using System.Reflection;
using System.Linq;

namespace Microsoft.PowerPlatform.Dataverse.ModelBuilderLib
{
	internal sealed class OrganizationMetadata : IOrganizationMetadata2
	{
		#region Fields
		private readonly EntityMetadata[] _entities;
		private readonly List<OptionSetMetadataBase> _optionSets;
		private readonly SdkMessages _sdkMessages;
		private ModeBuilderLoggerService _modeBuilderLoggerService;
		#endregion

		#region Constructors
		internal OrganizationMetadata(EntityMetadata[] entities, OptionSetMetadataBase[] optionSets, SdkMessages messages, ModeBuilderLoggerService modeBuilderLoggerService)
		{
			_modeBuilderLoggerService = modeBuilderLoggerService;
			modeBuilderLoggerService.TraceMethodStart();

			_entities = entities;
			_optionSets = optionSets == null? new List<OptionSetMetadataBase>() : optionSets.ToList();
			_sdkMessages = messages;

			modeBuilderLoggerService.TraceMethodStop();
		}
		#endregion

		#region IOrganizationMetadata Members

		EntityMetadata[] IOrganizationMetadata.Entities
		{
			get
			{
				return this._entities;
			}
		}

		OptionSetMetadataBase[] IOrganizationMetadata.OptionSets
		{
			get
			{
				return this._optionSets.ToArray();
			}
		}

		SdkMessages IOrganizationMetadata.Messages
		{
			get
			{
				return this._sdkMessages;
			}
		}

		#endregion

		#region IOrganizationMetadata2 Members

		/// <summary>
		/// Add OptionSet to OptionSets list. 
		/// </summary>
		/// <param name="optionSet"></param>
		public void AddOptionSetInfo(OptionSetMetadata optionSet)
		{
			if (_optionSets.Any(a => a.Name.Equals(optionSet.Name)))
				return; // skipping. 
			_optionSets.Add(optionSet); 
		}


		#endregion
	}
}
