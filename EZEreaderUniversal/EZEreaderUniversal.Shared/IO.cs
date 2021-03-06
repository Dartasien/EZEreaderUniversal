﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

namespace EZEreaderUniversal
{
    class Io
    {
        private static readonly StorageFolder AppBaseFolder = ApplicationData.Current.LocalFolder;
        
        
        /// <summary>
        /// Create a file in the specified folder
        /// </summary>
        /// <param name="storageFolder"></param>Name of folder in which file is to be created
        /// <param name="fileName"></param>Name of file to be created
        /// <returns></returns>
        public static async Task<StorageFile> CreateFileInFolder(StorageFolder storageFolder, string fileName)
        {
            StorageFile newFile;
            try
            {
                newFile = await storageFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
            }
            catch (Exception)
            {
                newFile = null;
            }
            return newFile;
        }

        /// <summary>
        /// Delete the specified file from the specified folder
        /// </summary>
        /// <param name="storageFolder">Name of folder in which file is to be deleted</param>
        /// <param name="fileName">Name of file to be deleted</param>
        /// <returns></returns>
        public static async Task<bool> DeleteFileInFolder(StorageFolder storageFolder, string fileName)
        {
            var deleteSuccessful = true;

            try
            {
                var fileToDelete = await storageFolder.GetFileAsync(fileName);
                await fileToDelete.DeleteAsync();
            }
            catch (Exception)
            {
                deleteSuccessful = false;
            }
            return deleteSuccessful;
        }

        /// <summary>
        /// Get the root folder for the current app.  This is used to retrieve data files
        /// you've bundled with your app as opposed to data files created by your user.
        /// </summary>
        /// <returns></returns>
        public static StorageFolder GetAppInstallationFolder()
        {
            var folder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            return folder;
        }

        /// <summary>
        /// Get the root folder for the current app.  This is used to retrieve data files
        /// you've bundled with your app as opposed to data files created by your user.
        /// </summary>
        /// <returns></returns>
        public static StorageFolder GetAppStorageFolder()
        {
            return AppBaseFolder;
        }

        /// <summary>
        /// Create the specified subfolder in the app's root storage folder
        /// </summary>
        /// <param name="folderName">Name of folder to be created</param>
        /// <returns></returns>
        public static async Task<StorageFolder> CreateOrGetFolder(string folderName)
        {
            StorageFolder newFolder;
            try
            {
                var storageFolder = ApplicationData.Current.LocalFolder;

                if (folderName == "\\")
                    return storageFolder;

                try
                {
                    newFolder = await storageFolder.GetFolderAsync(folderName);
                }
                catch (Exception)
                {
                    newFolder = null;
                }

                if (newFolder == null)
                    newFolder = await storageFolder.CreateFolderAsync(folderName);
            }
            catch (Exception)
            {
                newFolder = null;
            }
            return newFolder;
        }

        /// <summary>
        /// Create the specified subfolder in the app's root storage folder
        /// </summary>
        /// <param name="folderName">Name of folder to be created</param>
        /// <param name="sFolder">Name of the storageFolder to search</param>
        /// <returns></returns>
        public static async Task<StorageFolder> CreateOrGetFolder(string folderName,
            StorageFolder sFolder)
        {
            StorageFolder newFolder;
            try
            {

                if (folderName == "\\")
                    return sFolder;

                try
                {
                    newFolder = await sFolder.GetFolderAsync(folderName);
                }
                catch (Exception)
                {
                    newFolder = null;
                }

                if (newFolder == null)
                    newFolder = await sFolder.CreateFolderAsync(folderName);
            }
            catch (Exception)
            {
                newFolder = null;
            }
            return newFolder;
        }


        /// <summary>
        /// Get a StorageFile object for the specified file in the specified folder
        /// </summary>
        /// <param name="storageFolder">Name of the folder from which the file is to be retrieved</param>
        /// <param name="fileName">Name of the file to be retrieved</param>
        /// <returns></returns>
        public static async Task<StorageFile> GetFileInFolder(StorageFolder storageFolder, string fileName)
        {
            StorageFile newFile;
            try
            {
                newFile = await storageFolder.GetFileAsync(fileName);
            }
            catch (Exception)
            {
                newFile = null;
            }
            return newFile;

        }

        /// <summary>
        /// Get a StorageFolder whichin multiple folders using a string
        /// </summary>
        /// <param name="folder">Starting folder</param>
        /// <param name="directoryName">string array of folder names</param>
        /// <returns></returns>
        public static async Task<StorageFolder> CreateOrGetFolders(StorageFolder folder, string[] directoryName)
        {
            var folderOne = await CreateOrGetFolder(directoryName[0], folder);
            StorageFolder folderTwo;
            StorageFolder folderThree;
            StorageFolder folderFour;

            switch (directoryName.Length)
            {
                case 2:
                    return folderOne;
                case 3:
                    return await CreateOrGetFolder(directoryName[1], folderOne);
                case 4:
                    folderTwo = await CreateOrGetFolder(directoryName[1], folderOne);
                    return await CreateOrGetFolder(directoryName[2], folderTwo);
                case 5:
                    folderTwo = await CreateOrGetFolder(directoryName[1], folderOne);
                    folderThree = await CreateOrGetFolder(directoryName[2], folderTwo);
                    return await CreateOrGetFolder(directoryName[3], folderThree);
                case 6:
                    folderTwo = await CreateOrGetFolder(directoryName[1], folderOne);
                    folderThree = await CreateOrGetFolder(directoryName[2], folderTwo);
                    folderFour = await CreateOrGetFolder(directoryName[3], folderThree);
                    return await CreateOrGetFolder(directoryName[4], folderFour);
                default:
                    folderTwo = await CreateOrGetFolder(directoryName[1], folderOne);
                    folderThree = await CreateOrGetFolder(directoryName[2], folderTwo);
                    folderFour = await CreateOrGetFolder(directoryName[3], folderThree);
                    var folderFive = await CreateOrGetFolder(directoryName[4], folderFour);
                    return await CreateOrGetFolder(directoryName[4], folderFive);
            }
        }

        /// <summary>
        /// Method to delete all files and folders in the apps local folder
        /// </summary>
        /// <returns></returns>
        public async static Task DeleteAllFilesInLocalFolder()
        {
            var filesInRoot = await AppBaseFolder.GetFilesAsync();
            var foldersInRoot = await AppBaseFolder.GetFoldersAsync();
            foreach (var file in filesInRoot)
            {
                //if (file.Name != "ebooks.xml")
                //{
                await file.DeleteAsync();
                //}
            }
            foreach (var folders in foldersInRoot)
            {
                await folders.DeleteAsync();
            }

        }

        /// <summary>
        /// Deletes a folder in the local storage of the device
        /// </summary>
        /// <param name="folderName"></param>
        /// <returns></returns>
        public async static Task DeleteFolderInLocalFolder(string folderName)
        {
            var folderToDelete = await AppBaseFolder.GetFolderAsync(folderName);
            await folderToDelete.DeleteAsync();
        }

        /// <summary>
        /// Get the names of all the files in the default folder for this app
        /// </summary>
        /// <returns>List of all the files in this app's root folder</returns>
        public static async Task<List<string>> GetDocumentFiles()
        {
            var results = new List<string>();

            try
            {
                var storageFolder = ApplicationData.Current.LocalFolder;
                var queryResult = storageFolder.CreateFileQuery();
                var files = await queryResult.GetFilesAsync();
                results.AddRange(files.Select(file => file.Name));
            }
            catch (Exception)
            {
                results = null;
            }
            return results;
        }

        /// <summary>
        /// Get StorageFile objects for all the files in the specified folder
        /// </summary>
        /// <param name="storageFolder">Name of the folder for which the file objects are to be retrieved</param>
        /// <returns></returns>
        public static async Task<IReadOnlyList<StorageFile>> GetFilesInFolder(StorageFolder storageFolder)
        {
            IReadOnlyList<StorageFile> results = null;

            try
            {
                results = await storageFolder.GetFilesAsync();
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch (Exception)
            {
            }
            return results;
        }

        /// <summary>
        /// Write the specified string to a file overwriting all its previous contents
        /// </summary>
        /// <param name="f">StorageFile to which the data is to be written</param>
        /// <param name="data">String data to write to the file</param>
        /// <returns></returns>
        public static async Task<bool> WriteStringToFile(StorageFile f, string data)
        {
            var result = true;

            try
            {
                using (var stream = await f.OpenAsync(FileAccessMode.ReadWrite))
                {
                    using (var dataWriter = new DataWriter(stream))
                    {
                        dataWriter.WriteString(data);
                        await dataWriter.StoreAsync();
                        await dataWriter.FlushAsync();
                    }
                }
            }
            catch (Exception)
            {
                result = false;
            }
            return result;
        }

        /// <summary>
        /// Read the entire contents of the specified file and return it as a string
        /// </summary>
        /// <param name="f">StorageFile from which data is to be read</param>
        /// <returns>Entire contents of the file as a string</returns>
        public static async Task<string> ReadStringFromFile(StorageFile f)
        {
            string result;

            try
            {
                using (var stream = await f.OpenAsync(FileAccessMode.Read))
                {
                    using (var dataReader = new DataReader(stream))
                    {
                        var numBytesLoaded = await dataReader.LoadAsync((uint)stream.Size);
                        result = dataReader.ReadString(numBytesLoaded);
                    }
                }
            }
            catch (Exception)
            {
                result = null;
            }

            return result;
        }

        /// <summary>
        /// Get a list of names of files whose filename ends with the specified extension
        /// </summary>
        /// <param name="storageFolder">Folder to be searched</param>
        /// <param name="extension">File extension (e.g. .txt)</param>
        /// <returns>List of names of files in the specified folder which end with the specified extension</returns>
        public static async Task<List<string>> GetDocumentFilesOfTypeFromFolder(StorageFolder storageFolder, string extension)
        {
            var results = new List<string>();

            try
            {
                var queryResult = storageFolder.CreateFileQuery();
                var files = await queryResult.GetFilesAsync();
                results.AddRange(from file in files where file.Name.ToLower().EndsWith(extension.ToLower()) select file.Name);
            }
            catch (Exception)
            {
                results = null;
            }
            return results;
        }

        public static async Task<BitmapImage> GetCoverImage(string picLoc)
        {
            var img = new BitmapImage();
            var ras = new InMemoryRandomAccessStream();
            if (picLoc.Contains("/"))
            {
                var picLoc2 = picLoc.Split('/');
                var newFolder = await CreateOrGetFolders(AppBaseFolder, picLoc2);
                using (var fileSource = await newFolder.OpenStreamForReadAsync(picLoc2[picLoc2.Length - 1]))
                {
                    await fileSource.CopyToAsync(ras.AsStreamForWrite());
                    await img.SetSourceAsync(ras);
                    return img;
                }
            }
            return null;
        }

        /// <summary>
        /// Serialize an arbitrary object to an XML string
        /// </summary>
        /// <param name="obj">Object to be serialized.  Must be serializable.</param>
        /// <returns>XML string representing object or null if object is not serializable</returns>
        public static string SerializeToString(object obj)
        {
            var serializer = new XmlSerializer(obj.GetType());

            try
            {
                using (var writer = new StringWriter())
                {
                    serializer.Serialize(writer, obj);

                    return writer.ToString();
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Deserialize an arbitrary object from an XML string
        /// </summary>
        /// <typeparam name="T">Type of object to be deserialized</typeparam>
        /// <param name="xml">XML serialized version of the object</param>
        /// <returns>Object with data as described by XML or default(T) if deserialization failed.</returns>
        public static T SerializeFromString<T>(string xml)
        {
            var serializer = new XmlSerializer(typeof(T));

            try
            {
                using (var reader = new StringReader(xml))
                {
                    return (T)serializer.Deserialize(reader);
                }
            }
            catch
            {
                return default(T);
            }
        }

        /// <summary>
        /// Load XML from a file into an XDocument structure
        /// </summary>
        /// <param name="storageFolder">Folder containing the file</param>
        /// <param name="filename">Name of file containing the desired XML</param>
        /// <returns>XDocument representing the XML or default(XDocument) if failure occured</returns>
        public static async Task<XDocument> LoadXmlDocumentAsync(StorageFolder storageFolder, string filename)
        {

            var storageFile = await storageFolder.GetFileAsync(filename);

            try
            {
                var readStream = await storageFile.OpenAsync(FileAccessMode.Read);
                var inputStream = readStream.GetInputStreamAt(0);
                var dataReader = new DataReader(inputStream);
                var numBytesLoaded = await dataReader.LoadAsync((uint)readStream.Size);
                var s = dataReader.ReadString(numBytesLoaded);
                var stringReader = new StringReader(s);

                return XDocument.Load(stringReader);
            }
            catch
            {
                return default(XDocument);
            }
        }

        /// <summary>
        /// Write an XDocument to the specified file in the specified folder.
        /// </summary>
        /// <param name="xmlDoc">XDocument object containing the data to write as XML to the specified file</param>
        /// <param name="storageFolder">Folder where the file is to be written</param>
        /// <param name="filename">Name of the file to be written containing the XML representation of the XDocument's contents</param>
        /// <returns></returns>
        public static async Task SaveXmlDocumentAsync(XDocument xmlDoc, StorageFolder storageFolder, string filename)
        {

            var sb = new StringBuilder();

            var xws = new XmlWriterSettings {Indent = true};

            try
            {
                using (var xw = XmlWriter.Create(sb, xws))
                {

                    xmlDoc.Save(xw);

                }

                var storageFileResult = await storageFolder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);
                var streamResult = await storageFileResult.OpenAsync(FileAccessMode.ReadWrite);
                var outStream = streamResult.GetOutputStreamAt(0);

                var dataWriter = new DataWriter(outStream);

                dataWriter.WriteString(sb.ToString());

                await dataWriter.StoreAsync();

                await outStream.FlushAsync();
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch
            {
            }
        }
    }
}
