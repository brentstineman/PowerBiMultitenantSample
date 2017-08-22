using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.PowerBI.Api.V2.Models;
using System;
using Microsoft.Rest;

namespace TenantManagement
{
    public static class DeleteReport
    {
        [FunctionName("DeleteReport")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "delete", Route = "workspace/{workspaceid}/Reports/{reportId}")]HttpRequestMessage req, string workspaceId, string reportId, TraceWriter log)
        {
            log.Info("DeleteReport function processed a request.");

            HttpResponseMessage returnResponse = req.CreateResponse(HttpStatusCode.OK); ;

            try
            {
                await PBIEHelper.DeleteReportAsync(workspaceId, reportId);
            }
            catch (HttpOperationException ex)
            {
                if ((ex.Response.StatusCode == HttpStatusCode.BadRequest) || (ex.Response.StatusCode == HttpStatusCode.NotFound))
                    returnResponse = req.CreateResponse(HttpStatusCode.BadRequest, $"Request failed, Likely cause is that specified report or its dataset could not be found. Details: {ex.Message}");
            }
            catch (Exception ex)
            {
                returnResponse = req.CreateResponse(HttpStatusCode.BadRequest, $"An unknown error occured. Details: {ex.Message}");
            }

            return returnResponse;
        }
    }
}
