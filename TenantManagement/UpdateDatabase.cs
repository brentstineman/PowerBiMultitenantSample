using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.CSharp.RuntimeBinder;
using System;

namespace TenantManagement
{
    public class UpdateDatabaseRequest
    {
        public string workspaceId { get; set; }
        public string reportId { get; set; }
        public string server { get; set; }
        public UserCredentials credentials { get; set; }
    }

    public static class UpdateDatabase
    {
        [FunctionName("UpdateDatabase")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "database/{databasename}")]HttpRequestMessage req, string databasename, TraceWriter log)
        {
            log.Info("UpdateDatabase processed a request.");

            HttpResponseMessage rtnResponse = null;

            try
            {
                // Get request body
                UpdateDatabaseRequest requestparameters = await req.Content.ReadAsAsync<UpdateDatabaseRequest>();

                SQLHelper.PopulateCustomerDB(databasename, requestparameters);
                
                rtnResponse = req.CreateResponse(HttpStatusCode.OK);
            }
            catch (AggregateException ex)
            {
                rtnResponse = req.CreateResponse(HttpStatusCode.BadRequest, $"An unexpected http error occured. A database of the name you specified may already exist, the connection details are invalid, or perhaps the values contained invalid characters. Details: {ex.Message}");
            }

            return rtnResponse;
        }
    }
}
