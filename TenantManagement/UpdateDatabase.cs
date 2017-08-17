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
        public class Credentials
        {
            public string userid { get; set; }
            public string password { get; set; }
        }

        public string databasename { get; set; }
        public string workspaceId { get; set; }
        public string reportId { get; set; }
        public string server { get; set; }
        public Credentials credentials { get; set; }
    }

    public static class UpdateDatabase
    {
        [FunctionName("UpdateDatabase")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("UpdateDatabase processed a request.");

            HttpResponseMessage rtnResponse = null;
            string workspacename = string.Empty;

            try
            {
                // Get request body
                UpdateDatabaseRequest requestparameters = await req.Content.ReadAsAsync<UpdateDatabaseRequest>();

                SQLHelper.PopulateCustomerDB(requestparameters);
                
                rtnResponse = req.CreateResponse(HttpStatusCode.OK);
            }
            catch (RuntimeBinderException)
            {
                //  "name property doesn't exist in request body
                rtnResponse = req.CreateResponse(HttpStatusCode.BadRequest, @"Please pass the database name in the body of your request: { ""name"" : ""myname"", ""connectionstring"" : ""value"" }");
            }
            catch (AggregateException ex)
            {
                rtnResponse = req.CreateResponse(HttpStatusCode.BadRequest, @"An unexpected http error occured. A database of the name you specified may already exist, the connection details are invalid, or perhaps the values contained invalid characters");
            }

            return rtnResponse;
        }
    }
}
