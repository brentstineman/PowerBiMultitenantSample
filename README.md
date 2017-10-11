# PowerBiMultitenantSample
This project uses a collection of Azure Functions to illustrate common tasks when dealing with Power BI report management. Tasks such as importing reports, creating app workspaces, and changing report connection details are integral when dealing with the multi-tenancy challenges of using PowerBi Embedded reports in applications.

The intent of this project is to provide a reusable APi that you can leverage as part of your own applications. Hopefully, most of the code can simply be deployed and used "as is", but you are welcome to just copy the code you need for your specific implementation. 

##  The Approach
If you are building a multi-tenant web application and want to use PowerBI reports to display information to your end users, you will eventually realize you have a challenge in ensuring that your reports only display the data appropriate to the requesting user. Currently, PowerBI does not easily support this.

This solution is based on the ability to change the connection details of Power BI reports that are using live query and either SQL Server (via a data gateway) or Azure SQL DB. The overall approach is that each organization/tenant that's using your application would have its own Power BI App Workspace (this provides security/management boundaries). In the app workspaces would be reports with connection details specific to the tenant. Those details would either connect to seperate databases, or leveraging row level filtering and a tenant specific database username/password. 

This project uses Azure Functions to expose a REST based API that allows you to create workspaces, import template PBIX files into those workspaces, and update the report connection details. Thus making this approach to tenant report management easily reusable. Additionally, the API includes a few methods to help manage databases (either in SQL Server of SQL DB) as well as a few supporting methods (get workspaces, get reports, get embedded report details).

## API Overview
The Azure functions that comprise this project have been implemented in a way to create a single/unified API. Each function is its own API method. The API methods are as follows (bracketed values indicate variables):

**Post** /api/workspaces : creates a new app workspace

**Get** /api/workspaces : get a list of existing workspaces

**Post** /api/workspaces/\<workspaceid\>/reports : add/import a report to a workspace

**Get** /api/workspaces/\<workspaceid\>/reports : get a list of reports in a workspace

**Del** /api/workspaces/\<workspaceid\>/reports/\<reportid\> : remove a report from a workspace

**Get** /api/workspaces/\<workspaceid\>/reports/\<reportid\> : get embedded settings for a report

Additionally, there's a couple of related database APIs that were created to help demonstrate the solution.

**Post** /api/database : create a database

**Post** /api/database/\<databasename/> : update the database, adding values into the table we created for our report

Unless you're using this sample in conjuntion with its sister blog post, you likely won't need these methods. However, they are provided here out of simplicity. 

Currently, the solution only supports reports that are doing live queries against Azure SQL or SQL Server. The hope is that should this soluiton prove useful, it will be extended to support other data stores.

## Setting up an environment
While this project is based on Azure Functions (and uses the local runtime), it is still dependent on the online Power BI environment and some other online components. It also takes a dependency on Azure AD, Azure Storage (to store report PBIX files), and a SQL database. Before you can leverage the functions, you first need to set up these online components. 

### Power BI Setup
To work with the Power BI API, we need to set up both an Azure AD user and an Application (as of this post, the Power BI API does not support Azure style service tokens). I'll refer to these as the User Principle and Application Principle to help keep them straight. Additionally, while this repository provides its own walkthrough, you can find additional information/details by going to the official documentation. Two links I've found most useful are the [Embedded dashboards (for developers)](https://powerbi.microsoft.com/en-us/documentation/powerbi-developer-embed-sample-app-owns-data/) and [Embedding reports when the application owns the data](https://powerbi.microsoft.com/en-us/documentation/powerbi-developer-embedding-content/).

First, we need to create an [Azure AD User Principle (username/password)](https://powerbi.microsoft.com/en-us/documentation/powerbi-developer-embedding-content/#step-1-setup-your-embedded-analytics-development-environment). For this we'll need an Active Directory Tenant. If you have a subscription to Azure or Office 365, you already have one. But you'll also need administrative access since we need to create a new user ID in that tenant. 

The simplest way IMHO to do this is to create a new Azure AD tenant in Azure, and put your user in there. Yeah, it may have some funky @onmicrosoft.com domain, but since this user will only be used to programmatically access the API, that's fine. Once the user has been created, use it to log in at [PowerBI.com](https://www.powerbi.com). Then you can enable its free, 60 day trial subscription to PowerBI Pro. For developers, you can repeat this process (creating a new user and a new free trial) every 60 days. But for production you'll want to purchase a paid subscription. The documentation calls this an "Application Master User Account", as I mentioned, I'll just call this our User Principle. 

The next step is to [create the Application Principle and give it the proper permissions](https://powerbi.microsoft.com/en-us/documentation/powerbi-developer-register-app/) using the same Azure AD tenant that holds the User Principle. For this project, we'll create a Server-side Web App and give it a a redirect URL of http://localhost:13526/redirect. The home page url will just be http://localhost:13526. When it comes to permissions, give the application the appropriate permissions (all of them if you're going to use all this project's API methods) and finish registering the application. 

Note: be sure to snag the client ID and client secret after you register the application. We'll need those when setting up the project's environment. Also don't forget to "apply permissions" so that all users in the tenant are allowed to use the application.

### Storage Account and template reports
If you're going to leverage the "add report" method of this project, you'll want to create an Azure Storage Account. In that account create a blob container into which any Power BI PBIX files will be placed. Then, using your favorite storage exploration tool, upload the sample PBIX files that are provided in the assets folder of this repository. 

### A SQL based database
To properly leverage the full power of the API, you're also going to need to create an Azure SQL Database instance and a SQL Server which will be exposed via the [Power BI data gateway](https://powerbi.microsoft.com/en-us/gateway/). I found it easy to create an Azure hosted SQL Server virtual machine and install the data gateway there. 

Be sure to secure the database to prevent remote access except from where needed, either from your local machine's IP address, or Azrue Services. The database management API methods will attempt connect directly to the database. But PowerBI will use a data gateway. The Power BI data gateway is based on Service Bus Hybrid Connections, so please refer to [that documentation for a complete list of required ports](https://docs.microsoft.com/en-us/azure/biztalk-services/integration-hybrid-connection-overview#security-and-ports). 


## Setting up the project
Once you've cloned this repository locally, you should be able to open it using Visual Studio 2017. As previously mentioned, the project uses the new Azure Function SDK so you'll need to make sure you've updated VS and have the function SDK installed. Easiest way to do this is to open up Visual Studio and try to create a Function Project. If its not listed, make sure to update Visual Studio and once complete, make sure the Azure Functions and Web Jobs Tools is installed under "Extensions and Updates". 

With that out of the way, the project should build successfully. If it fails to build, that may be due to updates required to the Nuget packages. If you encounter this, please either fix it and submit a pull request, or at least drop me a message to alert me to the issue. 

Once the project builds successfully, we then need to configure it by setting up our local.settings.json file. This file contains the application/environment settings that will be used when running locally. You'll note that there is no file by this name in the project. I've done this intentionally so that changes don't accidently get committed to the project (its ignored by default). Instead, I've included a file called sample.local.settings.json. You just need to copy and rename that file to local.settings.json and provide values for all the values as follows:

**AzureWebJobsStorage** - the storage connection string where web job packages are stored

**reportContainer** - the container in that storage account where any PowerBI PBIX files will be placed

**AzureWebJobsDashboard** - the storage connection string where web job dashboard is stored

**authorityUrl** = The URL used to authenticate the application user identity. Leave as https://login.windows.net/common/oauth2/authorize

**apiUrl** - the URL for the Power BI API, Leave as https://api.powerbi.com/

**powerbiresourceUrl** - The URL used to get power BI reports, leave as https://analysis.windows.net/powerbi/api

**graphresourceUrl** - The URL used for interacting with the Microsoft O365 Graph API, not curently used but left for reference. Leave as https://graph.microsoft.com/

**AzureADTenantID** - The Azure AD tenant ID that contains the application and user that will be used to access the management APIs.

**client_id** - The Azure AD Application Id. Used to grant specific API usage permissions. Obtained while setting up your environment. 

**client_secret** - The Azure AD Application secret. Obtained when creating the application. 

**powerbi_user** - The user ID that will be used by the application when authenticating against the management APIs. Created during setup. User must have access to the application as well as a Power BI Pro user license. 

**powerbi_password** - The password associated with the powerbi\_user identity. 

## Working with the API
To make working with the API easier, especially when just trying to learn what its doing. I've created a [Postman](https://www.getpostman.com/) collection (PowerBIMultitenantSample.postman_collection.json) and exported it to this repository. This collection uses variables that should be defined in a cooresponding Postman environment (PowerBIMultitenantSample-Local.postman_environment.json). A sample environment file, to outline what values should be provided, has also been placed in the repository.

To use these files, simply install Postman and import the collection and environment files. Once imported, you'll need to customize the environment settings which will supply the variables used by the collection's methods. 

For more on Postman environments and variables, please see [https://www.getpostman.com/docs/postman/environments_and_globals/manage_environments]

With the project running either locally or deployed to Azure, and your collections properly configured, you're ready to start testing out the API. 

Most of the API methods in the Postman collection use query string parameters that are set in the imported environment. However, there are others that contain a JSON body that provides further details. When working with the API, pay close attention to the environment parameters. Values like App Workspace names, IDs, report IDs, etc... are specific to not only your test environment but also to individual test runs. 

When you're ready to execute an end to tend test, the expectation is that you'd perform the following API calls in the order provided: create database, create app workspace, import report, update database

Upon completion, you can log into PowerBI.com using the same username/password you created for the application and you should be able to view all workspaces and reports. This also gives you an easy way to check and make sure that the reports are properly rendering based on the target dataset. 

Its important to note that once a workspace has been created, it cannot be removed. So its recommended that use a single workspace for repeated testing. You can remove the report/dataset via the Portal, us use the API to "clean up" the workspace between test runs. 
