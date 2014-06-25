using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace DistributedShared.SystemMonitor.DllMonitoring
{
    /************************************************************************/
    /* this class is responsible for requesting dlls to be terminated       */
    /* and then replacing them with a new one.                              */
    /* the dll monitor will then take responsibiliy for reloading the       */
    /* replacement                                                          */
    /*                                                                      */
    /* IMPORTANT:  Directory monitors perform locking                       */
    /*     since this uses the DLLMonitor, the dll montitor will do locking */
    /*     thus to ensure that we do not have a dead lock, this must be     */
    /*     locked before calling any DLLMonitor function                    */
    /************************************************************************/
    public class DllUpdater<SharedMemoryType> : DirectoryMonitor
        where SharedMemoryType : DllSharedMemory, new()
    {
        private readonly DllMonitor<SharedMemoryType> _dllMonitor;
        private readonly String _targetCopyDirectory;


        public DllUpdater(String targetDir, DllMonitor<SharedMemoryType> dllMonitor, String targetCopyDir = null)
            : base(targetDir, DllHelper.GetDllExtension())
        {
            _dllMonitor = dllMonitor;
            _dllMonitor.DllDeleted += AttemptFileMove;

            _targetCopyDirectory = targetCopyDir;
            if (_targetCopyDirectory == null)
                _targetCopyDirectory = dllMonitor.FolderToMonitor;
        }


        protected virtual void ProcessFile(String fullFileName, String fileName)
        {
            // if we're not going to be replacing the dll directly then we don't want to delete it.
            // this means that the monitor watching for new server dlls will attempt to delete the dll,
            // the monitor watching the client dlls won't.
            //
            // this leads to the order of adding new dlls to be 
            // "copy new client dll into new client dir"
            // "copy new server dll into new server dir with different extension"
            // "rename new server dll to have correct extension"
            _dllMonitor.DeleteDll(fileName);
        }


        protected void AttemptFileMove(String fileName)
        {
            String from = Path.Combine(FolderToMonitor, fileName);
            String to = Path.Combine(_targetCopyDirectory, fileName);

            File.Move(from, to);
        }
    }
}
