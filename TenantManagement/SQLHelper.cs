using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TenantManagement
{
    public class SQLHelper
    {
        private const string queryInsertIntoTable =
            @"INSERT INTO Customer VALUES ('{0}', '{1}', '{2}')";

        private const string createTable =
            @"CREATE TABLE [dbo].[Customer]
                (

                    [WorkspaceID][nvarchar](max) NULL,

                   [ReportID] [nvarchar]
                (max) NULL,

                   [DatabaseName] [nvarchar]
                (max) NULL
		        ) ON[PRIMARY] TEXTIMAGE_ON[PRIMARY]";

        public static void CreateCustomerDB(AddDatabaseRequest requestParams)
        {
            using (SqlConnection connection = CreateSQLConnection(requestParams.server, "master", requestParams.credentials))
            {
                connection.Open();

                // create database
                SqlCommand cmd = new SqlCommand($"CREATE DATABASE [{requestParams.database}]", connection)
                {
                    CommandTimeout = 60 // default is only 30, if working with Azure SQL DB, we need to bump this up
                };
                cmd.ExecuteNonQueryAsync().Wait();

                connection.Close();
            }

        }

        private static SqlConnection CreateSQLConnection(string server, string database, UserCredentials creds)
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder
            {
                ["Data Source"] = server,
                ["Initial Catalog"] = database,
                ["user id"] = creds.userid,
                ["password"] = creds.password
            };

            return new SqlConnection(builder.ConnectionString);
        }

        public static void CreateCustomerDBSchema(AddDatabaseRequest requestParams)
        {
            // create table in database
            using (SqlConnection connection = CreateSQLConnection(requestParams.server, requestParams.database, requestParams.credentials))
            {
                connection.Open();

                SqlCommand cmd = new SqlCommand(createTable, connection)
                {
                    CommandTimeout = 60, // default is only 30, if working with Azure SQL DB, we need to bump this up
                    CommandType = CommandType.Text
                };
                cmd.ExecuteNonQueryAsync().Wait();

                connection.Close();
            }
        }

        public static void PopulateCustomerDB(UpdateDatabaseRequest requestParams)
        {
            using (SqlConnection connection = CreateSQLConnection(requestParams.server, requestParams.database, requestParams.credentials))
            {
                connection.Open();

                // populate table
                var insertQuery = string.Format(queryInsertIntoTable, requestParams.workspaceId, requestParams.reportId, requestParams.database);
                SqlCommand cmd = new SqlCommand(insertQuery, connection);
                cmd.ExecuteNonQueryAsync().Wait();

                connection.Close(); 
            }
        }
    }
}
