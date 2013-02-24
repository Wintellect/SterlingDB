
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace Wintellect.Sterling.WinRT.WindowsStorage
{
    // based on http://codepaste.net/gtu5mq
    internal static class StorageHelper
    {
        public static async Task<bool> FileExistsAsync( string path, StorageStrategies location = StorageStrategies.Local )
        {
            switch ( location )
            {
                case StorageStrategies.Roaming:
                    return await FileExistsAsync( path, ApplicationData.Current.RoamingFolder );

                case StorageStrategies.Temporary:
                    return await FileExistsAsync( path, ApplicationData.Current.TemporaryFolder );

                default:
                    return await FileExistsAsync( path, ApplicationData.Current.LocalFolder );
            }
        }

        public static async Task<bool> FileExistsAsync( string path, StorageFolder folder )
        {
            return ( await GetIfFileExistsAsync( path, folder ) ) != null;
        }

        public static async Task<StorageFolder> EnsureFolderExistsAsync( string path, StorageStrategies location = StorageStrategies.Local )
        {
            switch ( location )
            {
                case StorageStrategies.Roaming:
                    return await EnsureFolderExistsAsync( path, ApplicationData.Current.RoamingFolder );

                case StorageStrategies.Temporary:
                    return await EnsureFolderExistsAsync( path, ApplicationData.Current.TemporaryFolder );

                default:
                    return await EnsureFolderExistsAsync( path, ApplicationData.Current.LocalFolder );
            }
        }

        public static async Task<StorageFolder> EnsureFolderExistsAsync( string path, StorageFolder parentFolder )
        {
            var parent = parentFolder;

            foreach ( var name in path.Trim( '/' ).Split( '/' ) )
            {
                parent = await _EnsureFolderExistsAsync( name, parent );
            }

            return parent;  // now points to innermost folder
        }

        private static async Task<StorageFolder> _EnsureFolderExistsAsync( string name, StorageFolder parent )
        {
            return await parent.CreateFolderAsync( name, CreationCollisionOption.OpenIfExists );
        }

        public static async Task<bool> DeleteFileAsync( string path, StorageStrategies location = StorageStrategies.Local )
        {
            StorageFile file = null;

            switch ( location )
            {
                case StorageStrategies.Roaming:
                    file = await GetIfFileExistsAsync( path, ApplicationData.Current.RoamingFolder );
                    break;

                case StorageStrategies.Temporary:
                    file = await GetIfFileExistsAsync( path, ApplicationData.Current.TemporaryFolder );
                    break;

                default:
                    file = await GetIfFileExistsAsync( path, ApplicationData.Current.LocalFolder );
                    break;
            }
            
            if ( file != null )
                await file.DeleteAsync();

            return !( await FileExistsAsync( path, location ) );
        }

        public static async Task<StorageFolder> GetFolderAsync( string path, StorageStrategies location = StorageStrategies.Local )
        {
            switch ( location )
            {
                case StorageStrategies.Roaming:
                    return await GetFolderAsync( path, ApplicationData.Current.RoamingFolder );

                case StorageStrategies.Temporary:
                    return await GetFolderAsync( path, ApplicationData.Current.TemporaryFolder );

                default:
                    return await GetFolderAsync( path, ApplicationData.Current.LocalFolder );
            }
        }

        public static async Task<StorageFolder> GetFolderAsync( string path, StorageFolder parentFolder )
        {
            var parent = parentFolder;

            foreach ( var name in path.Trim( '/' ).Split( '/' ) )
            {
                parent = await _GetFolderAsync( name, parent );

                if ( parent == null ) return null;
            }

            return parent;  // now points to innermost folder
        }

        private static async Task<StorageFolder> _GetFolderAsync( string name, StorageFolder parent )
        {
            try
            {
                return await parent.GetFolderAsync( name );
            }
            catch ( FileNotFoundException )
            {
                return null;
            }
        }

        public static async Task<BinaryReader> GetReaderForFileAsync( string path, StorageStrategies location = StorageStrategies.Local )
        {
            switch ( location )
            {
                case StorageStrategies.Roaming:
                    return await GetReaderForFileAsync( path, ApplicationData.Current.RoamingFolder );

                case StorageStrategies.Temporary:
                    return await GetReaderForFileAsync( path, ApplicationData.Current.TemporaryFolder );

                default:
                    return await GetReaderForFileAsync( path, ApplicationData.Current.LocalFolder );
            }
        }

        public static async Task<BinaryReader> GetReaderForFileAsync( string path, StorageFolder folder )
        {
            var file = await CreateFileAsync( path, folder );

            var stream = await file.OpenStreamForReadAsync();

            return new BinaryReader( stream );
        }

        public static async Task<BinaryWriter> GetWriterForFileAsync( string path, StorageStrategies location = StorageStrategies.Local )
        {
            switch ( location )
            {
                case StorageStrategies.Roaming:
                    return await GetWriterForFileAsync( path, ApplicationData.Current.RoamingFolder );

                case StorageStrategies.Temporary:
                    return await GetWriterForFileAsync( path, ApplicationData.Current.TemporaryFolder );

                default:
                    return await GetWriterForFileAsync( path, ApplicationData.Current.LocalFolder );
            }
        }

        public static async Task<BinaryWriter> GetWriterForFileAsync( string path, StorageFolder folder )
        {
            var file = await CreateFileAsync( path, folder );

            var stream = await file.OpenStreamForWriteAsync();

            return new BinaryWriter( stream );
        }

        private static async Task<StorageFile> CreateFileAsync( string path, StorageFolder folder, CreationCollisionOption option = CreationCollisionOption.OpenIfExists )
        {
            var parts = path.Split( '/' );

            var fileName = parts.Last();

            if ( parts.Length > 1 )
            {
                folder = await EnsureFolderExistsAsync( path.Substring( 0, path.Length - fileName.Length ), folder );
            }

            return await folder.CreateFileAsync( fileName, option );
        }

        private static async Task<StorageFile> GetIfFileExistsAsync( string path, StorageFolder folder )
        {
            var parts = path.Split( '/' );

            var fileName = parts.Last();

            if ( parts.Length > 1 )
            {
                folder = await GetFolderAsync( path.Substring( 0, path.Length - fileName.Length ), folder );
            }

            if ( folder == null )
            {
                return null;
            }

            try
            {
                return await folder.GetFileAsync( fileName );
            }
            catch ( FileNotFoundException )
            {
                return null;
            }
        }

        public static string Serialize( object objectToSerialize )
        {
            using ( var strm = new MemoryStream() )
            {
                var ser = new DataContractJsonSerializer( objectToSerialize.GetType() );
                ser.WriteObject( strm, objectToSerialize );
                strm.Position = 0;
                var rdr = new StreamReader( strm );
                return rdr.ReadToEnd();
            }
        }

        public static T Deserialize<T>( string jsonString )
        {
            using ( var strm = new MemoryStream( Encoding.Unicode.GetBytes( jsonString ) ) )
            {
                var ser = new DataContractJsonSerializer( typeof( T ) );
                return (T) ser.ReadObject( strm );
            }
        }

        public enum StorageStrategies
        {
            /// <summary>Local, isolated folder</summary>
            Local,
            /// <summary>Cloud, isolated folder. 100k cumulative limit.</summary>
            Roaming,
            /// <summary>Local, temporary folder (not for settings)</summary>
            Temporary
        }
    }
}
