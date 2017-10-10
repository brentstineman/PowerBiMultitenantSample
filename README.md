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

Post /api/database/\<databasename/> : update the database, adding values into the table we created for our report

Unless you're using this sample in conjuntion with its sister blog post, you likely won't need these methods. However, they are provided here out of simplicity. 

Currently, the solution only supports reports that are doing live queries against Azure SQL or SQL Server. The hope is that should this soluiton prove useful, it will be extended to support other data stores.

## Setting up an environment
While this project is based on using the local runtime for Azure Functions, it is still dependent on the online Power BI environment. We also take a dependency on Azure AD and an Azure Storage account (to store our reports). So before we can actually leverage the functions, we first need to set up these online components. 

### Power BI Environment Setup
To work with the Power BI API, we need to set up both an Azure AD user and an Application (as of this post, the Power BI SDK does not support Azure style service tokens). I'll refer to these as the User Principle and Application Principle to help keep them straight. Additionally, while this repository provides its own walkthrough, you can find additional information/details by going to the official documentation. Two links I've found most useful are the [Embedded dashboards (for developers)](https://powerbi.microsoft.com/en-us/documentation/powerbi-developer-embed-sample-app-owns-data/) and [Embedding reports when the application owns the data](https://powerbi.microsoft.com/en-us/documentation/powerbi-developer-embedding-content/).

First, we need to create an [Azure AD User Principle (username/password)](https://powerbi.microsoft.com/en-us/documentation/powerbi-developer-embedding-content/#step-1-setup-your-embedded-analytics-development-environment). For this we'll need an Active Directory Tenant. If you have a subscription to Azure or Office 365, you already have one. But you'll also need administrative access since we need to create a new user ID in that tenant. 

The simplest way IMHO to do this is to create a new Azure AD tenant in Azure, and put your user in there. Yeah, it may have some funky @onmicrosoft.com domain, but since this user will only be used to programmatically access the API, that's fine. Once the user has been created, use it to log in at [PowerBI.com](https://www.powerbi.com). Then you can enable its free, 60 day trial subscription to PowerBI Pro. For developers, you can repeat this process (creating a new user and a new free trial) every 60 days. But for production you'll want to purchase a paid subscription. The documentation calls this an "Application Master User Account", as I mentioned, I'll just call this our User Principle. 

Once created, save the username and password as we'll need to use those to configure the application settings. 

The next step is to [create the Application and give it the proper permissions](https://powerbi.microsoft.com/en-us/documentation/powerbi-developer-register-app/) using the same Azure AD tenant that holds our User Principle. For this project, we'll create a Server-side Web App and give it a a redirect URL of http://localhost:13526/redirect. The home page url will just be http://localhost:13526. When it comes to permissions, give the application the appropriate permissions (all of them if you're going ot use all this project's API methods) and finish registering the application. 

Note: be sure to snag the client ID and client secret. We'll need those when setting up the project's environment. Also don't forget to "apply permissions" so that all users in the tenant are allowed to use the application.

Last, but not least, if you're going to leverage the "add report" method of this project, you'll want to create an Azure Storage Account. In that account you'll want to create a blob container into which any Power BI PBIX files will be placed. Then, using your favorite storage exploration tool, upload the sample PBIX files that are provided in the assets folder of this repository. 


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

To use these files, simply install Postman and import the collection and environment files. Once imported, you'll need to customize the collection which will supply the variables used by the collection's methods. 

For more on Postman environments and variables, please see [https://www.getpostman.com/docs/postman/environments_and_globals/manage_environments]


