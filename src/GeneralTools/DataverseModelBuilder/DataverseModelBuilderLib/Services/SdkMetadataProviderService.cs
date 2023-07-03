using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.ServiceModel.Description;
using System.Xml.Linq;

using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Metadata;
using System.Text;
using System.Data.Common;
using System.Diagnostics;
using Microsoft.Xrm.Sdk.Query;
using System.Linq;
using Microsoft.PowerPlatform.Dataverse.ModelBuilderLib.Utility;

namespace Microsoft.PowerPlatform.Dataverse.ModelBuilderLib
{
	internal sealed class SdkMetadataProviderService : IMetadataProviderService
	{
		#region Fields
		private readonly ModelBuilderInvokeParameters _parameters;
		private IOrganizationMetadata _organizationMetadata;
        private IOrganizationService _orgSvc = null;
		#endregion

		#region Constructors
		internal SdkMetadataProviderService(ModelBuilderInvokeParameters parameters)
		{
			this._parameters = parameters;
		}
		#endregion

		#region Properties
		private ModelBuilderInvokeParameters Parameters
		{
			get
			{
				return this._parameters;
			}
		}

        /// <summary>
        /// Set the IOrganization Service interface.
        /// </summary>
        public IOrganizationService ServiceConnection { get => _orgSvc; set { _orgSvc = value; } }

		/// <summary>
		/// This provider requires a Live connection to Dataverse.
		/// </summary>
        public bool IsLiveConnectionRequired => true;
        #endregion

        #region IMetadataProvider Members

        public IOrganizationMetadata LoadMetadata(IServiceProvider service)
        {
            if (this._organizationMetadata == null)
            {
                ServiceProvider services = null;
                if (service != null)
                    services = (ServiceProvider)service;
                else
                    return null;

                IOrganizationService organizationService = ServiceConnection;
                if (organizationService == null)
                    throw new Exception("Connection to Dataverse is not established. Aborting process.");
                _parameters.Logger.WriteConsole($"Begin Reading Metadata from Server", true, Status.ProcessStage.ReadMetadata);
				Stopwatch readerSw = Stopwatch.StartNew();
                Stopwatch operationSw = Stopwatch.StartNew();

                EntityMetadata[] entityMetadata = services.MetadataProviderQueryServcie.RetrieveEntities(organizationService);
                _parameters.Logger.WriteConsole($"Read {entityMetadata.Length} Entities - {operationSw.Elapsed.ToDurationString()}", true, Status.ProcessStage.ReadMetadata);
				
				operationSw.Restart();
                OptionSetMetadataBase[] optionSetMetadata = services.MetadataProviderQueryServcie.RetrieveOptionSets(organizationService);
                _parameters.Logger.WriteConsole($"Read {optionSetMetadata.Length} Global OptionSets - {operationSw.Elapsed.ToDurationString()}", true, Status.ProcessStage.ReadMetadata);

                operationSw.Restart();
                SdkMessages messages = services.MetadataProviderQueryServcie.RetrieveSdkRequests(organizationService);
                _parameters.Logger.WriteConsole($"Read {messages.MessageCollection.Count} SDK Messages - {operationSw.Elapsed.ToDurationString()}", true, Status.ProcessStage.ReadMetadata);
				operationSw.Stop(); 

                // Get System default language.
                _parameters.SystemDefaultLanguageId ??= GetSystemDefaultLanguageCode(organizationService);
                readerSw.Stop();
                // Write Status
                _parameters.Logger.WriteConsole($"Completed Reading Metadata from Server - {readerSw.Elapsed.ToDurationString()}", true, Status.ProcessStage.ReadMetadata);

                this._organizationMetadata = this.CreateOrganizationMetadata(entityMetadata, optionSetMetadata, messages);
            }
            return this._organizationMetadata;
        }

		/// <summary>
		/// Get and return the system default language code. 
		/// </summary>
		/// <param name="service"></param>
		/// <returns></returns>
        private int? GetSystemDefaultLanguageCode(IOrganizationService service)
        {
            // Get org system language 
            var systemLanguageFetch = $@"<?xml version='1.0' encoding='utf-16'?><fetch top='1' no-lock='true'><entity name='organization'><attribute name='languagecode' /></entity></fetch>";
			var queryRslt = service.RetrieveMultiple(new FetchExpression(systemLanguageFetch)); 
			if (queryRslt != null && queryRslt.Entities.Any())
			{
				// Get the first ( which must be there b/c the any returned true ) and return the LCID
				queryRslt.Entities[0].TryGetAttributeValue<int>("languagecode", out int iLcid);
				if (iLcid > 1000) {
					return iLcid; 
				}
			}
            return null; 
        }

        private EntityMetadata[] RetrieveEntities(IOrganizationService service)
		{
			OrganizationRequest request = new OrganizationRequest("RetrieveAllEntities");
			request.Parameters["EntityFilters"] = EntityFilters.Entity | EntityFilters.Attributes | EntityFilters.Relationships;
			request.Parameters["RetrieveAsIfPublished"] = false;

			OrganizationResponse response = service.Execute(request);
			return (EntityMetadata[])response.Results["EntityMetadata"];
		}

		private OptionSetMetadataBase[] RetrieveOptionSets(IOrganizationService service)
		{
			OrganizationRequest request = new OrganizationRequest("RetrieveAllOptionSets");
			request.Parameters["RetrieveAsIfPublished"] = true;

			OrganizationResponse response = service.Execute(request);
			return (OptionSetMetadataBase[])response.Results["OptionSetMetadata"];
		}

		// Microsoft CRM 201702 - Added OrderExpression on Link-Entity SdkMessageRequestField to force
		// old style pagination where we do not set top clause and retrieve all rows and discard on the server based on the page #
		// requested. The actual fix will be to fix pagination to handle the paging cookie correctly when we have link entities
		// where the entity's primary key will not be unique. In the Fetch below, SdkMessageRequestId will not be unique across rows.
		// Without the old style paging - current paging cookie logic will be based on entity primary key which is SdkMessageId which is not
		// unique and there is a data loss where the data spanning across pages is lost since the second round trip to the server to get
		// more rows will try to get rows where SdkMessageId > last SdkMessageId.
		private SdkMessages RetrieveSdkRequests(IOrganizationService service)
		{
			string fetchQuery = @"<fetch distinct='true' version='1.0'>
	<entity name='sdkmessage'>
		<attribute name='name'/>
		<attribute name='isprivate'/>
		<attribute name='sdkmessageid'/>
		<attribute name='customizationlevel'/>
		<link-entity name='sdkmessagepair' alias='sdkmessagepair' to='sdkmessageid' from='sdkmessageid' link-type='inner'>
			<filter>
				<condition alias='sdkmessagepair' attribute='endpoint' operator='eq' value='2011/Organization.svc' />
			</filter>
			<attribute name='sdkmessagepairid'/>
			<attribute name='namespace'/>
			<link-entity name='sdkmessagerequest' alias='sdkmessagerequest' to='sdkmessagepairid' from='sdkmessagepairid' link-type='outer'>
				<attribute name='sdkmessagerequestid'/>
				<attribute name='name'/>
				<link-entity name='sdkmessagerequestfield' alias='sdkmessagerequestfield' to='sdkmessagerequestid' from='sdkmessagerequestid' link-type='outer'>
					<attribute name='name'/>
					<attribute name='optional'/>
					<attribute name='position'/>
					<attribute name='publicname'/>
					<attribute name='clrparser'/>
					<order attribute='sdkmessagerequestfieldid' descending='false' />
				</link-entity>
				<link-entity name='sdkmessageresponse' alias='sdkmessageresponse' to='sdkmessagerequestid' from='sdkmessagerequestid' link-type='outer'>
					<attribute name='sdkmessageresponseid'/>
					<link-entity name='sdkmessageresponsefield' alias='sdkmessageresponsefield' to='sdkmessageresponseid' from='sdkmessageresponseid' link-type='outer'>
						<attribute name='publicname'/>
						<attribute name='value'/>
						<attribute name='clrformatter'/>
						<attribute name='name'/>
						<attribute name='position' />
					</link-entity>
				</link-entity>
			</link-entity>
		</link-entity>
		<link-entity name='sdkmessagefilter' alias='sdmessagefilter' to='sdkmessageid' from='sdkmessageid' link-type='inner'>
			<filter>
				<condition alias='sdmessagefilter' attribute='isvisible' operator='eq' value='1' />
			</filter>
			<attribute name='sdkmessagefilterid'/>
			<attribute name='primaryobjecttypecode'/>
			<attribute name='secondaryobjecttypecode'/>
		</link-entity>
		<order attribute='sdkmessageid' descending='false' />
	 </entity>
</fetch>";

			MessagePagingInfo pagingInfo = null;
			int currentPage = 1;
			SdkMessages messages = new SdkMessages(null);
			OrganizationRequest request = new OrganizationRequest("ExecuteFetch");

			while (pagingInfo == null || pagingInfo.HasMoreRecords)
			{
				string currentQuery = fetchQuery;
				if (pagingInfo != null)
					currentQuery = SetPagingCookie(fetchQuery, pagingInfo.PagingCookig, currentPage);

				request.Parameters["FetchXml"] = currentQuery;
				OrganizationResponse response = service.Execute(request);
				pagingInfo = SdkMessages.FromFetchResult(messages, (string)response.Results["FetchXmlResult"]);
				currentPage++;
			}

			return messages;
		}
		#endregion  IMetadataProvider Members

		#region Helper Methods
		private string SetPagingCookie(string fetchQuery, string pagingCookie, int pageNumber)
		{
			XDocument doc = XDocument.Parse(fetchQuery);
			if (pagingCookie != null)
			{
				doc.Root.Add(new XAttribute(XName.Get("paging-cookie"), pagingCookie));
			}
			doc.Root.Add(new XAttribute(XName.Get("page"), pageNumber.ToString(CultureInfo.InvariantCulture)));
			return doc.ToString();
		}

        //private ClientCredentials CreateDeviceCredentials(IServiceConfiguration<IOrganizationService> orgServiceConfig)
        //{
        //    if (orgServiceConfig.AuthenticationType != AuthenticationProviderType.LiveId)
        //    {
        //        return null;
        //    }

        //    if (!String.IsNullOrEmpty(this.Parameters.DeviceID) && !String.IsNullOrEmpty(this.Parameters.DevicePassword))
        //    {
        //        ClientCredentials clientCredentials = new ClientCredentials();

        //        clientCredentials.UserName.UserName = this.Parameters.DeviceID;
        //        clientCredentials.UserName.Password = this.Parameters.DevicePassword;

        //        return clientCredentials;
        //    }
        //    else
        //    {
        //        return DeviceIdManager.LoadOrRegisterDevice(StaticUtils.ApplicationId, orgServiceConfig.CurrentIssuer.IssuerAddress.Uri);
        //    }
        //}

        //private ClientCredentials CreateCredentials(IServiceConfiguration<IOrganizationService> orgServiceConfig)
        //{
        //    ClientCredentials clientCredentials = new ClientCredentials();

        //    // Setup the client credentials for the calling user.  Note that the null vs. String.Empty values here
        //    // for parameters not supplied is important as the .NET Framework treats the values very specifically to
        //    // determine if it should use the current calling credentials or not.

        //    if (ShouldUseWindowsCredentials(orgServiceConfig))
        //    {
        //        clientCredentials.Windows.ClientCredential = new System.Net.NetworkCredential();
        //        clientCredentials.Windows.ClientCredential.UserName = GetValueOrDefault(this.Parameters.UserName, null);
        //        clientCredentials.Windows.ClientCredential.Password = GetValueOrDefault(this.Parameters.Password, String.Empty);
        //        clientCredentials.Windows.ClientCredential.Domain = GetValueOrDefault(this.Parameters.Domain, null);
        //    }
        //    else
        //    {
        //        clientCredentials.UserName.UserName = GetValueOrDefault(this.Parameters.UserName, null);
        //        clientCredentials.UserName.Password = GetValueOrDefault(this.Parameters.Password, null);
        //    }

        //    return clientCredentials;
        //}

        //private bool ShouldUseWindowsCredentials(IServiceConfiguration<IOrganizationService> orgServiceConfig)
        //{
        //    //OSDP
        //    if (orgServiceConfig.AuthenticationType == AuthenticationProviderType.OnlineFederation &&
        //        !String.IsNullOrEmpty(this.Parameters.UserName))
        //        return false;

        //    //IFD - CRM SE 20175
        //    if (orgServiceConfig.IssuerEndpoints != null
        //        && orgServiceConfig.IssuerEndpoints.ContainsKey(TokenServiceCredentialType.Username.ToString()))
        //        return false;

        //    return true;
        //}

		private IOrganizationMetadata CreateOrganizationMetadata(EntityMetadata[] entityMetadata, OptionSetMetadataBase[] optionSetMetadata, SdkMessages messages)
		{
			return new OrganizationMetadata(entityMetadata, optionSetMetadata, messages, _parameters.Logger);
		}


       	private static string GetValueOrDefault(string value, string defaultValue)
		{
			if (String.IsNullOrWhiteSpace(value))
				return defaultValue;
			else
				return value;
		}

        /// <summary>
        /// Builds a connection string from the passed in parameters.
        /// </summary>
        /// <returns></returns>
        private string GetConnectionString()
        {
            // If the user provided a connection string.. try to connect using that.
            if (!string.IsNullOrEmpty(Parameters.ConnectionString))
                return Parameters.ConnectionString;

            // else  Build from parts.

            // Validate URl
            Uri serviceUri = null;
            if (!Uri.TryCreate(Parameters.Url, UriKind.RelativeOrAbsolute, out serviceUri))
            {
                throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, "Cannot connect to organization service at {0}", this.Parameters.Url));
            }

            StringBuilder connectionStringBld = new StringBuilder();

            DbConnectionStringBuilder.AppendKeyValuePair(connectionStringBld, "Server", serviceUri.ToString());
            DbConnectionStringBuilder.AppendKeyValuePair(connectionStringBld, "UserName", Parameters.UserName);
            DbConnectionStringBuilder.AppendKeyValuePair(connectionStringBld, "Password", Parameters.Password);
            if (!string.IsNullOrEmpty(Parameters.Domain))
            {
                DbConnectionStringBuilder.AppendKeyValuePair(connectionStringBld, "Domain", Parameters.Domain);
                DbConnectionStringBuilder.AppendKeyValuePair(connectionStringBld, "AuthType", "AD");
            }
            else
            {
                // Default to oAuth.
                DbConnectionStringBuilder.AppendKeyValuePair(connectionStringBld, "AuthType", "OAuth");
                DbConnectionStringBuilder.AppendKeyValuePair(connectionStringBld, "ClientId", "2ad88395-b77d-4561-9441-d0e40824f9bc");
                DbConnectionStringBuilder.AppendKeyValuePair(connectionStringBld, "RedirectUri", "app://5d3e90d6-aa8e-48a8-8f2c-58b45cc67315");
                DbConnectionStringBuilder.AppendKeyValuePair(connectionStringBld, "LoginPrompt", "Never");
            }

            return connectionStringBld.ToString();
        }

		#endregion
	}

}
