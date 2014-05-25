using EZEreaderUniversal.DataModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace EZEreaderUniversal
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LoadingPage : Page
    {
        private static StorageFolder sFolder = ApplicationData.Current.LocalFolder;
        MainPage rootPage = MainPage.Current;

        public LoadingPage()
        {
            this.InitializeComponent();
        }

        private FileActivatedEventArgs _fileEventArgs = null;
        public FileActivatedEventArgs FileEvent
        {
            get { return _fileEventArgs; }
            set { _fileEventArgs = value; }
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            string originalFile = " ";
            string newFile = "";
            if (rootPage.FileEvent != null)
            {    
                
                foreach (StorageFile file in rootPage.FileEvent.Files)
                {
                    originalFile = file.Name;
                    newFile = originalFile.Substring(0, originalFile.Length - 4);
                    newFile += "zip";
                    await IO.DeleteFileInFolder(sFolder, newFile);
                    try
                    {
                        await file.CopyAsync(sFolder, newFile);
                    }
                    catch (Exception)
                    {

                    }
                }
            }
            string folderName = newFile.Substring(0, newFile.Length - 4);
            await IO.CreateOrGetFolder(folderName);
            await UnZipTheFile(newFile, folderName);
            await IO.DeleteFileInFolder(sFolder, newFile);
            addBookToLibrary(folderName);
            this.Frame.Navigate(typeof(MainPage));
        }

        /// <summary>
        /// Unzips the .zip file into the specified folder to allow for the reading necessary
        /// to open the book in this app
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="folderName"></param>
        /// <returns></returns>
        private async Task UnZipTheFile(string fileName, string folderName)
        {
            StorageFolder folder = await sFolder.GetFolderAsync(folderName);
 
            using (var stream = await sFolder.OpenStreamForReadAsync(fileName))
            {
                using (ZipArchive archive = new ZipArchive(stream))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        if (entry.FullName.Contains('/'))
                        {
                            if (entry.FullName.ElementAt(entry.FullName.Length - 1) == '/')
                            {
                                string multiFolder = entry.FullName.Substring(0, entry.FullName.Length - 1);
                                try
                                {
                                    await sFolder.CreateFolderAsync(folderName + "\\"
                                        + multiFolder);
                                }
                                catch (Exception)
                                {
                                    
                                }
                            }
                            else
                            {
                                string[] directoryName = entry.FullName.Split('/');
                                StorageFolder lastFolder = await IO.CreateOrGetFolders(folder, directoryName);
                                using (var file = entry.Open())
                                {
                                    StorageFile newFile = null;
                                    try
                                    {
                                        newFile = await lastFolder.CreateFileAsync(entry.Name, CreationCollisionOption.ReplaceExisting);
                                    }
                                    catch (Exception)
                                    {
                                        
                                    }
                                    if (newFile == null)
                                    {
                                        newFile = await lastFolder.GetFileAsync(entry.Name);
                                    }
                                    using (var trans = await newFile.OpenStreamForWriteAsync())
                                    {
                                        file.CopyTo(trans);
                                    }
                                }
                            }
                        }
                        else
                        {
                            //add files to the base folder
                            using (var file = entry.Open())
                            {
                                StorageFile newFile = null;
                                try
                                {
                                    newFile = await folder.CreateFileAsync(entry.FullName, CreationCollisionOption.ReplaceExisting);
                                }
                                catch (Exception)
                                {
                                   
                                }
                                if (newFile == null)
                                {
                                    newFile = await folder.GetFileAsync(entry.FullName);
                                }
                                using (var trans = await newFile.OpenStreamForWriteAsync())
                                {
                                    file.CopyTo(trans);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Adds to book to the bookmodel collection
        /// </summary>
        /// <param name="folderName"></param>
        private void addBookToLibrary(string folderName)
        {
            rootPage.LibrarySource.ImportBook(folderName, true);
        }
         
    }
}
