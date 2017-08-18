using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using System;
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json;
using Microsoft.Rest;

namespace TenantManagement
{
    public class AddDatabaseRequest
    {
        public string server { get; set; }
        public string database { get; set; }
        public UserCredentials credentials { get; set; }
    }

    public static class CreateDatabase
    {
        [FunctionName("CreateDatabase")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("CreateDatabase processed a request.");

            HttpResponseMessage rtnResponse = null;
            string workspacename = string.Empty;

            try
            {
                // Get request body
                AddDatabaseRequest requestparameters = await req.Content.ReadAsAsync<AddDatabaseRequest>();

                SQLHelper.CreateCustomerDB(requestparameters);

                SQLHelper.CreateCustomerDBSchema(requestparameters);

                rtnResponse = req.CreateResponse(HttpStatusCode.OK);
            }
            catch (AggregateException ex)
            {
                rtnResponse = req.CreateResponse(HttpStatusCode.BadRequest, ex.InnerException.Message);
            }

            return rtnResponse;
        }
    }
}

