// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.DependencyInjection;
using FluentMigrator.Runner;
using FluentMigrator.Runner.VersionTableInfo;
using FluentMigrator;
using System.Data.SQLite;

Console.WriteLine("Hello, World!");

const string databaseFile = "test1836.db";
const string connectionString = $"Data Source={databaseFile}";

if (File.Exists(databaseFile))
{
    File.Delete(databaseFile);
}

var services = new ServiceCollection();

// wrong
services.AddScoped<IVersionTableMetaData, CustomVersionMetaData>();

services.AddFluentMigratorCore()
    .ConfigureRunner(x =>
    {
        x.AddSQLite()
            .WithGlobalConnectionString(connectionString)
            .ScanIn(typeof(Program).Assembly).For.Migrations();
    });

// right
//services.AddScoped<IVersionTableMetaData, CustomVersionMetaData>();

var serviceProvider = services.BuildServiceProvider();

using (var scope = serviceProvider.CreateScope())
{
    var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
    runner.MigrateUp();
}

using (var connection = new SQLiteConnection(connectionString))
{
    connection.Open();

    var command = connection.CreateCommand();
    object defaultVersionInfoExists = TableExists(command, "VersionInfo");

    var customVersionInfoExists = TableExists(command, "CustomVersionInfo");

    Console.WriteLine($"Default VersionInfo table: {defaultVersionInfoExists}");
    Console.WriteLine($"Custom VersionInfo table: {customVersionInfoExists}");
}

static bool TableExists(SQLiteCommand command, string tableName)
{
    command.CommandText = $"SELECT 1 FROM sqlite_master WHERE type='table' AND name='{tableName}';";
    var defaultVersionInfoExists = (long?)command.ExecuteScalar();
    return defaultVersionInfoExists == 1;
}

public class CustomVersionMetaData : DefaultVersionTableMetaData
{
    public override string TableName => "CustomVersionInfo";
}

[Migration(20180430121800)]
public class AddLogTable : Migration
{
    public override void Up()
    {
        Create.Table("Log")
            .WithColumn("Id").AsInt64().PrimaryKey().Identity()
            .WithColumn("Text").AsString();
    }

    public override void Down()
    {
        Delete.Table("Log");
    }
}