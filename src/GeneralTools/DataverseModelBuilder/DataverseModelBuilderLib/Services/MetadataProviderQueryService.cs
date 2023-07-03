using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Metadata.Query;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Microsoft.PowerPlatform.Dataverse.ModelBuilderLib
{
    internal sealed class MetadataProviderQueryService : IMetadataProviderQueryService
    {
        private readonly ModelBuilderInvokeParameters _parameters;
        internal MetadataProviderQueryService(ModelBuilderInvokeParameters parameters)
        {
            this._parameters = parameters;
            _parameters.Logger.TraceVerbose("Creating Default Metadata Provider Query Service");
        }

        public Xrm.Sdk.Metadata.EntityMetadata[] RetrieveEntities(Xrm.Sdk.IOrganizationService service)
        {

            if (!string.IsNullOrEmpty(_parameters.EntityNamesFilter))
            {
                // Read entities from filter list // expand this to support optionsets and such.
                // also support partial names.
                List<string> _s1 = Utility.Utilites.GetItemListFromString(_parameters.ToDictionary(), ";", "entitynamesfilter");

                if (_s1.Count() >= 0)
                {
                    // parse each list along the way now, to pick up the query settings.
                    List<MetadataConditionExpression> _conditions = new List<MetadataConditionExpression>();
                    foreach (var entName in _s1)
                    {
                        _conditions.Add(new MetadataConditionExpression("logicalname", MetadataConditionOperator.Equals, entName));
                    }
                    MetadataFilterExpression entityFilter = new MetadataFilterExpression(Xrm.Sdk.Query.LogicalOperator.Or);
                    entityFilter.Conditions.AddRange(_conditions);

                    EntityQueryExpression query = new EntityQueryExpression();
                    query.Criteria = entityFilter;
                    query.Properties = new MetadataPropertiesExpression() { AllProperties = true };

                    RetrieveMetadataChangesRequest req = new RetrieveMetadataChangesRequest();
                    req.Query = query;

                    RetrieveMetadataChangesResponse resp = (RetrieveMetadataChangesResponse)service.Execute(req);

                    return (EntityMetadata[])resp.EntityMetadata.ToArray();
                }
            }

            OrganizationRequest request = new OrganizationRequest("RetrieveAllEntities");
            request.Parameters["EntityFilters"] = EntityFilters.Entity | EntityFilters.Attributes | EntityFilters.Relationships;
            request.Parameters["RetrieveAsIfPublished"] = false;

            OrganizationResponse response = service.Execute(request);
            return (EntityMetadata[])response.Results["EntityMetadata"];
        }

        public Xrm.Sdk.Metadata.OptionSetMetadataBase[] RetrieveOptionSets(Xrm.Sdk.IOrganizationService service)
        {
            if (_parameters.LegacyMode || _parameters.GenerateGlobalOptionSets)
            {
                OrganizationRequest request = new OrganizationRequest("RetrieveAllOptionSets");
                request.Parameters["RetrieveAsIfPublished"] = true;
                OrganizationResponse response = service.Execute(request);
                return (OptionSetMetadataBase[])response.Results["OptionSetMetadata"];
            }
            else
                return new List<OptionSetMetadataBase>().ToArray(); 

        }

        public SdkMessages RetrieveSdkRequests(Xrm.Sdk.IOrganizationService service)
        {
            string fetchQuery = string.Empty;
            string entityMapFetchQuery = string.Empty; 

            if (!string.IsNullOrEmpty(_parameters.MessageNamesFilter))
            {
                // force custom actions to be generated to on here. 
                _parameters.GenerateSdkMessages = true; 

                // Read messages from filter list
                // also support partial names.
                List<string> _s1 = Utility.Utilites.GetItemListFromString(_parameters.ToDictionary(), ";", "messagenamesfilter");
                string conditionsList = string.Empty;
                if (_s1.Count() >= 0)
                {
                    foreach (var itm in _s1)
                    {
                        if (itm.Contains("*"))
                        {
                            conditionsList += ($"<condition attribute='name' operator='like' value='{itm.Replace("*", "%")}' />");
                        }
                        else
                        {
                            conditionsList += ($"<condition attribute='name' operator='eq' value='{itm}' />");
                        }
                    }
                    if (!string.IsNullOrEmpty(conditionsList))
                    {
                        //fetchQuery = string.Format(Properties.Resources.RetrieveFilterdListOfSdkMessages, conditionsList);
                        fetchQuery = string.Format(Properties.Resources.RetrieveFilterdSdkMessageListBase, conditionsList);
                        entityMapFetchQuery = string.Format(Properties.Resources.RetrieveFilteredSdkMessageListToEntity, conditionsList);
                    }
                }
            }
            else
            {
                if (_parameters.GenerateSdkMessages || _parameters.LegacyMode)
                {
                    fetchQuery = Properties.Resources.RetrieveBaseListOfSdkMessages;
                    entityMapFetchQuery = Properties.Resources.RetrieveSdkMessageToEntity;
                }
            }

            SdkMessages messages = new SdkMessages(null);
            if (!string.IsNullOrEmpty(fetchQuery))
            {
                MessagePagingInfo pagingInfo = null;
                int currentPage = 1;

                ExecuteFetchRequest request = new ExecuteFetchRequest();
                while (pagingInfo == null || pagingInfo.HasMoreRecords)
                {
                    string currentQuery = fetchQuery;
                    if (pagingInfo != null)
                        currentQuery = SetPagingCookie(fetchQuery, pagingInfo.PagingCookig, currentPage);

                    request.FetchXml = currentQuery; 
                    ExecuteFetchResponse response = (ExecuteFetchResponse)service.Execute(request);
                    pagingInfo = SdkMessages.FromFetchResult(messages, (string)response.FetchXmlResult);
                    currentPage++;
                }
            }

            //Add Second Stage to get SDK Message Filters
            if (!string.IsNullOrEmpty(entityMapFetchQuery))
            {
                MessagePagingInfo pagingInfo = null;
                int currentPage = 1;

                ExecuteFetchRequest request = new ExecuteFetchRequest();
                while (pagingInfo == null || pagingInfo.HasMoreRecords)
                {
                    string currentQuery = entityMapFetchQuery;
                    if (pagingInfo != null)
                        currentQuery = SetPagingCookie(entityMapFetchQuery, pagingInfo.PagingCookig, currentPage);

                    request.FetchXml = currentQuery;
                    ExecuteFetchResponse response = (ExecuteFetchResponse)service.Execute(request);
                    pagingInfo = SdkMessages.FromFetchResult(messages, (string)response.FetchXmlResult);
                    currentPage++;
                }
            }
            return messages;
        }

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
    }
}
