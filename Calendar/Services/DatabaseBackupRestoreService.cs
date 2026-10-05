using Calendar.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.IO;

namespace Calendar.Services
{
    public static class DatabaseBackupRestoreService
    {
        // =====================================================
        // CREATE BACKUP
        // =====================================================

        public static async Task CreateBackupAsync(
            string backupPath)
        {
            if (string.IsNullOrWhiteSpace(backupPath))
            {
                throw new ArgumentException(
                    "Backup path is required.",
                    nameof(backupPath));
            }


            string fullPath =
                Path.GetFullPath(backupPath);


            string? directory =
                Path.GetDirectoryName(fullPath);


            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new InvalidOperationException(
                    "Invalid backup path.");
            }


            Directory.CreateDirectory(directory);


            DatabaseConnectionInfo connectionInfo =
                GetConnectionInfo();


            await using SqlConnection connection =
                new(connectionInfo.MasterConnectionString);


            await connection.OpenAsync();


            string databaseIdentifier =
                QuoteIdentifier(
                    connectionInfo.DatabaseName);


            await using SqlCommand command =
                connection.CreateCommand();


            command.CommandTimeout = 0;


            command.CommandText =
                $"""
                BACKUP DATABASE {databaseIdentifier}
                TO DISK = @BackupPath
                WITH
                    INIT,
                    COPY_ONLY,
                    COMPRESSION,
                    CHECKSUM,
                    STATS = 10;
                """;


            command.Parameters.Add(
                new SqlParameter(
                    "@BackupPath",
                    SqlDbType.NVarChar,
                    4000)
                {
                    Value = fullPath
                });


            await command.ExecuteNonQueryAsync();


            // =================================================
            // VERIFY BACKUP
            // =================================================

            await using SqlCommand verifyCommand =
                connection.CreateCommand();


            verifyCommand.CommandTimeout = 0;


            verifyCommand.CommandText =
                """
                RESTORE VERIFYONLY
                FROM DISK = @BackupPath
                WITH CHECKSUM;
                """;


            verifyCommand.Parameters.Add(
                new SqlParameter(
                    "@BackupPath",
                    SqlDbType.NVarChar,
                    4000)
                {
                    Value = fullPath
                });


            await verifyCommand.ExecuteNonQueryAsync();
        }


        // =====================================================
        // READ BACKUP INFORMATION
        // =====================================================

        public static async Task<DatabaseBackupInfo>
            GetBackupInfoAsync(
                string backupPath)
        {
            string fullPath =
                Path.GetFullPath(backupPath);


            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException(
                    "Backup file not found.",
                    fullPath);
            }


            DatabaseConnectionInfo connectionInfo =
                GetConnectionInfo();


            await using SqlConnection connection =
                new(connectionInfo.MasterConnectionString);


            await connection.OpenAsync();


            await using SqlCommand command =
                connection.CreateCommand();


            command.CommandTimeout = 0;


            command.CommandText =
                """
                RESTORE HEADERONLY
                FROM DISK = @BackupPath;
                """;


            command.Parameters.Add(
                new SqlParameter(
                    "@BackupPath",
                    SqlDbType.NVarChar,
                    4000)
                {
                    Value = fullPath
                });


            await using SqlDataReader reader =
                await command.ExecuteReaderAsync();


            if (!await reader.ReadAsync())
            {
                throw new InvalidOperationException(
                    "The selected file does not contain a valid SQL Server backup.");
            }


            string databaseName =
                reader["DatabaseName"]?.ToString() ??
                "";


            DateTime? backupStartDate =
                reader["BackupStartDate"] == DBNull.Value
                    ? null
                    : Convert.ToDateTime(
                        reader["BackupStartDate"]);


            DateTime? backupFinishDate =
                reader["BackupFinishDate"] == DBNull.Value
                    ? null
                    : Convert.ToDateTime(
                        reader["BackupFinishDate"]);


            long backupSize =
                reader["BackupSize"] == DBNull.Value
                    ? 0
                    : Convert.ToInt64(
                        reader["BackupSize"]);


            long compressedBackupSize =
                reader["CompressedBackupSize"] == DBNull.Value
                    ? 0
                    : Convert.ToInt64(
                        reader["CompressedBackupSize"]);


            return new DatabaseBackupInfo
            {
                DatabaseName =
                    databaseName,

                BackupStartDate =
                    backupStartDate,

                BackupFinishDate =
                    backupFinishDate,

                BackupSize =
                    backupSize,

                CompressedBackupSize =
                    compressedBackupSize
            };
        }


        // =====================================================
        // RESTORE BACKUP
        // =====================================================

        public static async Task RestoreBackupAsync(
            string backupPath)
        {
            string fullPath =
                Path.GetFullPath(backupPath);


            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException(
                    "Backup file not found.",
                    fullPath);
            }


            DatabaseConnectionInfo connectionInfo =
                GetConnectionInfo();


            // Eerst controleren of dit werkelijk een backup
            // van CentralCalendar is.
            DatabaseBackupInfo backupInfo =
                await GetBackupInfoAsync(
                    fullPath);


            if (!backupInfo.DatabaseName.Equals(
                connectionInfo.DatabaseName,
                StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"The backup contains database " +
                    $"'{backupInfo.DatabaseName}' instead of " +
                    $"'{connectionInfo.DatabaseName}'.");
            }


            // Alle bestaande SqlClient pools sluiten voordat
            // de database in SINGLE_USER gaat.
            SqlConnection.ClearAllPools();


            await using SqlConnection connection =
                new(connectionInfo.MasterConnectionString);


            await connection.OpenAsync();


            // =================================================
            // CURRENT DATABASE FILE LOCATIONS
            // =================================================

            DatabaseFileLocations currentFiles =
                await GetCurrentDatabaseFilesAsync(
                    connection,
                    connectionInfo.DatabaseName);


            // =================================================
            // LOGICAL FILE NAMES FROM BACKUP
            // =================================================

            BackupLogicalFiles backupFiles =
                await GetBackupLogicalFilesAsync(
                    connection,
                    fullPath);


            string databaseIdentifier =
                QuoteIdentifier(
                    connectionInfo.DatabaseName);


            string logicalDataName =
                EscapeSqlString(
                    backupFiles.DataLogicalName);


            string logicalLogName =
                EscapeSqlString(
                    backupFiles.LogLogicalName);


            string dataPath =
                EscapeSqlString(
                    currentFiles.DataFilePath);


            string logPath =
                EscapeSqlString(
                    currentFiles.LogFilePath);


            bool databaseIsSingleUser =
                false;


            try
            {
                // =============================================
                // SINGLE USER
                // =============================================

                await using (
                    SqlCommand singleUserCommand =
                        connection.CreateCommand())
                {
                    singleUserCommand.CommandTimeout =
                        0;


                    singleUserCommand.CommandText =
                        $"""
                        ALTER DATABASE {databaseIdentifier}
                        SET SINGLE_USER
                        WITH ROLLBACK IMMEDIATE;
                        """;


                    await singleUserCommand
                        .ExecuteNonQueryAsync();


                    databaseIsSingleUser =
                        true;
                }


                // =============================================
                // RESTORE
                //
                // MOVE zorgt ervoor dat een backup van een
                // andere pc/laptop toch naar de huidige SQL
                // data/log locaties wordt teruggezet.
                // =============================================

                await using SqlCommand restoreCommand =
                    connection.CreateCommand();


                restoreCommand.CommandTimeout =
                    0;


                restoreCommand.CommandText =
                    $"""
                    RESTORE DATABASE {databaseIdentifier}
                    FROM DISK = @BackupPath
                    WITH
                        REPLACE,
                        RECOVERY,
                        CHECKSUM,
                        MOVE N'{logicalDataName}'
                            TO N'{dataPath}',
                        MOVE N'{logicalLogName}'
                            TO N'{logPath}',
                        STATS = 10;
                    """;


                restoreCommand.Parameters.Add(
                    new SqlParameter(
                        "@BackupPath",
                        SqlDbType.NVarChar,
                        4000)
                    {
                        Value = fullPath
                    });


                await restoreCommand
                    .ExecuteNonQueryAsync();


                // =============================================
                // MULTI USER
                // =============================================

                await using SqlCommand multiUserCommand =
                    connection.CreateCommand();


                multiUserCommand.CommandTimeout =
                    0;


                multiUserCommand.CommandText =
                    $"""
                    ALTER DATABASE {databaseIdentifier}
                    SET MULTI_USER;
                    """;


                await multiUserCommand
                    .ExecuteNonQueryAsync();


                databaseIsSingleUser =
                    false;
            }
            finally
            {
                // Als restore ergens fout gaat, proberen we
                // altijd de database weer MULTI_USER te maken.
                if (databaseIsSingleUser)
                {
                    try
                    {
                        await using SqlCommand recoveryCommand =
                            connection.CreateCommand();


                        recoveryCommand.CommandTimeout =
                            0;


                        recoveryCommand.CommandText =
                            $"""
                            ALTER DATABASE {databaseIdentifier}
                            SET MULTI_USER
                            WITH ROLLBACK IMMEDIATE;
                            """;


                        await recoveryCommand
                            .ExecuteNonQueryAsync();
                    }
                    catch
                    {
                        // Originele restore-error behouden.
                    }
                }


                SqlConnection.ClearAllPools();
            }
        }


        // =====================================================
        // CURRENT DATABASE FILES
        // =====================================================

        private static async Task<DatabaseFileLocations>
            GetCurrentDatabaseFilesAsync(
                SqlConnection connection,
                string databaseName)
        {
            await using SqlCommand command =
                connection.CreateCommand();


            command.CommandText =
                """
                SELECT
                    type_desc,
                    physical_name
                FROM sys.master_files
                WHERE database_id = DB_ID(@DatabaseName)
                ORDER BY file_id;
                """;


            command.Parameters.Add(
                new SqlParameter(
                    "@DatabaseName",
                    SqlDbType.NVarChar,
                    128)
                {
                    Value = databaseName
                });


            string? dataFile =
                null;

            string? logFile =
                null;


            await using SqlDataReader reader =
                await command.ExecuteReaderAsync();


            while (await reader.ReadAsync())
            {
                string type =
                    reader.GetString(0);


                string physicalPath =
                    reader.GetString(1);


                if (type.Equals(
                    "ROWS",
                    StringComparison.OrdinalIgnoreCase))
                {
                    dataFile ??=
                        physicalPath;
                }
                else if (type.Equals(
                    "LOG",
                    StringComparison.OrdinalIgnoreCase))
                {
                    logFile ??=
                        physicalPath;
                }
            }


            if (string.IsNullOrWhiteSpace(dataFile) ||
                string.IsNullOrWhiteSpace(logFile))
            {
                throw new InvalidOperationException(
                    "Unable to determine the current database file locations.");
            }


            return new DatabaseFileLocations
            {
                DataFilePath =
                    dataFile,

                LogFilePath =
                    logFile
            };
        }


        // =====================================================
        // BACKUP LOGICAL FILES
        // =====================================================

        private static async Task<BackupLogicalFiles>
            GetBackupLogicalFilesAsync(
                SqlConnection connection,
                string backupPath)
        {
            await using SqlCommand command =
                connection.CreateCommand();


            command.CommandTimeout =
                0;


            command.CommandText =
                """
                RESTORE FILELISTONLY
                FROM DISK = @BackupPath;
                """;


            command.Parameters.Add(
                new SqlParameter(
                    "@BackupPath",
                    SqlDbType.NVarChar,
                    4000)
                {
                    Value = backupPath
                });


            string? dataLogicalName =
                null;

            string? logLogicalName =
                null;


            await using SqlDataReader reader =
                await command.ExecuteReaderAsync();


            int logicalNameOrdinal =
                reader.GetOrdinal(
                    "LogicalName");


            int typeOrdinal =
                reader.GetOrdinal(
                    "Type");


            while (await reader.ReadAsync())
            {
                string logicalName =
                    reader.GetString(
                        logicalNameOrdinal);


                string type =
                    reader.GetString(
                        typeOrdinal);


                if (type.Equals(
                    "D",
                    StringComparison.OrdinalIgnoreCase))
                {
                    dataLogicalName ??=
                        logicalName;
                }
                else if (type.Equals(
                    "L",
                    StringComparison.OrdinalIgnoreCase))
                {
                    logLogicalName ??=
                        logicalName;
                }
            }


            if (string.IsNullOrWhiteSpace(
                    dataLogicalName) ||
                string.IsNullOrWhiteSpace(
                    logLogicalName))
            {
                throw new InvalidOperationException(
                    "Unable to determine the logical files in the backup.");
            }


            return new BackupLogicalFiles
            {
                DataLogicalName =
                    dataLogicalName,

                LogLogicalName =
                    logLogicalName
            };
        }


        // =====================================================
        // CONNECTION
        // =====================================================

        private static DatabaseConnectionInfo
            GetConnectionInfo()
        {
            using CentralCalendarDbContext database =
                new();


            string connectionString =
                database.Database
                    .GetDbConnection()
                    .ConnectionString;


            SqlConnectionStringBuilder builder =
                new(connectionString);


            string databaseName =
                builder.InitialCatalog;


            if (string.IsNullOrWhiteSpace(
                databaseName))
            {
                throw new InvalidOperationException(
                    "Database name is missing from the connection string.");
            }


            builder.InitialCatalog =
                "master";


            return new DatabaseConnectionInfo
            {
                DatabaseName =
                    databaseName,

                MasterConnectionString =
                    builder.ConnectionString
            };
        }


        // =====================================================
        // SQL HELPERS
        // =====================================================

        private static string QuoteIdentifier(
            string identifier)
        {
            return
                "[" +
                identifier.Replace(
                    "]",
                    "]]") +
                "]";
        }


        private static string EscapeSqlString(
            string value)
        {
            return value.Replace(
                "'",
                "''");
        }


        // =====================================================
        // INTERNAL MODELS
        // =====================================================

        private sealed class DatabaseConnectionInfo
        {
            public string DatabaseName { get; set; } =
                "";

            public string MasterConnectionString { get; set; } =
                "";
        }


        private sealed class DatabaseFileLocations
        {
            public string DataFilePath { get; set; } =
                "";

            public string LogFilePath { get; set; } =
                "";
        }


        private sealed class BackupLogicalFiles
        {
            public string DataLogicalName { get; set; } =
                "";

            public string LogLogicalName { get; set; } =
                "";
        }
    }


    public sealed class DatabaseBackupInfo
    {
        public string DatabaseName { get; set; } =
            "";

        public DateTime? BackupStartDate { get; set; }

        public DateTime? BackupFinishDate { get; set; }

        public long BackupSize { get; set; }

        public long CompressedBackupSize { get; set; }


        public double CompressedSizeMegabytes =>
            Math.Round(
                CompressedBackupSize /
                1024d /
                1024d,
                2);
    }
}