using Microsoft.Extensions.DependencyInjection;
using FluentMigrator.Runner;
using FluentMigrator.Runner.VersionTableInfo;
using FluentMigrator;
using System.Data.SQLite;
using Microsoft.Extensions.DependencyInjection.Extensions;

Console.WriteLine("FluentMigrator 1836 repo");
Console.WriteLine("---------------------");

const string databaseFile = "test1836.db";
const string connectionString = $"Data Source={databaseFile}";

if (File.Exists(databaseFile))
{
    File.Delete(databaseFile);
}

var services = new ServiceCollection();

Console.WriteLine("Repo of undocumented but mandatory DI registration order (before or after 'AddFluentMigratorCore()').");
Console.WriteLine("Should register custom VersionInfo table too early (wrong)? (Enter 'y' for 'yes', any key for 'no'):");
var registerTooEarly = Console.ReadKey().Key == ConsoleKey.Y;
Console.WriteLine();

// 1836 registration issue repo
if (registerTooEarly)
{
    // wrong - registered too early
    services.AddScoped<IVersionTableMetaData, CustomVersionMetaData>();
}

services.AddFluentMigratorCore()
    .ConfigureRunner(x =>
    {
        x.AddSQLite()
            .WithGlobalConnectionString(connectionString)
            .ScanIn(typeof(Program).Assembly).For.Migrations();
    });

if (!registerTooEarly)
{
    // right - registered after "AddFluentMigratorCore()"
    services.AddScoped<IVersionTableMetaData, CustomVersionMetaData>();
}


// "Replace"-question demo
// Replacing a service is the brute force way to ensure an implementation is used.
// Adding an implementation only if non is yet found is more graceful.
// Here's a demo of only registering a service if it is not yet registered in service collection.
// Add first service - in *user's* code.
services.AddScoped<ISomeLibraryInterface, UserImplementation>();

// A library (like FluentMigrator) only tries to add a default implementation if it was not yet registered before.
// This way it does not matter if user registers before or after the libary because user's registration takes priority.
services.TryAddScoped<ISomeLibraryInterface, DefaultLibraryImplementation>();

var serviceProvider = services.BuildServiceProvider();

// (Custom)VersionInfo table repo
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

    Console.WriteLine($"Registered user override too early: {registerTooEarly}");
    Console.WriteLine($"Default VersionInfo table found (wrong): {defaultVersionInfoExists}");
    Console.WriteLine($"Custom VersionInfo table found (right): {customVersionInfoExists}");
}

// DI registration demo
using (var scope = serviceProvider.CreateScope())
{
    var test = scope.ServiceProvider.GetRequiredService<ISomeLibraryInterface>();
    
    Console.WriteLine();
    Console.WriteLine("---------------------");
    Console.WriteLine("Give user DI registration priority");
    Console.WriteLine("Demo of giving user registered services priority by using 'TryAdd...' in library code.");
    Console.WriteLine("Resolved value should be 'UserImplementation', although registered before library's registrations.");
    Console.WriteLine($"Resolved instance type: {test.GetType().Name}");
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

public interface ISomeLibraryInterface
{
}

public class DefaultLibraryImplementation : ISomeLibraryInterface
{

}

public class UserImplementation : ISomeLibraryInterface
{

}