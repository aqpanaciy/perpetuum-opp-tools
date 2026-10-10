USE [perpetuumsa]
GO

DECLARE @Id INT = 200;

MERGE [dbo].[zones] AS Target
USING (SELECT @Id AS [id]) AS Source
ON (Target.[id] = Source.[id])

WHEN MATCHED THEN
    UPDATE SET
         [x] = 12800
        ,[y] = -1024
        ,[name] = 'zone_delta_z200'
        ,[description] = 'zone_delta_z200_desc'
        ,[note] = 'delta_note'
        ,[fertility] = 15
        ,[zoneplugin] = 'zone_200'
        ,[zoneip] = '109.236.88.106'
        ,[zoneport] = 19000
        ,[isinstance] = 0
        ,[enabled] = 1
        ,[spawnid] = 94
        ,[plantruleset] = 34
        ,[protected] = 0
        ,[raceid] = 1
        ,[width] = 2048
        ,[height] = 2048
        ,[terraformable] = 1
        ,[zonetype] = 2
        ,[sparkcost] = 3
        ,[maxdockingbase] = 2
        ,[sleeping] = 0
        ,[plantaltitudescale] = 1
        ,[host] = 'genxyHost-01'
        ,[active] = 1
        ,[timeLimitMinutes] = NULL
        ,[pbsTechLimit] = 1
        ,[PlantsGrowthTimerOverrideMin] = NULL

WHEN NOT MATCHED THEN
    INSERT (
         [id]
        ,[x]
        ,[y]
        ,[name]
        ,[description]
        ,[note]
        ,[fertility]
        ,[zoneplugin]
        ,[zoneip]
        ,[zoneport]
        ,[isinstance]
        ,[enabled]
        ,[spawnid]
        ,[plantruleset]
        ,[protected]
        ,[raceid]
        ,[width]
        ,[height]
        ,[terraformable]
        ,[zonetype]
        ,[sparkcost]
        ,[maxdockingbase]
        ,[sleeping]
        ,[plantaltitudescale]
        ,[host]
        ,[active]
        ,[timeLimitMinutes]
        ,[pbsTechLimit]
        ,[PlantsGrowthTimerOverrideMin])
    VALUES
        (@Id
        ,12800
        ,-1024
        ,'zone_delta_z200'
        ,'zone_delta_z200_desc'
        ,'delta_note'
        ,15
        ,'zone_200'
        ,'109.236.88.106'
        ,19000
        ,0
        ,1
        ,94
        ,34
        ,0
        ,1
        ,2048
        ,2048
        ,1
        ,2
        ,3
        ,2
        ,0
        ,1
        ,'genxyHost-01'
        ,1
        ,NULL
        ,1
        ,NULL);
GO

DECLARE @Id INT = 200;
DECLARE @storageDef AS INT = (SELECT TOP 1 definition FROM [dbo].[entitydefaults] WHERE [definitionname] = 'def_zone_storage');
DECLARE @ename NVARCHAR(128) = 'es_zone_'+CONVERT(varchar(10), @Id)+'_storage';

MERGE [dbo].[entities] AS Target
USING (SELECT @ename AS [ename]) AS Source
ON (Target.[ename] = Source.[ename])

WHEN MATCHED THEN
    UPDATE SET 
         [definition] = @storageDef
        ,[owner] = NULL
        ,[parent] = NULL
        ,[health] = 100
        ,[quantity] = 1
        ,[repackaged] = 0
        ,[dynprop] = NULL

WHEN NOT MATCHED THEN
    INSERT ([eid], [definition], [owner], [parent], [health], [ename], [quantity], [repackaged], [dynprop]) VALUES 
        (2100+@Id, @storageDef, NULL, NULL ,100, @ename, 1, 0, NULL);

DECLARE @eid AS INT =(SELECT TOP 1 eid FROM [dbo].[entities] WHERE [ename] = @ename)

MERGE [dbo].[entitystorage] AS Target
USING (SELECT @ename AS [storage_name]) AS Source
ON (Target.[storage_name] = Source.[storage_name])

WHEN MATCHED THEN
    UPDATE SET [eid] = @eid

WHEN NOT MATCHED THEN
    INSERT ([storage_name], [eid]) VALUES (@ename, @eid);
GO
