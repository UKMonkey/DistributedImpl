﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using DistributedShared.SystemMonitor.Managers;

namespace DistributedShared.SystemMonitor.DllMonitoring
{
    /************************************************************************/
    /* Monitors a directory, and when a file is found, an action is         */
    /* performed on that file.  That action for this class is nothing       */
    /* but can be overridden by other classes.                              */
    /* The callback is only ever called once for the file; and until the    */
    /* file is deleted the callback will not be run again                   */
    /************************************************************************/
    public class DirectoryMonitor
    {
        public readonly String FolderToMonitor;

        private Thread _montoringThread;
        private volatile bool _performWork = true;

        private readonly String _extensionToMonitor;
        private readonly Dictionary<String, String> _fileToMd5 = new Dictionary<string, string>(); 


        public DirectoryMonitor(String targetDirectory, String extensionToMonitor)
        {
            FolderToMonitor = targetDirectory;
            _extensionToMonitor = extensionToMonitor;

            Directory.CreateDirectory(targetDirectory);
        }



        /// <summary>
        /// Starts monitoring files for changes
        /// </summary>
        public void StartMonitoring()
        {
            _montoringThread = new Thread(MonitoringThreadMain);
            _performWork = true;
            StaticThreadManager.Instance.StartNewThread(_montoringThread, "DllMonitor");
        }


        /// <summary>
        /// Stops the monitoring process
        /// </summary>
        public void StopMonitoring()
        {
            if (_montoringThread != null)
            {
                _performWork = false;
                _montoringThread.Join();
                _montoringThread = null;
            }
        }


        /// <summary>
        /// Called on discovery of a new file
        /// </summary>
        /// <param name="fullFileName"></param>
        /// <param name="fileName"></param>
        protected virtual void ProcessFile(String fullFileName, String fileName)
        {
        }


        /// <summary>
        /// Cleans out the knowledge of the given file, ensuring that if it still exists
        /// when the monitor next loops that the "ProcessFile" is called on it.
        /// </summary>
        /// <param name="fileName"></param>
        protected virtual void RegisterRemovedFile(String fileName)
        {
            _fileToMd5.Remove(fileName);
        }


        /// <summary>
        /// Examines all files in a directory
        /// </summary>
        /// <param name="folderToExamine"></param>
        private void ExamineFolder(string folderToExamine)
        {
            var files = Directory.EnumerateFiles(folderToExamine, _extensionToMonitor);
            var processedFiles = new HashSet<String>();

            lock (this)
            {
                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file);
                    processedFiles.Add(fileName);

                    if (!_fileToMd5.ContainsKey(file))
                    {
                        ProcessFile(file, fileName);
                        _fileToMd5.Add(fileName, CalculateMD5(file));
                    }
                }

                // now remove any files that didn't exist from the md5 hash
                var deadFiles = _fileToMd5.Keys.Where(p => !processedFiles.Contains(p)).ToList();
                foreach (var file in deadFiles)
                    RegisterRemovedFile(file);
            }
        }


        /// <summary>
        /// Thread entry point - Watches a folder for any dlls and acts when a new dll is located
        /// </summary>
        private void MonitoringThreadMain()
        {
            while (_performWork)
            {
                ExamineFolder(FolderToMonitor);
                Thread.Sleep(1000);
            }
        }


        /// <summary>
        /// Returns the file contents of a dll.  Suitable for sending a file over the network
        /// This function performs caching if required - so callers of this function should NOT
        /// </summary>
        /// <param name="dll"></param>
        /// <returns></returns>
        public byte[] GetFileContentContent(string file)
        {
            lock (this)
            {
                var fullPath = Path.Combine(FolderToMonitor, file);
                return File.ReadAllBytes(fullPath);
            }
        }


        /// <summary>
        /// Returns the MD5 of a specific dll
        /// </summary>
        /// <param name="dllName"></param>
        /// <returns></returns>
        public String GetFileMd5(String fileName)
        {
            lock (this)
            {
                if (_fileToMd5.ContainsKey(fileName))
                    return _fileToMd5[fileName];
                if (File.Exists(Path.Combine(FolderToMonitor, fileName)))
                    return CalculateMD5(Path.Combine(FolderToMonitor, fileName));
                return "";
            }
        }


        /// <summary>
        /// Generates the md5 of a file
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        private string CalculateMD5(string filename)
        {
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