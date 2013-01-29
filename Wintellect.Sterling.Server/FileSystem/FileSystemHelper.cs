using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Wintellect.Sterling.Server.FileSystem
{
    /// <summary>
    ///     This class is used to assist with manager the isolated storage references, allowing
    ///     for nested requests to use the same isolated storage reference
    /// </summary>
    public class FileSystemHelper
    {
        private static readonly List<string> _paths = new List<string>();
        private static readonly List<string> _files = new List<string>();
                
        /// <summary>
        ///     Gets an isolated storage reader
        /// </summary>
        /// <param name="path">The path</param>
        /// <returns>The reader</returns>
        public BinaryReader GetReader(string path)
        {
            try
            {
                return new BinaryReader(File.OpenRead(path));
            }
            catch(Exception ex)
            {
                throw new SterlingFileSystemException(ex);
            }
        }

        private static readonly object _writerMutex = new object();

        /// <summary>
        ///     Get an isolated storage writer
        /// </summary>
        /// <param name="path">The path</param>
        /// <returns>The writer</returns>
        public BinaryWriter GetWriter(string path)
        {
            try
            {
                Monitor.Enter(_writerMutex);
                var stream = File.Open(path, FileMode.Create, FileAccess.Write);
                return new BinaryWriter(stream);
            }
            catch(Exception ex)
            {
                throw new SterlingFileSystemException(ex);
            }
            finally
            {
                Monitor.Exit(_writerMutex);
            }
        }

        /// <summary>
        ///     Delete a file based on its path
        /// </summary>
        /// <param name="path">The path</param>
        public void Delete(string path)
        {            
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                    if (_files.Contains(path))
                    {
                        _paths.Remove(path);
                    }
                }
            }
            catch(Exception ex)
            {
                throw new SterlingFileSystemException(ex);   
            }
        }
       
        /// <summary>
        ///     Ensure that a directory exists
        /// </summary>
        /// <param name="path">the path</param>
        public void EnsureDirectory(string path)
        {            
            if (path.EndsWith("/"))
            {
                path = path.Substring(0, path.Length - 1);
            }

            try
            {
                if (!_paths.Contains(path))
                {
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    _paths.Add(path);
                }
            }
            catch(Exception ex)
            {
                throw new SterlingFileSystemException(ex);
            }
        }

        /// <summary>
        ///     Check to see if a file exists
        /// </summary>
        /// <param name="path">The path to the file</param>
        /// <returns>True if it exists</returns>
        public bool FileExists(string path)
        {
            try
            {
                if (_files.Contains(path))
                    return true; 

                if (File.Exists(path))
                {
                    _files.Add(path);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                throw new SterlingFileSystemException(ex);
            }
        }

        /// <summary>
        /// Purge a directory and everything beneath it
        /// </summary>
        /// <param name="path">The path</param>
        public void Purge(string path)
        {
            _Purge(path, true);
        }

        /// <summary>
        /// Purge a directory and everything beneath it
        /// </summary>
        /// <param name="path">The path</param>
        /// <param name="clear">A value indicating whether the internal lists should be cleared</param>
        private static void _Purge(string path, bool clear)
        {
            if (clear)
            {
                _paths.Clear();
                _files.Clear();
            }

            try
            {
                // already purged!
                if (!Directory.Exists(path))
                {
                    return;
                }

                // clear the sub directories
                var directory = new DirectoryInfo(path);

                foreach (var dir in directory.EnumerateDirectories())
                {
                    _Purge(Path.Combine(path, dir.FullName), false);
                }

                // clear the files - don't use a where clause because we want to get closer to the delete operation
                // with the filter
                foreach (var filePath in
                    directory.EnumerateFiles()
                    .Select(file => file.FullName))
                {
                    var pathLock = PathLock.GetLock(filePath.Replace("\\", "/"));
                    lock (pathLock)
                    {
                        File.Delete(filePath);
                    }
                }

                var dirPath = path.TrimEnd('\\', '/');
                if (!string.IsNullOrEmpty(dirPath) && Directory.Exists(dirPath))
                {
                    Directory.Delete(dirPath);                   
                }
            }
            catch (Exception ex)
            {
                throw new SterlingFileSystemException(ex);
            }
        }        
        
        public static void PurgeAll()
        {
            var fileHelper = new FileSystemHelper();
            fileHelper.Purge(PathProvider.RootPath);
        }
    }
}
