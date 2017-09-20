using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;

namespace TenantManagement
{
    public static class GetReportsByWorkspace
    {
        [FunctionName("GetReportsByWorkspace")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = "workspace/{workspaceid}/Reports")]HttpRequestMessage req, string workspaceId, TraceWriter log)
        {
            log.Info("GetReportsByWorkspace processed a request.");

            // get a list of workspaces the application user has access too
            var reports = await PBIEHelper.GetReportsInWorkspace(workspaceId);

            // convert the list to JSON
            var result = JsonConvert.SerializeObject(reports);

            // return the result
            return req.CreateResponse(HttpStatusCode.OK, result);
        }
    }
}
