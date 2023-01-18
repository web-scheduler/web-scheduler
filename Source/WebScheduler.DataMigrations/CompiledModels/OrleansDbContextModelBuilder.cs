﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

#pragma warning disable 219, 612, 618
#nullable enable

namespace WebScheduler.DataMigrations.CompiledModels
{
    public partial class OrleansDbContextModel
    {
        partial void Initialize()
        {
            var orleansMembershipTable = OrleansMembershipTableEntityType.Create(this);
            var orleansMembershipVersionTable = OrleansMembershipVersionTableEntityType.Create(this);
            var orleansQuery = OrleansQueryEntityType.Create(this);
            var orleansRemindersTable = OrleansRemindersTableEntityType.Create(this);
            var orleansStorage = OrleansStorageEntityType.Create(this);

            OrleansMembershipTableEntityType.CreateForeignKey1(orleansMembershipTable, orleansMembershipVersionTable);

            OrleansMembershipTableEntityType.CreateAnnotations(orleansMembershipTable);
            OrleansMembershipVersionTableEntityType.CreateAnnotations(orleansMembershipVersionTable);
            OrleansQueryEntityType.CreateAnnotations(orleansQuery);
            OrleansRemindersTableEntityType.CreateAnnotations(orleansRemindersTable);
            OrleansStorageEntityType.CreateAnnotations(orleansStorage);

            AddAnnotation("ProductVersion", "7.0.2");
            AddAnnotation("Relational:MaxIdentifierLength", 64);
        }
    }
}