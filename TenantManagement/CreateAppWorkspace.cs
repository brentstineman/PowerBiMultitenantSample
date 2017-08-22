using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json;
using System;
using Microsoft.Rest;

namespace TenantManagement
{
    public static class CreateAppWorkspace
    {
        [FunctionName("CreateAppWorkspace")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "workspace")]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("CreateAppWorkspace processed a request.");

            HttpResponseMessage rtnResponse = null;
            string workspacename = string.Empty;

            try
            {
                // Get request body
                dynamic data = await req.Content.ReadAsAsync<object>();

                workspacename = data.name;

                var workspace = PBIEHelper.CreateGroupAsync(workspacename).Result;

                rtnResponse = req.CreateResponse(HttpStatusCode.OK, JsonConvert.SerializeObject(workspace));
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
                    var workspace = PBIEHelper.GetAppWorkspaceByNameAsync(workspacename).Result;
                    rtnResponse = (workspace != null)
                        ? req.CreateResponse(HttpStatusCode.OK, JsonConvert.SerializeObject(workspace))
                        : req.CreateResponse(HttpStatusCode.BadRequest, @"An unexpected http error occured. An App Workspace of the name you specified may already exist. Did it contain invalid characters?");
                }
            }


            return rtnResponse;
        }
    }
}