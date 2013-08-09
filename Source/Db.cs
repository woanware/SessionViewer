using System;
using System.Data.Common;
using System.Data.SqlServerCe;
using System.IO;

namespace SessionViewer
{

    /// <summary>
    /// Used for creating the SQL CE session database, creating new database connections etc
    /// </summary>
    public class Db
    {
        #region SQL_TABLE_SESSION
        private const string SQL_TABLE_SESSION = @"CREATE TABLE [Sessions] (
  [Id] bigint NOT NULL IDENTITY (1,1)
, [Guid] nvarchar(36)
, [HttpHost] nvarchar(1024)
, [HttpMethods] nvarchar(100)
, [IsGzipped] bit
, [IsChunked] bit
, [DataSize] bigint NULL
, [TimestampFirstPacket] datetime NULL
, [TimestampLastPacket] datetime NULL
, [SourceIp] bigint NULL
, [SourcePort] int NULL
, [SourceCountry] nvarchar(2) NULL
, [DestinationIp] bigint NULL
, [DestinationPort] int NULL
, [DestinationCountry] nvarchar(2) NULL
);";
        #endregion

        #region SQL_TABLE_SESSION_PK
        private const string SQL_TABLE_SESSION_PK = @"ALTER TABLE [Sessions] ADD CONSTRAINT [Id_Session] PRIMARY KEY ([Id]);";
        #endregion

        #region Public Methods
        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static string CreateDatabase(string fileName)
        {
            try
            {
                using (SqlCeEngine sqlCeEngine = new SqlCeEngine(GetConnectionString(fileName)))
                {
                    sqlCeEngine.CreateDatabase();

                    using (SqlCeConnection connection = new SqlCeConnection(GetConnectionString(fileName)))
                    {
                        connection.Open();

                        using (SqlCeCommand command = new SqlCeCommand(SQL_TABLE_SESSION, connection))
                        {
                            command.ExecuteNonQuery();
                        }

                        using (SqlCeCommand command = new SqlCeCommand(SQL_TABLE_SESSION_PK, connection))
                        {
                            command.ExecuteNonQuery();
                        }
                    }
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static string GetConnectionString(string outputPath)
        {
            return string.Format("DataSource=\"{0}\"; Max Database Size=4091", Path.Combine(outputPath, Global.DB_FILE));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static DbConnection GetOpenConnection(string outputPath)
        {
            var connection = new SqlCeConnection(GetConnectionString(outputPath));
            connection.Open();

            return connection;
        }
        #endregion
    }
}
