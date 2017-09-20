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

        public static async Task DeleteReportAsync(string workspaceId, string reportId)
        {
            using (PowerBIClient client = await CreateClient())
            {
                Report selectedReport = await client.Reports.GetReportInGroupAsync(workspaceId, reportId);

                var tmp = await client.Datasets.DeleteDatasetByIdInGroupAsync(workspaceId, selectedReport.DatasetId);
            }
        }

        public static async Task<Gateway> GetWorkspaceGatewaysAsync(string gatewayName)
        {
            Gateway returnVal = null; 

            if (string.IsNullOrEmpty(gatewayName))
            {
                throw new ArgumentException("no gateway name specified on the request . Value is required");
            }

            using (PowerBIClient client = await CreateClient())
            {
                ODataResponseListGateway gateways = await client.Gateways.GetGatewaysAsync();

                IEnumerable<Gateway> gatewayquery = gateways.Value.Where(x => x.Name.Contains(gatewayName));

                try
                {
                    returnVal = gatewayquery.First();
                }
                catch(InvalidOperationException ex)
                {
                    throw new ArgumentException($"Specified gateway '{gatewayName}' not found. Please check the name of the gateway and try again. Details: {ex.Message}");
                }

                return returnVal;
            }
        }

        private static CredentialDetails GetCredentialDetails(GatewayPublicKey gatewayPublicKey, string userName, string Password)
        {
            // build credential object
            var credentialDetails = new CredentialDetails()
            {
                CredentialType = "Basic",
                EncryptionAlgorithm = "RSA-OAEP",
                Credentials = AsymmetricKeyEncryptionHelper.EncodeCredentials(userName, Password, gatewayPublicKey),
                EncryptedConnection = "Encrypted",
                PrivacyLevel = "None"
            };

            return credentialDetails;
        }

        public static async Task<GatewayDatasource> CreateGatewayDatasourceAsync(Gateway gateway, string server, string databaseName, UserCredentials credentials)
        {

            PublishDatasourceToGatewayRequest publishDatasourceRequest = new PublishDatasourceToGatewayRequest()
            {
                DataSourceName = databaseName,
                DataSourceType = "SQL",
                ConnectionDetails = "{ \"server\":\"" + server + "\"," + "\"database\":\"" + databaseName + "\"}",
                CredentialDetails = GetCredentialDetails(gateway.PublicKey, credentials.userid, credentials.password)
            };

            using (var client = await CreateClient())
            {
                GatewayDatasource rtnGatewayDatasource = await client.Gateways.CreateDatasourceAsync(gateway.Id, publishDatasourceRequest);

                return rtnGatewayDatasource;
            }
        }

        public static void UpdateDataSetConnectionString(string workspaceId, string datasetId, AddReportRequest requestparameters)
        {
            ConnectionDetails connectionDetails = new ConnectionDetails($"Data Source={requestparameters.server};Initial Catalog={requestparameters.database}");

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

            string userid = ConfigurationManager.AppSettings["powerbi_user"];
            string password = ConfigurationManager.AppSettings["powerbi_password"];


            // Create a user password cradentials.
            var credential = new UserPasswordCredential(userid, password);

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

        public static async Task<IEnumerable<GatewayDatasource>> GetGatewayDataSourcesAsync(string workspaceId, string datasetId)
        {
            using (PowerBIClient client = await CreateClient())
            {
                ODataResponseListGatewayDatasource response = await client.Datasets.GetGatewayDatasourcesInGroupAsync(workspaceId, datasetId);

                return response.Value;
            }
        }

        public static async Task<IEnumerable<Dataset>> GetDatasetsByAppWorkspaceAsync(string workspaceId)
        {
            using (PowerBIClient client = await CreateClient())
            {
                ODataResponseListDataset response = await client.Datasets.GetDatasetsInGroupAsync(workspaceId);

                ODataResponseListGatewayDatasource response2 = await client.Datasets.GetGatewayDatasourcesInGroupAsync(workspaceId, response.Value[0].Id);

                ODataResponseListDatasource response3 = await client.Datasets.GetDatasourcesInGroupAsync(workspaceId, response.Value[0].Id);;

                return response.Value;
            }
        }

        public class EmbeddedReportSettings
        {
            public string ReportId;
            public string URL;
            public EmbedToken Token;
        }

        public static async Task<EmbeddedReportSettings> GetEmbeddedReportSettings(string workspaceId, string reportId)
        {
            using (PowerBIClient client = await CreateClient())
            {
                // get report details
                Report report = await client.Reports.GetReportInGroupAsync(workspaceId, reportId);

                // Generate Embed Token.
                var generateTokenRequestParameters = new GenerateTokenRequest(accessLevel: "view");
                EmbedToken tokenResponse = await client.Reports.GenerateTokenInGroupAsync(workspaceId, reportId, generateTokenRequestParameters);

                // Generate Embed Configuration.
                var embedConfig = new EmbeddedReportSettings()
                {
                    Token = tokenResponse,
                    URL = report.EmbedUrl,
                    ReportId = report.Id
                };

                return embedConfig;
            }
        }


        public static async Task<IEnumerable<Report>> GetReportsInWorkspace(string workspaceId)
        {
            using (PowerBIClient client = await CreateClient())
            {
                ODataResponseListReport response = await client.Reports.GetReportsInGroupAsync(workspaceId);
                return response.Value;
            }
        }
    }
}
