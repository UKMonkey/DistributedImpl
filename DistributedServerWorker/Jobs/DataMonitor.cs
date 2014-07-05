using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DistributedSharedInterfaces.Jobs;

namespace DistributedServerWorker.Jobs
{
    public class DataMonitor
    {
        private readonly Dictionary<String, byte[]> _changedData;
        public long CurrentVersion { get; private set; }


        public DataMonitor(Dictionary<String, byte[]> _starting)
        {
            _changedData = new Dictionary<String, byte[]>();
            if (_starting.ContainsKey(ReservedKeys.Version))
                CurrentVersion = BitConverter.ToInt64(_starting[ReservedKeys.Version], 0);
            else
                CurrentVersion = 0;
        }


        public void DataChanged(String key, byte[] newData)
        {
            if (_changedData.ContainsKey(key))
                _changedData.Remove(key);
            _changedData.Add(key, newData);

            if (_changedData.Count == 1)
            {
                CurrentVersion++;
                _changedData[ReservedKeys.Version] = BitConverter.GetBytes(CurrentVersion);
            }
        }


        public Dictionary<String, byte[]> GetChanged()
        {
            return _changedData;
        }


        public void Reset()
        {
            _changedData.Clear();
        }
    }
}
