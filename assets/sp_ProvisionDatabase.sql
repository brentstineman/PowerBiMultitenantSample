USE Master;  
GO  
CREATE PROCEDURE ProvisionDatabase   
    @databaseName nvarchar(50)
AS   
    SET NOCOUNT ON;  

	exec('create database ' + @databaseName)

	DECLARE @createTable AS varchar(MAX) = '
		USE @databaseName
		CREATE TABLE @databaseName.[dbo].[Customer]
		(
			[WorkspaceID] [nvarchar](max) NULL,
			[ReportID] [nvarchar](max) NULL,
			[DatabaseName] [nvarchar](max) NULL
		) ON[PRIMARY] TEXTIMAGE_ON[PRIMARY]
	'
	DECLARE @finalSQL AS varchar(MAX) = REPLACE(@createTable, '@databaseName', @databaseName)

	EXEC(@finalSQL)
GO  
