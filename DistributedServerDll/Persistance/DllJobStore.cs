using System;
using System.Collections.Generic;
using DistributedSharedInterfaces.Jobs;
using System.IO;
using System.Data.SQLite;
using System.Data;
using DistributedShared.Jobs;

namespace DistributedServerDll.Persistance
{
    public delegate IJobGroup JobGroupGenerator();

    public class DllJobStore : IDisposable
    {
        private readonly String _fileName;
        private readonly String _dllName;
        private readonly String _connectionString;
        private readonly SQLiteConnection _connection;


        public DllJobStore(string saveDirectory, string dllName)
        {
            _dllName = dllName;
            _fileName = saveDirectory + Path.DirectorySeparatorChar + dllName + ".s3db";
            var fileExists = File.Exists(_fileName);

            Directory.CreateDirectory(saveDirectory);

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


        /// <summary>
        /// Returns any groups that have not been expanded
        /// </summary>
        /// <returns></returns>
        public IEnumerable<WrappedJobGroup> GetStoredGroups()
        {
            var cmd = _connection.CreateCommand();
            cmd.CommandText = @"SELECT JobGroups.GroupId, JobGroups.JobDataSize, JobGroups.JobData, JobGroups.SupportingData, JobGroups.JobCount FROM JobGroups 
                                    LEFT JOIN Jobs ON (JobGroups.GroupId = Jobs.GroupId)
                                WHERE Jobs.JobId IS NULL";
            var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var grp = new WrappedJobGroup();
                
                grp.GroupId = reader.GetInt64(0);
                
                var size = reader.GetInt32(1);
                var buffer = new byte[size];
                reader.GetBytes(2, 0, buffer, 0, size);

                grp.Data = buffer;
                grp.SupportingDataVersion = reader.GetInt64(3);
                grp.JobCount = reader.GetInt32(4);
                grp.DllName = _dllName;
                yield return grp;
            }

            cmd.Dispose();
            reader.Dispose();
        }


        /// <summary>
        /// Returns any jobs that have been expanded from their job group
        /// but not yet completed
        /// </summary>
        /// <returns></returns>
        public IEnumerable<WrappedJobData> GetStoredJobs()
        {
            var cmd = _connection.CreateCommand();
            cmd.CommandText = @"SELECT Jobs.GroupId, Jobs.JobId, Jobs.JobDataSize, Jobs.JobData, JobGroups.SupportingData FROM Jobs
                                    LEFT JOIN JobResults ON (Jobs.GroupId = JobResults.GroupId AND Jobs.JobId = JobResults.JobId)
                                    LEFT JOIN JobGroups ON (Jobs.GroupId = JobGroups.GroupId)
                                WHERE JobResults.JobId IS NULL";
            var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var job = new WrappedJobData();
                job.DllName = _dllName;
                job.GroupId = reader.GetInt64(0);
                job.JobId = reader.GetInt64(1);

                var size = reader.GetInt32(2);
                var buffer = new byte[size];
                reader.GetBytes(3, 0, buffer, 0, size);
                job.Data = buffer;

                job.SupportingDataVersion = reader.GetInt64(4);
                yield return job;
            }

            cmd.Dispose();
            reader.Dispose();
        }


        public Dictionary<String, byte[]> GetStoredStatus()
        {
            var ret = new Dictionary<String, byte[]>();
            ret[ReservedKeys.Version] = BitConverter.GetBytes((long)0);

            var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT Key, Size, Value FROM Status";
            var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var key = reader.GetString(0);
                var size = reader.GetInt32(1);

                var buffer = new byte[size];
                reader.GetBytes(2, 0, buffer, 0, size);

                ret[key] = buffer;
            }

            cmd.Dispose();
            reader.Dispose();
            return ret;
        }


        public Dictionary<String, byte[]> GetStoredSupportingData()
        {
            var ret = new Dictionary<String, byte[]>();
            ret[ReservedKeys.Version] = BitConverter.GetBytes((long)0);

            var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT Key, Size, Value FROM SupportingData";
            var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var key = reader.GetString(0);
                var size = reader.GetInt32(1);

                var buffer = new byte[size];
                reader.GetBytes(1, 0, buffer, 0, size);

                ret[key] = buffer;
            }

            cmd.Dispose();
            reader.Dispose();
            return ret;
        }


        public void StoreNewGroup(WrappedJobGroup group, Dictionary<String, byte[]> status, Dictionary<String, byte[]> supportingData)
        {
            var cmd = _connection.CreateCommand();
            var transaction = _connection.BeginTransaction();

            cmd.CommandText = @"INSERT INTO JobGroups 
                                (JobData, JobDataSize, JobCount, SupportingData)
                               VALUES 
                                (@Data, @JobDataSize, @JobCount, @SupportingData)";
            cmd.Parameters.Add("@JobDataSize", DbType.Int32).Value = group.Data.Length;
            cmd.Parameters.Add("@Data", DbType.Binary, group.Data.Length).Value = group.Data;
            cmd.Parameters.Add("@JobCount", DbType.Int32).Value = group.JobCount;
            cmd.Parameters.Add("@SupportingData", DbType.Int64).Value = group.SupportingDataVersion;
            cmd.ExecuteNonQuery();

            cmd.Parameters.Clear();
            cmd.CommandText = "SELECT last_insert_rowid()";
            var reader = cmd.ExecuteReader();
            reader.Read();
            group.GroupId = reader.GetInt64(0);
            reader.Dispose();

            foreach (var statusPair in status)
            {
                cmd.Parameters.Clear();
                cmd.CommandText = @"REPLACE INTO Status (Key, Value, Size) VALUES (@Key, @Value, @Size)";
                cmd.Parameters.Add("@Key", DbType.String).Value = statusPair.Key;
                cmd.Parameters.Add("@Value", DbType.Binary, statusPair.Value.Length).Value = statusPair.Value;
                cmd.Parameters.Add("@Size", DbType.Int64).Value = statusPair.Value.Length;
                cmd.ExecuteNonQuery();
            }

            foreach (var supportingDataPair in supportingData)
            {
                cmd.Parameters.Clear();
                cmd.CommandText = @"REPLACE INTO SupportingData (Key, Value, Size) VALUES (@Key, @Value, @Size)";
                cmd.Parameters.Add("@Key", DbType.String).Value = supportingDataPair.Key;
                cmd.Parameters.Add("@Value", DbType.Binary, supportingDataPair.Value.Length).Value = supportingDataPair.Value;
                cmd.Parameters.Add("@Size", DbType.Int64).Value = supportingDataPair.Value.Length;
                cmd.ExecuteNonQuery();
            }

            transaction.Commit();
            transaction.Dispose();
            cmd.Dispose();
        }


        public void StoreGroupData(WrappedJobGroup group, List<WrappedJobData> jobs)
        {
            var cmd = _connection.CreateCommand();
            var transaction = _connection.BeginTransaction();
            var jobId = 0;

            foreach (var job in jobs)
            {
                cmd.Parameters.Clear();
                cmd.CommandText = @"INSERT INTO Jobs (GroupId, JobId, JobData, JobDataSize) VALUES (@GroupId, @JobId, @Data, @DataSize)";
                cmd.Parameters.Add("@GroupId", DbType.Int64).Value = group.GroupId;
                cmd.Parameters.Add("@JobId", DbType.Int64).Value = job.JobId = jobId++;
                cmd.Parameters.Add("@Data", DbType.Binary, job.Data.Length).Value = job.Data;
                cmd.Parameters.Add("@DataSize", DbType.Int32).Value = job.Data.Length;
                cmd.ExecuteNonQuery();
            }

            transaction.Commit();
            transaction.Dispose();
            cmd.Dispose();
        }


        private void PrepareDatabase()
        {
            const string statusCommand = @"CREATE TABLE IF NOT EXISTS Status (
                            Key VARCHAR NOT NULL,
                            Value BLOB NOT NULL,
                            Size INTEGER NOT NULL,
                            PRIMARY KEY(Key));";
            const string supportingCommand = @"CREATE TABLE IF NOT EXISTS SupportingData (
                            Key VARCHAR NOT NULL,
                            Value BLOB NOT NULL,
                            Size INTEGER NOT NULL,
                            PRIMARY KEY(Key));";
            const string jobGroupCommand = @"CREATE TABLE IF NOT EXISTS JobGroups (
                            GroupId INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                            JobData BLOB NOT NULL,
                            JobDataSize INTEGER NOT NULL,
                            JobCount INTEGER NOT NULL,
                            SupportingData INTEGER NOT NULL);";
            const string jobCommand = @"CREATE TABLE IF NOT EXISTS Jobs (
                            GroupId INTEGER NOT NULL,
                            JobId INTEGER NOT NULL,
                            JobData BLOB NOT NULL,
                            JobDataSize INTEGER NOT NULL,
                            PRIMARY KEY(GroupId, JobId));";
            const string resultsCommand = @"CREATE TABLE IF NOT EXISTS JobResults (
                            GroupId INTEGER NOT NULL,
                            JobId INTEGER NOT NULL,
                            ResultData BLOB NOT NULL,
                            ResultDataSize INTEGER NOT NULL,
                            PRIMARY KEY(GroupId, JobId));";

            var cmd = _connection.CreateCommand();
            cmd.CommandText = statusCommand;
            cmd.ExecuteNonQuery();

            cmd.CommandText = supportingCommand;
            cmd.ExecuteNonQuery();

            cmd.CommandText = jobGroupCommand;
            cmd.ExecuteNonQuery();

            cmd.CommandText = jobCommand;
            cmd.ExecuteNonQuery();

            cmd.CommandText = resultsCommand;
            cmd.ExecuteNonQuery();

            cmd.Dispose();
        }
    }
}
