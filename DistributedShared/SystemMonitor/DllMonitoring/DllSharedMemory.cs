using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.MemoryMappedFiles;
using System.Threading;
using System.IO;
using System.Runtime.InteropServices;
using DistributedSharedInterfaces.Serialisation;

namespace DistributedShared.SystemMonitor.DllMonitoring
{
    public delegate void DllSharedMemoryCallback(DllSharedMemory item);


    public enum StopReason : short
    {
        Requested
    }


    public class DllSharedMemory : IDisposable
    {
        // reserve the first 10k for locks, security as well as future-proofing etc
        // which will all be used for this class.  Anything after that is available for
        // any parent classes of this one.
        private const long _protectedOffset = 1024 * 10;

        // How those offsets are used...
        // The ID offset allows each program connecting to pick a uid.
        // that uid can then be used to identify which program changed which parts of the 
        // memory, so that they don't start thinking that changes they've made were done by someone else
        private const long _nextIdOffset = 0;
        private const long _nextIdSize = 2;

        private const long _protectedDataUpdatedOffset = _nextIdOffset + _nextIdSize;
        private const long _protectedDataUpdatedSize = 2;

        // when the wrapped data is updated, it needs some form of ID so that we know what
        // should be reloaded (if anything) 
        private const long _wrappedDataUpdatedOffset = _protectedDataUpdatedOffset + _protectedDataUpdatedSize;
        private const long _wrappedDataUpdatedSize = 2;

        // When the exe should stop, this bit is set to true
        private const long _stopRequestedOffset = _wrappedDataUpdatedOffset + _wrappedDataUpdatedSize;
        private const long _stopRequestedSize = 1;

        // When the exe stopped for some reason and the exe was able to explain why, it should be stored here
        private const long _stopReasonOffset = _stopRequestedOffset + _stopRequestedSize;
        private const long _stopReasonSize = 2;

        private const long _maxProtectedArea = _stopReasonOffset + _stopReasonSize;
        

        // event triggered when the exe has been requested to stop.
        public event DllSharedMemoryCallback ExeStopRequest;

        // event triggered when the data in the shared memory has been changed
        public event DllSharedMemoryCallback WrappedDataChanged;

        // event triggered when the data in the protected memory has been changed
        private event DllSharedMemoryCallback ProtectedDataChanged;

        // TODO - throw if trying to set the DLL name or shared memory path if we've
        // already connected to the shared memory
        public String DllName { get; set; }
        public String SharedMemoryPath { get; set; }
        public String SharedMemoryFile { get; private set; }
        public StopReason StopReason { get; private set; }
        public bool StopRequested { get; private set; }

        // Thread that will fire off the various events
        private Thread _fileMonitor;
        private volatile bool _montiorFile;

        private MemoryMappedFile _file;
        private Mutex _fileProtectionMutex;
        private int _id;


        public DllSharedMemory()
        {
            ProtectedDataChanged += ReloadProtectedData;
            _montiorFile = true;

            // TODO - make sure this does cause a compiler failure in the case of 
            // the the required protected area exceeding the actual area
            var compileError = 1 / (_maxProtectedArea > _protectedOffset ? 0 : 1);
        }


        public void Dispose()
        {
            _montiorFile = false;
            while (_fileMonitor.IsAlive)
                Thread.Sleep(10);

            _file.Dispose();
            _fileProtectionMutex.Dispose();
        }


        public void Connect(bool isNew)
        {
            if (_file != null)
                throw new ArgumentException("Already connected to the shared memory");

            SharedMemoryFile = Path.Combine(SharedMemoryPath, DllName) + ".data";
            _fileProtectionMutex = new Mutex(false, DllName + "Protection");

            if (isNew)
            {
                _file = MemoryMappedFile.CreateFromFile(SharedMemoryFile);
                initialiseProtectedSpace();
                initialiseWrappedSpace();
            }
            else
            {
                _file = MemoryMappedFile.OpenExisting(SharedMemoryFile);
            }

            GetId();
            using (var lck = GetSharedLock())
            {
                ReloadProtectedData(this);
            }

            _fileMonitor = new Thread(ThreadMonitorMain);
            _fileMonitor.Start();
        }


        private void GetId()
        {
            using (var lck = GetSharedLock())
            {
                long offset = _nextIdOffset;
                _id = BitConverter.ToInt16(ReadBytes(ref offset, (int)_nextIdSize), 0);
                if (_id == 0)
                    _id = 1;

                offset = _nextIdOffset;
                WriteBytes(ref offset, BitConverter.GetBytes(_id+1));
            }
        }


        private void initialiseProtectedSpace()
        {
            var data = new byte[_protectedOffset];
            for (var i = 0; i < data.Length; ++i)
                data[i] = 0;

            long offset = 0;
            WriteBytes(ref offset, data);
        }


        protected virtual void initialiseWrappedSpace()
        {
            // does nothing here
        }


        private void ThreadMonitorMain()
        {
            while (_montiorFile)
            {
                using (var lck = GetSharedLock(50))
                {
                    if (!lck.Valid)
                        continue;  // means the other process has a lock on the file, we should just wait longer

                    if (HasProtectedDataChanged())
                        ProtectedDataChanged(this);
                    if (HasWrappedDataChanged())
                        WrappedDataChanged(this);
                }
                Thread.Sleep(50);
            }
        }


        private bool HasProtectedDataChanged()
        {
            using (var lck = GetSharedLock())
            {
                long offset = _protectedDataUpdatedOffset;
                var id = BitConverter.ToInt16(ReadBytes(ref offset, (int)_protectedDataUpdatedSize), 0);

                if (id == _id || id == 0)
                    return false;

                return true;
            }
        }


        private bool HasWrappedDataChanged()
        {
            using (var lck = GetSharedLock())
            {
                long offset = _wrappedDataUpdatedOffset;
                var id = BitConverter.ToInt16(ReadBytes(ref offset, (int)_wrappedDataUpdatedSize), 0);

                if (id == _id || id == 0)
                    return false;

                return true;
            }
        }


        private static void ReloadProtectedData(DllSharedMemory item)
        {
            var fireStopRequested = false;
            long offset;

            if (!item.StopRequested)
            {
                offset = _stopRequestedOffset;
                item.StopRequested = item.ReadBytes(ref offset, (int)_stopRequestedSize)[0] == 1;
                fireStopRequested = true;
            }

            offset = _stopReasonOffset;
            item.StopReason = (StopReason)(BitConverter.ToInt16(item.ReadBytes(ref offset, (int)_stopReasonSize), 0));

            if (fireStopRequested)
                item.ExeStopRequest(item);
        }


        public void RequestStop(StopReason reason)
        {
            using (var lck = GetSharedLock())
            {
                var data = new byte[_stopRequestedSize];
                data[0] = 1;

                long offset = _stopRequestedOffset;
                WriteBytes(ref offset, data);

                offset = _stopReasonOffset;
                WriteBytes(ref offset, BitConverter.GetBytes((short)reason));

                StopRequested = true;
                StopReason = reason;

                MarkOffsetWithId(_protectedDataUpdatedOffset);
            }
        }


        protected void WrappedDataUpdated()
        {
            MarkOffsetWithId(_wrappedDataUpdatedOffset);
        }


        private void MarkOffsetWithId(long offset)
        {
            WriteBytes(ref offset, BitConverter.GetBytes(_id));
        }


        // call this and DISPOSE the object for each bulk of reading / writing that needs to be done.
        private Lock GetSharedLock()
        {
            return new Lock(_fileProtectionMutex);
        }


        private Lock GetSharedLock(int waitTime)
        {
            return new Lock(_fileProtectionMutex, waitTime);
        }


        private void WriteBytes(ref long offset, byte[] data)
        {
            using (var accessor = _file.CreateViewAccessor(offset, data.Length))
            {
                accessor.WriteArray(offset, data, 0, data.Length);
            }
            offset += data.Length;
        }


        private byte[] ReadBytes(ref long offset, int amount)
        {
            byte[] result = new byte[amount];

            using (var accessor = _file.CreateViewAccessor(offset, amount))
            {
                accessor.ReadArray(0, result, 0, amount);
            }
            offset += amount;
            return result;
        }


        protected void WriteDataList<T>(ref long offset, List<T> items)
            where T : ISerialisable
        {
            WriteBytes(ref offset, BitConverter.GetBytes(items.Count));
            for (var i = 0; i < items.Count; ++i)
                WriteData(ref offset, items[i]);
        }


        protected List<T> ReadDataList<T>(ref long offset)
            where T:ISerialisable, new()
        {
            var listSize = BitConverter.ToInt32(ReadBytes(ref offset, 4), 0);
            var ret = new List<T>(listSize);

            for (var i = 0; i < listSize; ++i)
                ret.Add(ReadData<T>(ref offset));

            return ret;
        }


        protected T ReadData<T>(ref long offset)
            where T: ISerialisable, new()
        {
            offset += _protectedOffset;
            var ret = new T();
            var dataSize = BitConverter.ToInt32(ReadBytes(ref offset, 4), 0);
            ret.Data = ReadBytes(ref offset, dataSize);
            return ret;
        }


        protected void WriteData<T>(ref long offset, T item)
            where T : ISerialisable
        {
            offset += _protectedOffset;
            byte[] data = item.Data;

            WriteBytes(ref offset, BitConverter.GetBytes(data.Length));
            WriteBytes(ref offset, data);
        }
    }
}
