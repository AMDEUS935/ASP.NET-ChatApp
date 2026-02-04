using Microsoft.Data.Sqlite;
using System;
using System.IO;

if (args.Length == 0)
{
    Console.WriteLine("Usage: dotnet run -- <path-to-app.db> [--delete-profiles]");
    return 1;
}

var dbPath = args[0];
var deleteProfiles = args.Length > 1 && args[1] == "--delete-profiles";

if (!File.Exists(dbPath))
{
    Console.WriteLine($"DB not found: {dbPath}");
    return 1;
}

var connString = new SqliteConnectionStringBuilder { DataSource = dbPath }.ToString();
using var conn = new SqliteConnection(connString);
conn.Open();
using var tx = conn.BeginTransaction();
try
{
    // Gather user ids
    var userIds = new System.Collections.Generic.List<string>();
    using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = "SELECT Id FROM AspNetUsers";
        using var r = cmd.ExecuteReader();
        while (r.Read()) userIds.Add(r.GetString(0));
    }

    if (userIds.Count == 0)
    {
        Console.WriteLine("No users found.");
        tx.Commit();
        return 0;
    }

    Console.WriteLine($"Found {userIds.Count} users. Deleting related records...");

    // Helper to execute with parameterized IN
    void ExecIn(string table, string col)
    {
        using var cmd = conn.CreateCommand();
        var idx = 0;
        var parms = new System.Text.StringBuilder();
        foreach (var id in userIds)
        {
            var pname = "@p" + idx++;
            cmd.Parameters.AddWithValue(pname, id);
            if (parms.Length > 0) parms.Append(",");
            parms.Append(pname);
        }
        cmd.CommandText = $"DELETE FROM {table} WHERE {col} IN ({parms})";
        var affected = cmd.ExecuteNonQuery();
        Console.WriteLine($"Deleted {affected} rows from {table}");
    }

    // Delete mappings referencing users
    ExecIn("AspNetUserRoles", "UserId");
    ExecIn("AspNetUserClaims", "UserId");
    ExecIn("AspNetUserLogins", "UserId");
    ExecIn("AspNetUserTokens", "UserId");

    // If there is a UserConnections table with UserId
    try { ExecIn("UserConnections", "UserId"); } catch { }

    // Finally delete users
    ExecIn("AspNetUsers", "Id");

    tx.Commit();
    Console.WriteLine("User data cleared successfully.");

    if (deleteProfiles)
    {
        var webRoot = Path.Combine(Directory.GetParent(dbPath)!.FullName, "wwwroot");
        var profilesDir = Path.Combine(webRoot, "profiles");
        if (Directory.Exists(profilesDir))
        {
            foreach (var f in Directory.EnumerateFiles(profilesDir))
            {
                try { File.Delete(f); } catch { }
            }
            Console.WriteLine("Cleared files under wwwroot/profiles");
        }
    }

    return 0;
}
catch (Exception ex)
{
    Console.WriteLine("Error: " + ex.Message);
    try { tx.Rollback(); } catch { }
    return 2;
}