using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Rest;
using System;
using Newtonsoft.Json;

namespace TenantManagement
{
    public static class GetEmbeddedReportSettings
    {
        [FunctionName("GetEmbeddedReportSettings")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = "workspace/{workspaceid}/Reports/{reportId}")]HttpRequestMessage req, string workspaceId, string reportId, TraceWriter log)
        {
            log.Info("GetEmbeddedReportSettings function processed a request.");

            HttpResponseMessage returnResponse = req.CreateResponse(HttpStatusCode.OK);

            try
            {
                var settings = await PBIEHelper.GetEmbeddedReportSettings(workspaceId, reportId);

                var result = JsonConvert.SerializeObject(settings);

                // return the result
                returnResponse = req.CreateResponse(HttpStatusCode.OK, result);
            }
            catch (HttpOperationException ex)
            {
                if ((ex.Response.StatusCode == HttpStatusCode.BadRequest) || (ex.Response.StatusCode == HttpStatusCode.NotFound))
                    returnResponse = req.CreateResponse(ex.Response.StatusCode, $"Request failed, Likely cause is that specified workspace or report could not be found. Details: {ex.Message}");
                else if (ex.Response.StatusCode == HttpStatusCode.Unauthorized)
                    returnResponse = req.CreateResponse(ex.Response.StatusCode, $"Request failed, there appears to be an issue with access. Details: {ex.Message}");
                else
                    throw ex;
            }
            catch (Exception ex)
            {
                returnResponse = req.CreateResponse(HttpStatusCode.BadRequest, $"An unknown error occured. Details: {ex.Message}");
            }

            return returnResponse;
        }
    }
}
