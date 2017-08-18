using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using System;
using Newtonsoft.Json;

namespace TenantManagement
{
    public static class GetAppWorkspaces
    {
        [FunctionName("GetAppWorkspaces")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("GetAppWorkspaces processed a request.");

            // get a list of workspaces the application user has access too
            var workspaces = await PBIEHelper.GetAppWorkspacesAsync();

            // convert the list to JSON
            var result = JsonConvert.SerializeObject(workspaces); 

            // return the result
            return req.CreateResponse(HttpStatusCode.OK, result);
        }
    }
}