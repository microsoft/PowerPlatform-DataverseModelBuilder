using Microsoft.PowerPlatform.Dataverse.DataverseModelBuilder.ConnectionManagement.LoginControl;
using Microsoft.PowerPlatform.Dataverse.ModelBuilderLib;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Tooling.Connector;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PowerPlatform.Dataverse.DataverseModelBuilder.ConnectionManagement
{
    public class Connection
    {
        private ModelBuilderInvokeParameters Parameters;
        private CrmServiceClient crmSvcCli = null;

        public Connection(ModelBuilderInvokeParameters parameters)
        {
            Parameters = parameters;
        }

        public IOrganizationService CreateConnection()
        {
            // Get profile name if not present.
            string profileName = string.IsNullOrEmpty(Parameters.ConnectionProfileName) ? "default" : Parameters.ConnectionProfileName;

            CrmServiceClient.MaxConnectionTimeout = TimeSpan.FromMinutes(20);
            // Create Connection from Profile and AppName
            if (!string.IsNullOrEmpty(Parameters.ConnectionAppName))
            {
                // Doing a Private login using a host and profile name.
                // Create silent host.. .and login.
                DvInteractiveLogin login = new DvInteractiveLogin();
                login.HostProfileName = profileName;
                login.HostApplicatioNameOveride = Parameters.ConnectionAppName;

                login.ShowDialog();
                if (login.CrmConnectionMgr != null && login.CrmConnectionMgr.CrmSvc != null && login.CrmConnectionMgr.CrmSvc.IsReady)
                {
                    return login.CrmConnectionMgr.CrmSvc;
                }
                else
                {
                    Console.WriteLine(crmSvcCli.LastCrmError);
                    return null;
                }
            }

            // Create connection string from URI
            if (!Parameters.UseInteractiveLogin)
            {
                //CrmServiceClient.AuthOverrideHook
                if (!string.IsNullOrEmpty(Parameters.AccessToken))
                {
                    // Setup Access token.
                    CrmServiceClient.AuthOverrideHook = new UseProvidedAccessToken(Parameters.AccessToken);
                    Uri serviceUri = null;
                    if (!Uri.TryCreate(Parameters.Url, UriKind.RelativeOrAbsolute, out serviceUri))
                    {
                        throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, "Cannot connect to organization service at {0}", this.Parameters.Url));
                    }
                    crmSvcCli = new CrmServiceClient(serviceUri, true);
                    if (crmSvcCli != null && crmSvcCli.IsReady)
                        return crmSvcCli;
                    else
                    {
                        // write error here
                        Console.WriteLine(crmSvcCli.LastCrmError);
                        return null;
                    }

                }
                else
                {
                    crmSvcCli = new CrmServiceClient(GetConnectionString());
                    if (crmSvcCli != null && crmSvcCli.IsReady)
                        return crmSvcCli;
                    else
                    {
                        // write error here
                        Console.WriteLine(crmSvcCli.LastCrmError);
                        return null;
                    }
                }
            }
            else
            {
                DvInteractiveLogin login = new DvInteractiveLogin();
                login.ForceDirectLogin = true;
                login.HostProfileName = profileName;
                if (!string.IsNullOrEmpty(Parameters.ConnectionAppName))
                    login.HostApplicatioNameOveride = Parameters.ConnectionAppName;

                if (!login.ShowDialog().Value)
                {
                    // Failed,  Shutdown and exit.
                    Console.WriteLine("User aborted Login");
                    return null;
                }
                try
                {
                    if (login.CrmConnectionMgr != null && login.CrmConnectionMgr.CrmSvc != null && login.CrmConnectionMgr.CrmSvc.IsReady)
                    {
                        crmSvcCli = login.CrmConnectionMgr.CrmSvc;
                        return crmSvcCli;
                    }
                    else
                    {
                        Console.WriteLine(crmSvcCli.LastCrmError);
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    // Log error.
                    Console.WriteLine("Failed to Login:");
                    Console.WriteLine(ex.Message);
                    return null;
                }


            }
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
    }
    internal class UseProvidedAccessToken : IOverrideAuthHookWrapper
    {
        string AccessToken = string.Empty;
        public UseProvidedAccessToken(string accessToken)
        {
            AccessToken = accessToken;
        }

        public string GetAuthToken(Uri connectedUri)
        {
            return AccessToken;
        }
    }
}
