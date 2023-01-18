﻿// <auto-generated />
using System;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata;

#pragma warning disable 219, 612, 618
#nullable enable

namespace WebScheduler.DataMigrations.CompiledModels
{
    internal partial class OrleansStorageEntityType
    {
        public static RuntimeEntityType Create(RuntimeModel model, RuntimeEntityType? baseEntityType = null)
        {
            var runtimeEntityType = model.AddEntityType(
                "WebScheduler.DataMigrations.OrleansStorage",
                typeof(OrleansStorage),
                baseEntityType);

            var id = runtimeEntityType.AddProperty(
                "Id",
                typeof(long),
                propertyInfo: typeof(OrleansStorage).GetProperty("Id", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(OrleansStorage).GetField("<Id>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                afterSaveBehavior: PropertySaveBehavior.Throw);
            id.AddAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            var grainIdHash = runtimeEntityType.AddProperty(
                "GrainIdHash",
                typeof(int),
                propertyInfo: typeof(OrleansStorage).GetProperty("GrainIdHash", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(OrleansStorage).GetField("<GrainIdHash>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                afterSaveBehavior: PropertySaveBehavior.Throw);

            var grainTypeHash = runtimeEntityType.AddProperty(
                "GrainTypeHash",
                typeof(int),
                propertyInfo: typeof(OrleansStorage).GetProperty("GrainTypeHash", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(OrleansStorage).GetField("<GrainTypeHash>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                afterSaveBehavior: PropertySaveBehavior.Throw);

            var grainIdExtensionString = runtimeEntityType.AddProperty(
                "GrainIdExtensionString",
                typeof(string),
                propertyInfo: typeof(OrleansStorage).GetProperty("GrainIdExtensionString", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(OrleansStorage).GetField("<GrainIdExtensionString>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                nullable: true,
                maxLength: 512);
            grainIdExtensionString.AddAnnotation("MySql:CharSet", "utf8");

            var grainIdN0 = runtimeEntityType.AddProperty(
                "GrainIdN0",
                typeof(long),
                propertyInfo: typeof(OrleansStorage).GetProperty("GrainIdN0", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(OrleansStorage).GetField("<GrainIdN0>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly));

            var grainIdN1 = runtimeEntityType.AddProperty(
                "GrainIdN1",
                typeof(long),
                propertyInfo: typeof(OrleansStorage).GetProperty("GrainIdN1", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(OrleansStorage).GetField("<GrainIdN1>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly));

            var grainTypeString = runtimeEntityType.AddProperty(
                "GrainTypeString",
                typeof(string),
                propertyInfo: typeof(OrleansStorage).GetProperty("GrainTypeString", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(OrleansStorage).GetField("<GrainTypeString>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                maxLength: 512);
            grainTypeString.AddAnnotation("MySql:CharSet", "utf8");

            var isScheduledTaskDeleted = runtimeEntityType.AddProperty(
                "IsScheduledTaskDeleted",
                typeof(bool?),
                propertyInfo: typeof(OrleansStorage).GetProperty("IsScheduledTaskDeleted", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(OrleansStorage).GetField("<IsScheduledTaskDeleted>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                nullable: true,
                valueGenerated: ValueGenerated.OnAddOrUpdate,
                beforeSaveBehavior: PropertySaveBehavior.Ignore,
                afterSaveBehavior: PropertySaveBehavior.Ignore);
            isScheduledTaskDeleted.AddAnnotation("Relational:ColumnType", "bit");
            isScheduledTaskDeleted.AddAnnotation("Relational:ComputedColumnSql", "CASE WHEN GrainTypeHash = 2108290596 THEN\r\n    CASE WHEN JSON_EXTRACT(PayloadJson, \"$.isDeleted\") IS NOT NULL THEN \r\n        true\r\n    ELSE \r\n        false\r\n    END \r\nEND");
            isScheduledTaskDeleted.AddAnnotation("Relational:IsStored", true);

            var isScheduledTaskEnabled = runtimeEntityType.AddProperty(
                "IsScheduledTaskEnabled",
                typeof(bool?),
                propertyInfo: typeof(OrleansStorage).GetProperty("IsScheduledTaskEnabled", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(OrleansStorage).GetField("<IsScheduledTaskEnabled>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                nullable: true,
                valueGenerated: ValueGenerated.OnAddOrUpdate,
                beforeSaveBehavior: PropertySaveBehavior.Ignore,
                afterSaveBehavior: PropertySaveBehavior.Ignore);
            isScheduledTaskEnabled.AddAnnotation("Relational:ColumnType", "bit");
            isScheduledTaskEnabled.AddAnnotation("Relational:ComputedColumnSql", "CASE WHEN GrainTypeHash = 2108290596 THEN\r\n    CASE WHEN JSON_EXTRACT(PayloadJson, \"$.task.isEnabled\") IS NOT NULL THEN \r\n        true\r\n    ELSE \r\n        false\r\n    END \r\nEND");
            isScheduledTaskEnabled.AddAnnotation("Relational:IsStored", true);

            var modifiedOn = runtimeEntityType.AddProperty(
                "ModifiedOn",
                typeof(DateTime),
                propertyInfo: typeof(OrleansStorage).GetProperty("ModifiedOn", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(OrleansStorage).GetField("<ModifiedOn>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly));
            modifiedOn.AddAnnotation("Relational:ColumnType", "datetime");

            var payloadBinary = runtimeEntityType.AddProperty(
                "PayloadBinary",
                typeof(byte[]),
                propertyInfo: typeof(OrleansStorage).GetProperty("PayloadBinary", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(OrleansStorage).GetField("<PayloadBinary>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                nullable: true);
            payloadBinary.AddAnnotation("Relational:ColumnType", "blob");

            var payloadJson = runtimeEntityType.AddProperty(
                "PayloadJson",
                typeof(string),
                propertyInfo: typeof(OrleansStorage).GetProperty("PayloadJson", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(OrleansStorage).GetField("<PayloadJson>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                nullable: true);
            payloadJson.AddAnnotation("Relational:ColumnType", "json");

            var payloadXml = runtimeEntityType.AddProperty(
                "PayloadXml",
                typeof(string),
                propertyInfo: typeof(OrleansStorage).GetProperty("PayloadXml", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(OrleansStorage).GetField("<PayloadXml>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                nullable: true);

            var serviceId = runtimeEntityType.AddProperty(
                "ServiceId",
                typeof(string),
                propertyInfo: typeof(OrleansStorage).GetProperty("ServiceId", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(OrleansStorage).GetField("<ServiceId>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                maxLength: 150);
            serviceId.AddAnnotation("MySql:CharSet", "utf8");

            var tenantId = runtimeEntityType.AddProperty(
                "TenantId",
                typeof(string),
                propertyInfo: typeof(OrleansStorage).GetProperty("TenantId", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(OrleansStorage).GetField("<TenantId>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                nullable: true,
                valueGenerated: ValueGenerated.OnAddOrUpdate,
                beforeSaveBehavior: PropertySaveBehavior.Ignore,
                afterSaveBehavior: PropertySaveBehavior.Ignore,
                maxLength: 255);
            tenantId.AddAnnotation("MySql:CharSet", "utf8");
            tenantId.AddAnnotation("Relational:ComputedColumnSql", "CASE WHEN GrainTypeHash = 2108290596 THEN\r\n    CASE WHEN JSON_UNQUOTE(JSON_EXTRACT(PayloadJson, \"$.tenantId\")) IS NOT NULL THEN \r\n        JSON_UNQUOTE(JSON_EXTRACT(PayloadJson, \"$.tenantId\"))\r\n    ELSE \r\n        JSON_UNQUOTE(JSON_EXTRACT(PayloadJson, \"$.tenantIdString\"))\r\n    END \r\nEND");
            tenantId.AddAnnotation("Relational:IsStored", true);

            var version = runtimeEntityType.AddProperty(
                "Version",
                typeof(int?),
                propertyInfo: typeof(OrleansStorage).GetProperty("Version", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(OrleansStorage).GetField("<Version>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                nullable: true);

            var key = runtimeEntityType.AddKey(
                new[] { id, grainIdHash, grainTypeHash });
            runtimeEntityType.SetPrimaryKey(key);
            key.AddAnnotation("Relational:Name", "PRIMARY");

            var index = runtimeEntityType.AddIndex(
                new[] { isScheduledTaskDeleted });
            index.AddAnnotation("Relational:Name", "IX_OrleansStorage_ScheduledTaskState_IsScheduledTaskDeleted");

            var index0 = runtimeEntityType.AddIndex(
                new[] { isScheduledTaskEnabled });
            index0.AddAnnotation("Relational:Name", "IX_OrleansStorage_ScheduledTaskState_IsScheduledTaskEnabled");

            var index1 = runtimeEntityType.AddIndex(
                new[] { tenantId });
            index1.AddAnnotation("Relational:Name", "IX_OrleansStorage_ScheduledTaskState_TenantId");

            var index2 = runtimeEntityType.AddIndex(
                new[] { grainIdHash, grainTypeHash });
            index2.AddAnnotation("Relational:Name", "IX_OrleansStorage");

            return runtimeEntityType;
        }

        public static void CreateAnnotations(RuntimeEntityType runtimeEntityType)
        {
            runtimeEntityType.AddAnnotation("Relational:FunctionName", null);
            runtimeEntityType.AddAnnotation("Relational:Schema", null);
            runtimeEntityType.AddAnnotation("Relational:SqlQuery", null);
            runtimeEntityType.AddAnnotation("Relational:TableName", "OrleansStorage");
            runtimeEntityType.AddAnnotation("Relational:ViewName", null);
            runtimeEntityType.AddAnnotation("Relational:ViewSchema", null);

            Customize(runtimeEntityType);
        }

        static partial void Customize(RuntimeEntityType runtimeEntityType);
    }
}
