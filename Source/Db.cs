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

        private const string SQL_TABLE_SESSION2 = @"CREATE TABLE [Sessions] ( 
    Id          INTEGER          PRIMARY KEY AUTOINCREMENT,
    Guid        VARCHAR( 36 )    NOT NULL,
    HttpHost    VARCHAR( 1024 ),
    HttpMethods VARCHAR( 100 ) ,
    DataSize BIGINT,
    TimestampFirstPacket DATETIME,
    TimestampLastPacket DATETIME,
    SourceIp BIGINT,
    SourcePort INT,
    SourceCountry VARCHAR(2),
    DestinationIp BIGINT,
    DestinationPort INT,
    DestinationCountry VARCHAR(2)
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
        ///// </summary>
        ///// <param name="fileName"></param>
        ///// <returns></returns>
        //public static string CreateDatabase2(string fileName)
        //{
        //    try
        //    {
        //        //using (System.Data.SQLite.d sqlCeEngine = new SqlCeEngine(GetConnectionString(fileName)))
        //        //{
        //            //sqlCeEngine.CreateDatabase();

        //        System.Data.SQLite.SQLiteConnection.CreateFile(Path.Combine(fileName, Global.DB_FILE));
        //            using (System.Data.SQLite.SQLiteConnection connection = new System.Data.SQLite.SQLiteConnection(GetConnectionString2(fileName)))
        //            {
        //                connection.Open();

        //                using (System.Data.SQLite.SQLiteCommand command = new System.Data.SQLite.SQLiteCommand(SQL_TABLE_SESSION2, connection))
        //                {
        //                    command.ExecuteNonQuery();
        //                }

        //                //using (System.Data.SQLite.SQLiteCommand command = new System.Data.SQLite.SQLiteCommand(SQL_TABLE_SESSION_PK, connection))
        //                //{
        //                //    command.ExecuteNonQuery();
        //                //}
        //            }
        //        //}

        //        return string.Empty;
        //    }
        //    catch (Exception ex)
        //    {
        //        return ex.Message;
        //    }
        //}

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <returns></returns>
        //public static string GetConnectionString2(string outputPath)
        //{
        //    return string.Format("Data Source=\"{0}\";Version=3;", Path.Combine(outputPath, Global.DB_FILE));
        //}

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static string GetConnectionString(string outputPath)
        {
            return string.Format("DataSource=\"{0}\"; Max Database Size=4091", Path.Combine(outputPath, Global.DB_FILE));
        }

        //

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <returns></returns>
        //public static DbConnection GetOpenConnection2(string outputPath)
        //{
        //    var connection = new System.Data.SQLite.SQLiteConnection(GetConnectionString2(outputPath));
        //    connection.Open();

        //    return connection;
        //}

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
