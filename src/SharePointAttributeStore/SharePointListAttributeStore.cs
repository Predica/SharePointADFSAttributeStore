using Microsoft.IdentityModel.Threading;
using Microsoft.IdentityServer.ClaimsPolicy.Engine.AttributeStore;
using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Predica.Tools.SharePoint.SharePointAttributeStore
{
    // http://msdn.microsoft.com/en-us/library/ee895358.aspx

    public class SharePointListAttributeStore : IAttributeStore
    {
        private string SiteUrl;
        private string ListName;

        delegate string[][] RunQueryDelegate(string query, string[] parameters);

        public SharePointListAttributeStore() { }

        void ctx_MixedAuthRequest(object sender, WebRequestEventArgs e)
        {
            try
            {
                // Add the header that tells SharePoint to use Windows authentication.
                e.WebRequestExecutor.RequestHeaders.Add("X-FORMS_BASED_AUTH_ACCEPTED", "f");
            }
            catch (Exception ex)
            {
                throw new AttributeStoreInvalidConfigurationException("Error setting authentication header" + ex, ex);
            }
        }

        private ClientContext CreateClientContext(string SiteUrl)
        {
            var clientContext = new ClientContext(SiteUrl);
            clientContext.ExecutingWebRequest += ctx_MixedAuthRequest;   // Ensure use of Windows Authentication
            clientContext.AuthenticationMode = ClientAuthenticationMode.Default; //Set the Windows credentials.
            clientContext.Credentials = System.Net.CredentialCache.DefaultCredentials;
            return clientContext;
        }

        public IAsyncResult BeginExecuteQuery(string query, string[] parameters, AsyncCallback callback, object state)
        {
            #region Input check

            if (String.IsNullOrEmpty(query))
            {
                throw new AttributeStoreQueryFormatException("No query string.");
            }

            if (!query.Contains("<ViewFields>"))
            {
                throw new AttributeStoreQueryFormatException("Query must contain ViewFields clause to indicate which fields should be returned by the query");
            }

            #endregion

            /* Note the usage of TypedAsyncResult class defined in Microsoft.IdentityModel.Threading namespace. */
            AsyncResult queryResult = new TypedAsyncResult<string[][]>(callback, state);

            /* The asynchronous query is being implemented using a delegate. This is to show the asynchronous way of running the query,
             * even though we are reading from a simple text file, which can be done synchronously.
             * You may also use a data access interface that provides an asynchronous way to access the data.
             * For example, the BeginExecuteReader method of System.Data.SqlClient.SqlCommand class asynchronously accesses data from a SQL Server data store. */
            RunQueryDelegate queryDelegate = new RunQueryDelegate(RunQuery);
            queryDelegate.BeginInvoke(query, parameters, new AsyncCallback(AsyncQueryCallback), queryResult);
            return queryResult;
        }

        public string[][] EndExecuteQuery(IAsyncResult result)
        {
            return TypedAsyncResult<string[][]>.End(result);
        }

        public void Initialize(Dictionary<string, string> config)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            if (!config.TryGetValue("SiteUrl", out SiteUrl))
            {
                throw new AttributeStoreInvalidConfigurationException("SiteUrl configuration entry not found");
            }
            if (string.IsNullOrEmpty(SiteUrl))
            {
                throw new AttributeStoreInvalidConfigurationException("SiteUrl should be valid");
            }

            if (!config.TryGetValue("ListName", out ListName))
            {
                throw new AttributeStoreInvalidConfigurationException("ListName configuration entry not found");
            }
            if (string.IsNullOrEmpty(ListName))
            {
                throw new AttributeStoreInvalidConfigurationException("ListName should be valid");
            }

            try
            {
                ClientContext clientContext = CreateClientContext(SiteUrl);
                clientContext.ExecuteQuery();
            }
            catch (Exception e)
            {
                throw new AttributeStoreInvalidConfigurationException("Connection to SharePoint unsuccessful", e);
            }
        }

        void AsyncQueryCallback(IAsyncResult result)
        {
            TypedAsyncResult<string[][]> queryResult = (TypedAsyncResult<string[][]>)result.AsyncState;
            System.Runtime.Remoting.Messaging.AsyncResult delegateAsyncResult = (System.Runtime.Remoting.Messaging.AsyncResult)result;
            RunQueryDelegate runQueryDelegate = (RunQueryDelegate)delegateAsyncResult.AsyncDelegate;

            string[][] values = null;
            Exception originalException = null;
            try
            {
                values = runQueryDelegate.EndInvoke(result);
            }
            /* We don't want exceptions to be thrown from the callback method as these need to be made available to the thread that calls EndExecuteQuery. */
            catch (Exception e)
            {
                originalException = e;
            }

            /* Any exception is stored in query Result and re-thrown when EndExecuteQueryMethod calls TypedAsyncResult<string[][]>.End(..) method. */
            queryResult.Complete(values, false, originalException);
        }

        private string[][] RunQuery(string query, string[] parameters)
        {
            // http://msdn.microsoft.com/en-us/library/ee534956.aspx

            #region Prepare query

            ClientContext clientContext = CreateClientContext(SiteUrl);
            List list = clientContext.Web.Lists.GetByTitle(ListName);
            CamlQuery camlQuery = new CamlQuery();
            ListItemCollection collListItem = null;

            try
            {
                if (parameters == null || parameters.Count() == 0)
                {
                    camlQuery.ViewXml = query;
                }
                else
                {
                    camlQuery.ViewXml = String.Format(query, parameters);
                }
            }
            catch (Exception ex)
            {
                throw new AttributeStoreQueryFormatException("Query cannot be constructed with given parameters", ex);
            }

            List<string> fields = new List<string>();

            //get the list of fieldrefs
            try
            {
                XmlDocument queryXml = new XmlDocument();
                queryXml.LoadXml(camlQuery.ViewXml);
                XmlNodeList xList = queryXml.SelectNodes("View/ViewFields/FieldRef");
                if ((xList != null) && (xList.Count > 0))
                {
                    foreach (XmlNode xNode in xList)
                    {
                        fields.Add(xNode.Attributes["Name"].Value);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new AttributeStoreQueryFormatException("Query is misformatted XML - should at minimum have following structure: View/ViewFields/FieldRef", ex);
            }

            if (fields.Count == 0)
            {
                throw new AttributeStoreQueryFormatException("No view fields found in the query");
            }

            #endregion

            #region Process results

            try
            {
                collListItem = list.GetItems(camlQuery);
                clientContext.Load(collListItem);
                clientContext.ExecuteQuery();
            }
            catch (Exception ex)
            {
                throw new AttributeStoreQueryExecutionException("Query failed", ex);
            }

            List<string[]> results = new List<string[]>();

            try
            {
                foreach (ListItem listItem in collListItem)
                {
                    string[] result = new string[fields.Count];
                    int i = 0;

                    foreach (string field in fields)
                    {
                        result[i++] = listItem[field].ToString();
                    }

                    results.Add(result);
                }
            }
            catch (Exception ex)
            {
                throw new AttributeStoreQueryExecutionException("There was an error processing query results data", ex);
            }

            return results.ToArray();

            #endregion
        }
    }
}
