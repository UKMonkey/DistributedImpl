using System;
using System.IO;
using System.Security.Cryptography;

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
    public class DllUpdater<TSharedMemoryType> : DirectoryMonitor
        where TSharedMemoryType : DllCommunication
    {
        private readonly DllMonitor<TSharedMemoryType> _dllMonitor;
        private readonly String _targetCopyDirectory;

        public event FilenameCallback FileUpdated;


        public DllUpdater(String targetDir, DllMonitor<TSharedMemoryType> dllMonitor, String targetCopyDir = null)
            : base(targetDir, DllHelper.GetDllExtension())
        {
            _dllMonitor = dllMonitor;
            _dllMonitor.DllDeleted += AttemptFileMove;

            _targetCopyDirectory = targetCopyDir;
            if (_targetCopyDirectory == null)
                _targetCopyDirectory = dllMonitor.FolderToMonitor;
        }


        protected override void ProcessFile(String fullFileName, String fileName)
        {
            // if we're not going to be replacing the dll directly then we don't want to delete it.
            // this means that the monitor watching for new server dlls will attempt to delete the dll,
            // the monitor watching the client dlls won't.
            //
            // this leads to the order of adding new dlls to be 
            // "copy new client dll into new client dir"
            // "copy new server dll into new server dir with different extension"
            // "rename new server dll to have correct extension"
            _dllMonitor.DeleteFile(fileName);
        }


        protected void AttemptFileMove(String fileName)
        {
            String from = Path.Combine(FolderToMonitor, fileName);
            String to = Path.Combine(_targetCopyDirectory, fileName);

            Console.WriteLine("Attempting to move " + from + " to " + to);

            File.Delete(to);
            File.Move(from, to);
            ForceClearMd5(fileName);

            if (FileUpdated != null)
                FileUpdated(fileName);
        }


        /// <summary>
        /// Generates the md5 of a file
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        protected override string CalculateMD5(string file)
        {
            string filename = Path.Combine(_targetCopyDirectory, file);
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    return BitConverter.ToString(md5.ComputeHash(stream));
                }
            }
        }
    }
}
