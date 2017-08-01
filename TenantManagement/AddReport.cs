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

namespace TenantManagement
{
    public static class AddReport
    {
        [FunctionName("AddReport")]

        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "AddReport/{workspaceid}/")]HttpRequestMessage req, string workspaceId, TraceWriter log)
        {
            log.Info("AddReport function processed a request.");

            HttpResponseMessage rtnResponse = null;

            try
            {
                // Get request body
                AddReportRequest requestparameters = await req.Content.ReadAsAsync<AddReportRequest>();

                //workspacename = data.name;

                // make sure the workspace exists

                // import the report
                var reportresult = PBIEHelper.ImportPbix(workspaceId, requestparameters.name, requestparameters.report).Result;
                foreach(Dataset dataset in reportresult.Datasets)
                {
                    PBIEHelper.UpdateDataSetConnectionString(workspaceId, dataset.Id, requestparameters);
                }

                rtnResponse = req.CreateResponse(HttpStatusCode.OK, JsonConvert.SerializeObject(reportresult.Id));
            }
            catch (RuntimeBinderException)
            {
                //  "name property doesn't exist in request body
                rtnResponse = req.CreateResponse(HttpStatusCode.BadRequest, @"Please pass the app workspace name in the body of your request: { ""name"" : ""myname"" }");
            }
            catch (AggregateException ex)
            {
                if (ex.InnerException.GetType().Equals(typeof(HttpOperationException)))
                {
                    // check to see if the workspace already exists
                    //var workspace = PBIEHelper.GetAppWorkspaceByNameAsync(workspacename).Result;
                    //rtnResponse = (workspace != null)
                    //    ? req.CreateResponse(HttpStatusCode.OK, JsonConvert.SerializeObject(workspace))
                    //    : req.CreateResponse(HttpStatusCode.BadRequest, @"An unexpected http error occured. An App Workspace of the name you specified may already existspecified already exist? Did it contain invalid characters? body of your request: { ""name"" : ""myname"" }");
                }
            }


            return rtnResponse;

        }
    }
}