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

## Working with the API
To make working with the API easier, especially when just trying to learn what its doing. I've created a [Postman](https://www.getpostman.com/) collection (PowerBIMultitenantSample.postman_collection.json) and exported it to this repository. This collection uses variables that should be defined in a cooresponding Postman environment (PowerBIMultitenantSample-Local.postman_environment.json). A sample environment file, to outline what values should be provided, has also been placed in the repository.

To use these files, simply install Postman and import the collection and environment files. Once imported, you'll need to customize the collection which will supply the variables used by the collection's methods. 

For more on Postman environments and variables, please see [https://www.getpostman.com/docs/postman/environments_and_globals/manage_environments]


