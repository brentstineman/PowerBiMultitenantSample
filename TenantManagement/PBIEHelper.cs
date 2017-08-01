using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Rest;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Threading;
using System.Configuration;

using System.Net;
using System.IO;
using Newtonsoft.Json;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.PowerBI.Api.V2;
using Microsoft.PowerBI.Api.V2.Models;

namespace TenantManagement
{
    public class PBIEHelper
    {
        public static async Task<IEnumerable<Group>> GetAppWorkspacesAsync()
        {
            using (PowerBIClient client = await CreateClient())
            {
                ODataResponseListGroup response = await client.Groups.GetGroupsAsync();
                return response.Value;
            }
        }

        public static async Task<Group> GetAppWorkspaceByNameAsync(string workspaceName)
        {
            Group workspace = null;

            using (PowerBIClient client = await CreateClient())
            {
                ODataResponseListGroup workspaces = await client.Groups.GetGroupsAsync();

                IEnumerable<Group> workspaceQuery = workspaces.Value.Where(x => x.Name.Equals(workspaceName));
                // get gateway we're after

                if (workspaceQuery.Count() > 0)
                    workspace = workspaceQuery.FirstOrDefault<Group>();

                return workspace;
            }
        }

        public static async Task<Group> CreateGroupAsync(string workspaceName)
        {
            GroupCreationRequest request = null;

            if (!string.IsNullOrEmpty(workspaceName))
            {
                request = new GroupCreationRequest(workspaceName);
            }

            using (PowerBIClient client = await CreateClient())
            {
                return await client.Groups.CreateGroupAsync(request);
            }
        }

        /// <summary>
        /// Creates a new instance of the PowerBIClient with the specified token
        /// </summary>
        /// <returns></returns>
        static async Task<PowerBIClient> CreateClient()
        {
            // Create a token credentials with "AppKey" type
            string accessToken = await PBIEHelper.GetPowerBIAPIAuthTokenAsync();
            var tokenCredentials = new TokenCredentials(accessToken, "Bearer");

            // Instantiate your Power BI client passing in the required credentials
            var client = new PowerBIClient(new Uri(ConfigurationManager.AppSettings["apiUrl"]), tokenCredentials);

            return client;
        }

        /// <summary>
        /// Imports a Power BI Desktop file (pbix) into the Power BI Embedded service
        /// </summary>
        /// <param name="workspaceId">The target Power BI workspace id</param>
        /// <param name="datasetName">The dataset name to apply to the uploaded dataset</param>
        /// <param name="filePath">A local file path on your computer</param>
        /// <returns></returns>
        public static async Task<Import> ImportPbix(string workspaceId, string datasetName, string reportURL)
        {

            using (var reportStream = GetReportStream(reportURL))
            {
                using (PowerBIClient client = await CreateClient())
                {
                    // Set request timeout to support uploading large PBIX files
                    client.HttpClient.Timeout = TimeSpan.FromMinutes(60);
                    client.HttpClient.DefaultRequestHeaders.Add("ActivityId", Guid.NewGuid().ToString());

                    // Import PBIX file from the file stream
                    var import = await client.Imports.PostImportWithFileAsyncInGroup(workspaceId, reportStream, datasetName);

                    // Example of polling the import to check when the import has succeeded.
                    while (import.ImportState != "Succeeded" && import.ImportState != "Failed")
                    {
                        import = await client.Imports.GetImportByIdInGroupAsync(workspaceId, import.Id);
                        Console.WriteLine("Checking import state... {0}", import.ImportState);
                        Thread.Sleep(1000);
                    }

                    if (import.ImportState == "Failed")
                        throw new Exception("report import failed");

                    return import;
                }
            }
        }

        public static async Task<IEnumerable<Gateway>> GetWorkspaceGatewaysAsync()
        {
            using (PowerBIClient client = await CreateClient())
            {
                ODataResponseListGateway response = await client.Gateways.GetGatewaysAsync();
                return response.Value;
            }
        }

        private static CredentialDetails GetCredentialDetails(GatewayPublicKey gatewayPublicKey)
        {
            string username = ConfigurationManager.AppSettings["sqlUserName"];
            string password = ConfigurationManager.AppSettings["sqlPassword"];

            // build credential object
            var credentialDetails = new CredentialDetails()
            {
                CredentialType = "Basic",
                EncryptionAlgorithm = "RSA-OAEP",
                Credentials = AsymmetricKeyEncryptionHelper.EncodeCredentials(username, password, gatewayPublicKey),
                EncryptedConnection = "Encrypted",
                PrivacyLevel = "None"
            };

            return credentialDetails;
        }

        public static async Task<GatewayDatasource> CreateDatasourceAsync(string databaseName)
        {
            string gatewayName = ConfigurationManager.AppSettings["datasourceGatewayName"];

            // get data gatway's public key. Assuming gateway is first and only
            IEnumerable<Gateway> gateways = await GetWorkspaceGatewaysAsync();
            // filter gateway list
            // TODO: use something more exact then "contains" to get the right gateway
            IEnumerable<Gateway> gatewayquery = gateways.Where(x => x.Name.Contains(gatewayName));
            // get gateway we're after
            Gateway gateway = gatewayquery.FirstOrDefault<Gateway>();

            PublishDatasourceToGatewayRequest publishDatasourceRequest = new PublishDatasourceToGatewayRequest()
            {
                DataSourceName = databaseName,
                DataSourceType = "SQL",
                ConnectionDetails = "{ \"server\":\"" + ConfigurationManager.AppSettings["datasourceServerName"] + "\"," + "\"database\":\"" + databaseName + "\"}",
                CredentialDetails = GetCredentialDetails(gateway.PublicKey)
            };

            using (var client = await CreateClient())
            {
                IEnumerable<GatewayDatasource> datasources = await GetGatewayDatasourcesAsync(gateway.Id); // get datasources associated with teh current gateway
                // TODO: use something more exact then "contains" to get the right gateway
                IEnumerable<GatewayDatasource> datasourceQuery = datasources.Where(x => x.DatasourceName.Contains(databaseName));
                // If we have a datasource that matches our name, return that datasource, don't create a new one
                if (datasourceQuery.Count<GatewayDatasource>() > 0)
                    return datasourceQuery.FirstOrDefault<GatewayDatasource>();

                GatewayDatasource rtnGatewayDatasource = await client.Gateways.CreateDatasourceAsync(gateway.Id, publishDatasourceRequest);

                return rtnGatewayDatasource;
            }
        }

        public static void UpdateDataSetConnectionString(string workspaceId, string datasetId, AddReportRequest requestparameters)
        {
            ConnectionDetails connectionDetails = new ConnectionDetails($"data source={requestparameters.server};initial catalog={requestparameters.database};persist security info=True;encrypt=True;trustservercertificate=False;");
            if (requestparameters.server.Contains("database.windows.net"))
                connectionDetails.ConnectionString = connectionDetails.ConnectionString + $"Uid={requestparameters.credentials.userid};Pwd={requestparameters.credentials.password}";

            using (PowerBIClient client = CreateClient().Result)
            {
                client.Datasets.SetAllDatasetConnectionsInGroupAsync(workspaceId, datasetId, connectionDetails).Wait();
            }
        }

        static System.IO.Stream GetReportStream(string reportURL)
        {
            CloudStorageAccount account = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["AzureWebJobsStorage"]);
            CloudBlobClient blobClient = account.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(ConfigurationManager.AppSettings["reportContainer"]);
            CloudBlockBlob blob = container.GetBlockBlobReference(reportURL);

            return blob.OpenRead();
        }

        public async static Task<string> GetPowerBIAPIAuthTokenAsync()
        {
            string authorityUrl = ConfigurationManager.AppSettings["authorityUrl"];
            string resourceUrl = ConfigurationManager.AppSettings["powerbiresourceUrl"];

            string clientId = ConfigurationManager.AppSettings["client_id"];
            string clientSecret = ConfigurationManager.AppSettings["client_secret"];

            // Create a user password cradentials.
            var credential = new UserPasswordCredential(ConfigurationManager.AppSettings["powerbi_user"], ConfigurationManager.AppSettings["powerbi_password"]);

            // Authenticate using created credentials
            var authenticationContext = new AuthenticationContext(authorityUrl);
            var authenticationResult = await authenticationContext.AcquireTokenAsync(resourceUrl, clientId, credential);

            if (authenticationResult == null)
            {
                throw new Exception("Authentication Failed");
            }

            return authenticationResult.AccessToken;
        }

        public static async Task<IEnumerable<GatewayDatasource>> GetGatewayDatasourcesAsync(string gatewayId)
        {
           using (PowerBIClient client = await CreateClient())
            {
               ODataResponseListGatewayDatasource response = await client.Gateways.GetDatasourcesAsync(gatewayId);
                return response.Value;
            }
        }

        public static async Task<IEnumerable<Dataset>> GetDatasetsByppWorkspaceAsync(string workspaceId)
        {
            using (PowerBIClient client = await CreateClient())
            {
                ODataResponseListDataset response = await client.Datasets.GetDatasetsInGroupAsync(workspaceId);

                ODataResponseListGatewayDatasource response2 = await client.Datasets.GetGatewayDatasourcesInGroupAsync(workspaceId, response.Value[0].Id);

                ODataResponseListDatasource response3 = await client.Datasets.GetDatasourcesInGroupAsync(workspaceId, response.Value[0].Id);;

                return response.Value;
            }
        }
    }
}
