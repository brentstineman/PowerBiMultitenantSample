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
            SqlConnectionStringBuilder builder =  new SqlConnectionStringBuilder();
            builder["Data Source"] = requestParams.server;
            builder["Initial Catalog"] = requestParams.database;
            builder["user id"] = requestParams.credentials.userid;
            builder["password"] = requestParams.credentials.password;

            using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
            {
                connection.Open();

                //// create database
                SqlCommand cmd = new SqlCommand($"CREATE DATABASE [{requestParams.databasename}]", connection);
                cmd.CommandTimeout = 60; // default is only 30, if working with Azure SQL DB, we need to bump this up
                cmd.ExecuteNonQueryAsync().Wait();

                connection.Close();
            }

        }

        public static void CreateCustomerDBSchema(AddDatabaseRequest requestParams)
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
            builder["Data Source"] = requestParams.server;
            builder["Initial Catalog"] = requestParams.databasename;
            builder["user id"] = requestParams.credentials.userid;
            builder["password"] = requestParams.credentials.password;

            // create table in database
            using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
            {
                connection.Open();

                SqlCommand cmd = new SqlCommand(createTable, connection);
                cmd.CommandTimeout = 60; // default is only 30, if working with Azure SQL DB, we need to bump this up
                cmd.CommandType = CommandType.Text;
                //var dbNameParm = cmd.Parameters.Add("@databaseName", SqlDbType.NVarChar);
                //dbNameParm.Value = requestParams.databasename;
                cmd.ExecuteNonQueryAsync().Wait();

                connection.Close();
            }

        }

        public static void PopulateCustomerDB(UpdateDatabaseRequest requestParams)
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
            builder["Data Source"] = requestParams.server;
            builder["Initial Catalog"] = requestParams.databasename;
            builder["user id"] = requestParams.credentials.userid;
            builder["password"] = requestParams.credentials.password;

            using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
            {
                connection.Open();

                // populate table
                var insertQuery = string.Format(queryInsertIntoTable, requestParams.workspaceId, requestParams.reportId, requestParams.databasename);
                SqlCommand cmd = new SqlCommand(insertQuery, connection);
                cmd.ExecuteNonQueryAsync().Wait();

                connection.Close(); 
            }
        }
    }
}
