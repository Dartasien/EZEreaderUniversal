using EZEreaderUniversal.DataModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        //string newBook;

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
                    //Debug.WriteLine(file.Name);
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
                    ///TODO add file operations
                }
            }
            string folderName = newFile.Substring(0, newFile.Length - 4);
            await IO.CreateOrGetFolder(folderName);
            await UnZipTheFile(newFile, folderName);
            await IO.DeleteFileInFolder(sFolder, newFile);
            addBookToLibrary(folderName);
            this.Frame.Navigate(typeof(MainPage));
        }

        private async void PrintFiles()
        {
            var results = await sFolder.GetFilesAsync();

            foreach (StorageFile file in results)
            {
                Debug.WriteLine(file.Name);
            }
        }

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
                                    Debug.WriteLine("folder exists");
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
                                        Debug.WriteLine("newFile exists");
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
                                    Debug.WriteLine("newFile exists");
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

        private static async Task<StorageFolder> CreateOrGetFolders(StorageFolder folder, string[] directoryName)
        {
            StorageFolder folderOne = await IO.CreateOrGetFolder(directoryName[0], folder);
            StorageFolder folderTwo;
            StorageFolder folderThree;
            
            if (directoryName.Length == 2)
            {
                return folderOne;
            }
            else if (directoryName.Length == 3)
            {
                return await IO.CreateOrGetFolder(directoryName[1], folderOne);
            }
            else if (directoryName.Length == 4)
            {
                folderTwo = await IO.CreateOrGetFolder(directoryName[1], folderOne);
                return await IO.CreateOrGetFolder(directoryName[2], folderTwo);
            }
            else
            {
                folderTwo = await IO.CreateOrGetFolder(directoryName[1], folderOne);
                folderThree = await IO.CreateOrGetFolder(directoryName[2], folderTwo);
                return await IO.CreateOrGetFolder(directoryName[3], folderThree);
            }
        }

        private void addBookToLibrary(string folderName)
        {
            rootPage.LibrarySource.ImportBook(folderName, true);
        }
    }
}
