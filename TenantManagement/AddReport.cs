using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using System.Threading.Tasks;
using Microsoft.CSharp.RuntimeBinder;
using System;
using Microsoft.Rest;
using Newtonsoft.Json;
using Microsoft.PowerBI.Api.V2.Models;
using System.Collections.Generic;

namespace TenantManagement
{
    public class AddReportRequest
    {
        public string name { get; set; }
        public string report { get; set; }
        public string server { get; set; }
        public string database { get; set; }
        public string gatewayname { get; set; }
        public UserCredentials credentials { get; set; }
    }

    public static class AddReport
    {
        [FunctionName("AddReport")]

        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "workspaces/{workspaceid}/reports")]HttpRequestMessage req, string workspaceId, TraceWriter log)
        {
            log.Info("AddReport function processed a request.");

            HttpResponseMessage rtnResponse = null;

            try
            {
                // Get request body
                AddReportRequest requestparameters = await req.Content.ReadAsAsync<AddReportRequest>();

                // import the report
                var reportresult = PBIEHelper.ImportPbix(workspaceId, requestparameters.name, requestparameters.report).Result;

                foreach(Dataset dataset in reportresult.Datasets)
                {
                    PBIEHelper.UpdateDataSetConnectionString(workspaceId, dataset.Id, requestparameters);

                    // we're not dealing with an Azure SQL DB, so we may need to create a datasource
                    if (!requestparameters.server.ToLower().Contains(".database.windows.net"))
                    {
                        IEnumerable<GatewayDatasource> datasources = await PBIEHelper.GetGatewayDataSourcesAsync(workspaceId, dataset.Id);
                        if (datasources.Count() <= 0) // no associated gateway, need to create a datasource
                        {
                            // find matching gateway
                            Gateway gateway = await PBIEHelper.GetWorkspaceGatewaysAsync(requestparameters.gatewayname);

                            if (gateway != null)
                            {
                                await PBIEHelper.CreateGatewayDatasourceAsync(gateway, requestparameters.server, requestparameters.database, requestparameters.credentials);
                            }
                        }
                    }
                }            

                rtnResponse = req.CreateResponse(HttpStatusCode.OK, JsonConvert.SerializeObject(reportresult.Id));
            }
            catch (Exception ex)
            {
                rtnResponse = req.CreateResponse(HttpStatusCode.InternalServerError, $"An unexpected error occured. This could be caused by any number of issues including invalid parameters or incorrect database connection details. Please check the request parameters and try again. Details: {ex.Message}");
            }


            return rtnResponse;

        }
    }
}