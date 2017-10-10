# PowerBiMultitenantSample
A sample solution for doing multi-tenant PowerBI report management. 

This project uses a collection of Azure Functions to illustrate common tasks when dealing with Power BI report management. Tasks such as importing reports, creating app workspaces, and changing report connection details. Its part of a larger effort to highlight a way of dealing with multi-tenancy challenges when using PowerBi Embedded reports in applications.

These functions have been implemented in a way to create a API footprint. These API methods are as follows:

Post /api/workspace : creates a new app workspace

Get /apiworkspace : get a list of existing workspaces

Post /api/workspace/\<workspaceid\>/reports : add/import a report to a workspace

Get /api/workspace/\<workspaceid\>/reports : get a list of reports in a workspace

Del /api/workspace/\<workspaceid\>/reports/\<reportid\> : remove a report from a workspace

Get /api/workspace/\<workspaceid\>/reports/\<reportid\> : get embedded settings for a report

Additionally, there's a series of related database APIs that were created to help demonstrate the solution.

Post /api/database : create a database

Post /api/database/\<databasename/> : update the database

Unless you're using this sample in conjuntion with its sister blog post, you likely won't need these methods. However, they are provided here out of simplicity. 

Currently, the solution only supports reports that are doing live queries against Azure SQL or SQL Server. The hope is that should this soluiton prove useful, it will be extended to support other data stores.

## Setting up an environment
*pending*

## Settng up the project
Once you've cloned this repository locally, you should be able to open it using Visual Studio 2017. As previously mentioned, the project uses the new Azure Function SDK so you'll need to make sure you've updated VS and have the function SDK installed. Easiest way to do this is to open up Visual Studio and try to create a Function Project. If its not listed, make sure to update Visual Studio and once complete, make sure the Azure Functions and Web Jobs Tools is installed under "Extensions and Updates". 

With that out of the way, the project should build successfully. If it fails to build, that may be due to updates required to the Nuget packages. If you encounter this, please either fix it and submit a pull request, or at least drop me a message to alert me to the issue. 

Once the project builds successfully, we then need to configure it by setting up our local.settings.json file. This file contains the application/environment settings that will be used when running locally. You'll note that there is no file by this name in the project. I've done this intentionally so that changes don't accidently get committed to the project (its ignored by default). Instead, I've included a file called sample.local.settings.json. You just need to copy and rename that file to local.settings.json and provide values for all the values as follows:

**AzureWebJobsSTorage** - the storage connection string where web job packages are stored

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

To use these files, simply install Postman and import the collection and environment files. Once imported, you'll need to customize the collection which will supply the variables used by the collection's methods. 

For more on Postman environments and variables, please see [https://www.getpostman.com/docs/postman/environments_and_globals/manage_environments]


