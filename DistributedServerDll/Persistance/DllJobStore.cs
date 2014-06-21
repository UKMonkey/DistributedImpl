using System;
using System.Collections.Generic;
using DistributedSharedInterfaces.Jobs;
using System.IO;
using System.Data.SQLite;
using System.Data;

namespace DistributedServerDll.Persistance
{
    public delegate IJobGroup JobGroupGenerator();

    public class DllJobGroupStore : IDisposable
    {
        private readonly String _fileName;
        private readonly String _connectionString;
        private readonly SQLiteConnection _connection;


        public IEnumerable<IJobGroup> GetStoredGroups(JobGroupGenerator generator)
        {
            var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT GroupId, JobDataSize, JobData, SupportingData FROM JobGroups WHERE Completed = 0";
            var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                IJobGroup grp = generator();
                grp.GroupId = reader.GetInt64(0);
                
                var size = reader.GetInt32(1);
                var buffer = new byte[size];
                reader.GetBytes(2, 0, buffer, 0, size);
                grp.Data = buffer;

                grp.SupportingDataVersion = reader.GetInt64(3);
                yield return grp;
            }

            cmd.Dispose();
        }


        public void RemoveGroup(IJobGroup group)
        {
            var cmd = _connection.CreateCommand();
            cmd.CommandText = string.Format("UPDATE JobGroups SET Completed = 1 WHERE GroupId = {0}", group.GroupId);

            cmd.ExecuteNonQuery();
            cmd.Dispose();
        }


        public byte[] GetStoredStatus()
        {
            var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT Size, Content FROM Status WHERE Key = (SELECT max(Key) from Status)";
            var reader = cmd.ExecuteReader();
            
            if (!reader.Read())
                return new byte[0];

            var size = reader.GetInt32(0);
            var buffer = new byte[size];
            reader.GetBytes(1, 0, buffer, 0, size);
            cmd.Dispose();
            return buffer;
        }


        public long GetStoredSupportingDataVersion()
        {
            var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT SupportingDataVersion FROM Status WHERE Key = (SELECT max(Key) from Status)";
            var reader = cmd.ExecuteReader();

            if (!reader.Read())
                return 0;

            return reader.GetInt64(0);
        }


        public void StoreNewGroup(IJobGroup group, byte[] status, long supportingDataVersion)
        {
            var cmd = _connection.CreateCommand();
            var transaction = _connection.BeginTransaction();

            cmd.CommandText = @"INSERT INTO JobGroups 
                                (GroupId, JobData, JobDataSize, SupportingData, Completed)
                               VALUES 
                                (@GrpId, @Data, @JobDataSize, @SupportingData, false)";
            cmd.Parameters.Add("@GrpId", DbType.Int64).Value = group.GroupId;
            cmd.Parameters.Add("@JobDataSize", DbType.Int32).Value = group.Data.Length;
            cmd.Parameters.Add("@Data", DbType.Binary, group.Data.Length).Value = group.Data;
            cmd.Parameters.Add("@SupportingData", DbType.Int64).Value = group.SupportingDataVersion;
            cmd.ExecuteNonQuery();

            cmd.Parameters.Clear();
            cmd.CommandText = "SELECT last_insert_rowid()";
            var reader = cmd.ExecuteReader();
            reader.Read();
            group.GroupId = reader.GetInt64(0);

            cmd.Parameters.Clear();
            cmd.CommandText = @"INSERT INTO Status (Content, Size, SupportingDataVersion) VALUES (@Content, @Size, @Version)";
            cmd.Parameters.Add("@Content", DbType.Binary, status.Length).Value = status;
            cmd.Parameters.Add("@Size", DbType.Int32, status.Length).Value = status.Length;
            cmd.Parameters.Add("@Version", DbType.Int64, status.Length).Value = supportingDataVersion;
            cmd.ExecuteNonQuery();

            transaction.Commit();
            cmd.Dispose();
        }


        public DllJobGroupStore(string saveDirectory, string dllName)
        {
            _fileName = saveDirectory + Path.DirectorySeparatorChar + dllName + ".s3db";
            var fileExists = File.Exists(_fileName);

            _connectionString = "Data Source=" + _fileName + "; Version=3;";
            _connection = new SQLiteConnection(_connectionString);
            _connection.Open();

            if (!fileExists)
                PrepareDatabase();
        }


        public void Dispose()
        {
            _connection.Close();
            _connection.Dispose();
        }


        private void PrepareDatabase()
        {
            const string statusCommand = @"CREATE TABLE IF NOT EXISTS Status (
                            Key INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                            Size INTEGER NOT NULL,
                            Content BLOB NOT NULL,
                            SupportingDataVersion INTEGER NOT NULL)";
            const string jobCommand = @"CREATE TABLE IF NOT EXISTS JobGroups (
                            GroupId INTEGER NOT NULL PRIMARY KEY,
                            JobData BLOB NOT NULL,
                            JobDataSize INTEGER NOT NULL,
                            SupportingData INTEGER NOT NULL,
                            Completed INTEGER NOT NULL)";
            const string indexCommand = @"CREATE INDEX IF NOT EXISTS JobGroupsCompleted ON JobGroups (Completed)";

            var cmd = _connection.CreateCommand();
            cmd.CommandText = statusCommand;
            cmd.ExecuteNonQuery();

            cmd.CommandText = jobCommand;
            cmd.ExecuteNonQuery();

            cmd.CommandText = indexCommand;
            cmd.ExecuteNonQuery();

            cmd.Dispose();
        }
    }
}
