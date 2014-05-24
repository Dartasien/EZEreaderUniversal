﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using System.IO;
using System.Windows;
using System.Xml;
using System.Xml.Linq;
using System.Diagnostics;
using Windows.Storage.Streams;
using System.Xml.Serialization;
using Windows.Data.Xml.Dom;
using CollectionView;
using Windows.UI;
using Windows.UI.Xaml.Media;


namespace EZEreaderUniversal.DataModels
{
    public class BooksModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        StorageFolder appFolder = ApplicationData.Current.LocalFolder;

        private void NotifyPropertyChanged(String p)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(p));
            }
        }
        private ListCollectionView _sortedBooks;
    
        public ListCollectionView SortedBooks
        { 
            get
            {
                return _sortedBooks;
            }
            set
            {
                _sortedBooks = value;
                NotifyPropertyChanged("SortedBooks");
            }
        }

        private ObservableCollection<BookModel> _library;
        public ObservableCollection<BookModel> Library
        {
            get 
            { 
                return _library;
            }
            set 
            { 
                _library = value;
                NotifyPropertyChanged("Library");
            }
        }


        private ListCollectionView _recentbooks;

        public ListCollectionView RecentBooks
        {
            get
            {
                return _recentbooks;
            }
            set
            {
                _recentbooks = value;
                NotifyPropertyChanged("RecentBooks");
            }
        }

        private ObservableCollection<BookModel> _recentreads;
        public ObservableCollection<BookModel> RecentReads
        {
            get
            {
                return _recentreads;
            }
            set
            {
                _recentreads = value;
                NotifyPropertyChanged("RecentReads");
            }
        }

        public bool IsDataLoaded
        {
            get;
            private set;
        }

        private int _readingfontsize;
        public int ReadingFontSize
        {
            get
            {
                return _readingfontsize;
            }
            set
            {
                if (value != _readingfontsize)
                {
                    _readingfontsize = value;
                    NotifyPropertyChanged("ReadingFontSize");
                }
            }
        }

        private string _readingfontfamily;
        public string ReadingFontFamily
        {
            get
            {
                return _readingfontfamily;
            }
            set
            {
                if (value != _readingfontfamily)
                {
                    _readingfontfamily = value;
                    NotifyPropertyChanged("ReadingFontFamily");
                }
            }
        }

        private SolidColorBrush _readingfontcolor;
        public SolidColorBrush ReadingFontColor
        {
            get
            {
                return _readingfontcolor;
            }
            set
            {
                if (value != _readingfontcolor)
                {
                    _readingfontcolor = value;
                    NotifyPropertyChanged("ReadingFontColor");
                }
            }
        }

        private SolidColorBrush _backgroundreadingcolor;
        public SolidColorBrush BackgroundReadingColor
        {
            get
            {
                return _backgroundreadingcolor;
            }
            set
            {
                if (value != _backgroundreadingcolor)
                {
                    _backgroundreadingcolor = value;
                    NotifyPropertyChanged("BackgroundReadingColor");
                }
            }
        }

        /// <summary>
        /// Sets the ListCollection to be sorted by book name.
        /// </summary>
        public void SortByBookNameAscending()
        {          
            SortedBooks.SortDescriptions.Add(new SortDescription("BookName", ListSortDirection.Ascending));
        }

        /// <summary>
        /// Sets the ListCollection to be sorted by most recently accessed book
        /// </summary>
        public void SortBooksByAccessDate()
        {
            RecentBooks.SortDescriptions.Add(new SortDescription("OpenedRecentlyTime", ListSortDirection.Descending));
        }

        /// <summary>
        /// Method to search for a book
        /// </summary>
        /// <param name="bookID"></param>
        /// <returns>BookModel class</returns>
        public BookModel GetItem(string bookID)
        {
            BookModel result = this.Library.Where(f => f.BookID == bookID).FirstOrDefault();
            return result;
        }

        /// <summary>
        /// imports a book into the datamodel
        /// </summary>
        /// <returns>BookModel class</returns>
        public async void ImportBook(string folderName, bool isInStorage)
        {
            string bookID = folderName;
            string directoryLoc = bookID + "/";
            string contentOPFLoc = await FindContentOPF(directoryLoc, isInStorage);
            string dateKey = DateTime.Now.Ticks.ToString();
            string tableOfContents = await GetTableOfContents(directoryLoc + contentOPFLoc, isInStorage);
            BookModel result = new BookModel() { 
                BookID = bookID,
                BookName = await FindTitle(directoryLoc + contentOPFLoc, isInStorage), 
                AuthorID = await FindAuthor(directoryLoc + contentOPFLoc, isInStorage),
                TableOfContents = tableOfContents,
                AddedDate = dateKey,
                CoverPic = await GetCoverPic(directoryLoc, isInStorage, contentOPFLoc),
                MainDirectory = directoryLoc,
                ContentDirectory = contentOPFLoc,
                Chapters = await ParseBookManifest(directoryLoc + contentOPFLoc, directoryLoc,
                tableOfContents, isInStorage),
                CurrentChapter = 0,
                CurrentPage = 0,
                IsoStore = isInStorage,
                IsStarted = false,
                IsCompleted = false
            };
            this.Library.Add(result);
            SortByBookNameAscending();
            //uncomment below line to allow for persistent data
            //CallUpdateBooks();
        }

        /// <summary>
        /// Finds the location and name of the cover pic of the book
        /// </summary>
        /// <param name="directoryLoc">location of book main folder</param>
        /// <param name="isInStorage">boolean if book is in storage or assets</param>
        /// <param name="contentOPFLoc">location of content.opf folder</param>
        /// <returns></returns>
        public async Task<string> GetCoverPic(string directoryLoc, bool isInStorage, string contentOPFLoc)
        {
            string coverPic;
            if (isInStorage)
            {
                coverPic = "isostore:" + directoryLoc + await GetStoragePicLocationFromContentOPF(directoryLoc, contentOPFLoc);
            }
            else
            {
               return coverPic = directoryLoc + "cover.jpeg";
            }
            return coverPic;
        }

        /// <summary>
        /// Method to find the location of the Table of Contents of the book
        /// </summary>
        /// <param name="contentOPF">string location of the content.opf file</param>
        /// <param name="isInStorage">bool that tells if location is assets or storage</param>
        /// <returns></returns>
        public async Task<string> GetTableOfContents(string contentOPF, bool isInStorage)
        {
            XDocument xdoc;
            string tableOfContents = "test";
            StorageFolder newFolder;
            string newContentOPF = contentOPF;
            if (isInStorage)
            {
                if (contentOPF.Contains('/'))
                {
                    string[] newContent = contentOPF.Split('/');
                    newFolder = await FindContentOPFFolder(contentOPF, newContent);
                    newContentOPF = newContent[newContent.Length - 1];
                }
                else
                {
                    newFolder = appFolder;
                }
                using (var file = await 
                    newFolder.OpenStreamForReadAsync(newContentOPF))
                {
                    xdoc = XDocument.Load(file);
                    XNamespace ns = "http://www.idpf.org/2007/opf";
                    var tocLoc = from x in xdoc.Descendants()
                                       where (string)x.Attribute("type") == "toc"
                                       select (string)x.Attribute("href");

                    foreach (string s in tocLoc)
                    {
                        if (s != null)
                        {
                            tableOfContents = s;
                        }
                    }
                }
            
            }
            else
            {
                xdoc = XDocument.Load(contentOPF);
                XNamespace ns = "http://www.idpf.org/2007/opf";
                var tocLoc = from q in xdoc.Descendants()
                             where (string)q.Attribute("type") == "toc"
                             select (string)q.Attribute("href");
                foreach (string s in tocLoc)
                {
                    if (s != null)
                    {
                        tableOfContents = s;
                        
                    }
                }
            }
            if (tableOfContents.Contains("#"))
            {
                int index = tableOfContents.IndexOf('#');
                tableOfContents = tableOfContents.Substring(0, index);
            }
            return tableOfContents;
        }

        /// <summary>
        /// Reads the table of contents to find out which chapters are associated
        /// with which html files.
        /// </summary>
        /// <param name="tocLoc"></param>
        /// <param name="isInStrage"></param>
        /// <returns></returns>
        private async Task<List<string[]>> GetChapterNamesFromTOC(string tocLoc, bool isInStrage)
        {
            XDocument xdoc;
            List<string[]> chapters = new List<string[]>();
            string[] arrayOfChapter;
            StorageFolder storageFolder;
            string newTocLoc = tocLoc;
            if (isInStrage)
            {
                if (newTocLoc.Contains('/'))
                {
                    string[] splitTocLoc = newTocLoc.Split('/');
                    storageFolder = await FindContentOPFFolder(tocLoc, splitTocLoc);
                    newTocLoc = splitTocLoc[splitTocLoc.Length - 1];
                }
                else
                {
                    storageFolder = appFolder;
                }
                using (var file = await storageFolder.OpenStreamForReadAsync(newTocLoc))
                {
                    xdoc = XDocument.Load(file);
                    XNamespace ns = "http://www.w3.org/1999/xhtml";

                    var chapterNames = from x in xdoc.Root.Element(ns + "body").Descendants()
                                       select new ChapterToC
                                       (
                                           x.Value,
                                           (string)x.Attribute("href")
                                       );

                    foreach (var test in chapterNames)
                    {
                        //only keeps the chapter names that have a chapter html file associated
                        if (test.ChapterName != null && test.ChapterString != null)
                        {
                            arrayOfChapter = new string[2];
                            if (test.ChapterString.Contains('#'))
                            {
                                arrayOfChapter[0] = test.ChapterString.Substring(0, test.ChapterString.IndexOf('#'));
                            }
                            else
                            {
                                arrayOfChapter[0] = test.ChapterString;
                            }
                            arrayOfChapter[1] = test.ChapterName;
                            chapters.Add(arrayOfChapter);
                        }
                    }
                   
                }
            }
            else
            {
                xdoc = XDocument.Load(tocLoc);
                XNamespace ns = "http://www.w3.org/1999/xhtml";

                var chapterNames = from x in xdoc.Root.Element(ns + "body").Descendants()
                                       select new ChapterToC
                                       (
                                           x.Value,
                                           (string)x.Attribute("href")
                                       );

                foreach (var test in chapterNames)
                {
                    if (test.ChapterName != null && test.ChapterString != null)
                    {
                        arrayOfChapter = new string[2];
                        if (test.ChapterString.Contains('#'))
                        {
                            arrayOfChapter[0] = test.ChapterString.Substring(0, test.ChapterString.IndexOf('#'));
                        }
                        else
                        {
                            arrayOfChapter[0] = test.ChapterString;
                        }
                        arrayOfChapter[1] = test.ChapterName;
                        chapters.Add(arrayOfChapter);
                    }
                }
                
            }
            return chapters;
        }

        /// <summary>
        /// parse the content.opf file for the location/name of the cover pic if the book is in storage
        /// </summary>
        /// <param name="directoryLoc">location of book</param>
        /// <param name="contentOPFLoc">location of content.opf file</param>
        /// <returns>string of the location of cover picture</returns>
        public async Task<string> GetStoragePicLocationFromContentOPF(string directoryLoc, string contentOPFLoc)
        {
            XDocument xdoc;
            string contentOPF = directoryLoc + contentOPFLoc;
            StorageFolder newFolder;
            string newContentOPF = contentOPF;
            string coverPic = "test";
            if (contentOPF.Contains('/'))
            {
                string[] newContent = contentOPF.Split('/');
                newFolder = await FindContentOPFFolder(contentOPF, newContent);
                newContentOPF = newContent[newContent.Length - 1];
            }
            else
            {
                newFolder = appFolder;
            }
            using (var file = await
                newFolder.OpenStreamForReadAsync(newContentOPF))
            {
                xdoc = XDocument.Load(file);
                XNamespace ns = "http://www.idpf.org/2007/opf";
                var manifestHref = from x in xdoc.Descendants()
                                   where (string)x.Attribute("id") == "cover"
                                   select (string)x.Attribute("href");

                foreach (string s in manifestHref)
                {
                    if (s != null)
                    {
                        coverPic = s;
                    }
                }
            }
            return coverPic;
        }

        /// <summary>
        /// Finds the location of the content.opf file which olds all the information needed to parse
        /// so that you can find chapters, cover page, and any other items you might want or need.
        /// </summary>
        /// <param name="directoryLoc"></param>
        /// <returns></returns>
        private async Task<string> FindContentOPF(string directoryLoc, bool isInStorage)
        {
            XDocument xdoc;
            if (isInStorage)
            {
                string newDirectory = directoryLoc.Substring(0, directoryLoc.Length - 1);
                StorageFolder folder = await appFolder.GetFolderAsync(newDirectory);
                StorageFolder metaFolder = await folder.GetFolderAsync("META-INF");
                StorageFile file = await metaFolder.GetFileAsync("container.xml");

                using (Stream fileStream = await file.OpenStreamForReadAsync())
                {
                    xdoc = XDocument.Load(fileStream);

                    var contentOPFLoc = from q in xdoc.Descendants()
                                        select (string)q.Attribute("full-path");
                    string contentOPF = "test";
                    foreach (string s in contentOPFLoc)
                    {
                        if (s != null)
                        {
                            contentOPF = s;
                        }
                    }

                    return contentOPF;
                }
                
            }
            else
            {
                xdoc = XDocument.Load(directoryLoc + "META-INF/container.xml");

                var contentOPFLoc = from q in xdoc.Descendants()
                                    select (string)q.Attribute("full-path");
                string contentOPF = "test";
                foreach (string s in contentOPFLoc)
                {
                    if (s != null)
                    {
                        contentOPF = s;
                    }
                }

                return contentOPF;
            }
        }

        /// <summary>
        /// Parse the content.opf xml file for the authors name and arrange it to show
        /// the first name then the last name
        /// </summary>
        /// <param name="contentOPF">directory of the content.opf file</param>
        /// <returns>First name and last name as a single string</returns>
        private async Task<string> FindAuthor(string contentOPF, bool isInStorage)
        {
            XDocument xdoc;
            string authorName = "test";
            StorageFolder newFolder;
            string newContentOPF = contentOPF;
            if (isInStorage)
            {
                if (contentOPF.Contains('/'))
                {
                    string[] newContent = contentOPF.Split('/');
                    newFolder = await FindContentOPFFolder(contentOPF, newContent);
                    newContentOPF = newContent[newContent.Length - 1];
                }
                else
                {
                    newFolder = appFolder;
                }
                using (var file = await 
                    newFolder.OpenStreamForReadAsync(newContentOPF))
                {
                    xdoc = XDocument.Load(file);
                    XNamespace ns = "http://www.idpf.org/2007/opf";
                    var author = from q in xdoc.Descendants()
                                 select (string)q.Attribute(ns + "file-as");

                    foreach (string s in author)
                    {
                        if (s != null)
                        {
                            authorName = s;
                        }
                    }
                }
            }
            else
            {
                xdoc = XDocument.Load(contentOPF);
                XNamespace ns = "http://www.idpf.org/2007/opf";
                var author = from q in xdoc.Descendants()
                             select (string)q.Attribute(ns + "file-as");

                
                foreach (string s in author)
                {
                    if (s != null)
                    {
                        authorName = s;
                    }
                }
            }
            // if there is a comma, then remove it and flip names
            // so Austen, Jane becaomes Jane Austen
            if (authorName.Contains(","))
            {
                string[] splitName = authorName.Split(',');
                string[] splitNameTwo = splitName[1].Split(' ');
                return splitNameTwo[1] + " " + splitName[0];
            }
            else
            {
                return authorName;
            }
        }

        /// <summary>
        /// Finds the folder that contains the content.opf folder for loading the file
        /// </summary>
        /// <param name="contentOPF"></param>
        /// <param name="newContent"></param>
        /// <returns></returns>
        private async Task<StorageFolder> FindContentOPFFolder(string contentOPF, string[] newContent)
        {
            StorageFolder directoryFolder;
            StorageFolder folderOne;
            StorageFolder folderTwo;
            StorageFolder folderThree;

            
            if (newContent.Length == 2)
            {
                return await IO.CreateOrGetFolder(newContent[0], appFolder);
            }
            else if (newContent.Length == 3)
            {
                directoryFolder = await IO.CreateOrGetFolder(newContent[0], appFolder);
                return await IO.CreateOrGetFolder(newContent[1], directoryFolder);
            }
            else if (newContent.Length == 4)
            {
                directoryFolder = await IO.CreateOrGetFolder(newContent[0], appFolder);
                folderOne = await IO.CreateOrGetFolder(newContent[1], directoryFolder);
                return await IO.CreateOrGetFolder(newContent[2], folderOne);
            }
            else if (newContent.Length == 5)
            {
                directoryFolder = await IO.CreateOrGetFolder(newContent[0], appFolder);
                folderOne = await IO.CreateOrGetFolder(newContent[1], directoryFolder);
                folderTwo = await IO.CreateOrGetFolder(newContent[2], folderOne);
                return await IO.CreateOrGetFolder(newContent[3], folderTwo);
            }
            else
            {
                directoryFolder = await IO.CreateOrGetFolder(newContent[0], appFolder);
                folderOne = await IO.CreateOrGetFolder(newContent[1], directoryFolder);
                folderTwo = await IO.CreateOrGetFolder(newContent[2], folderOne);
                folderThree = await IO.CreateOrGetFolder(newContent[3], folderTwo);
                return await IO.CreateOrGetFolder(newContent[4], folderThree);
            }
        }

        /// <summary>
        /// Find the book's title from the content.opf xml by parsing
        /// </summary>
        /// <param name="contentOPF">content.opf file location string</param>
        /// <returns>title of the book as a string</returns>
        private async Task<string> FindTitle(string contentOPF, bool isInStorage)
        {
            List<string> bookTitle = new List<string>();
            StorageFolder newFolder;
            string newContentOPF = contentOPF;
            XmlDocument xdoc;
            if (isInStorage)
            {
                if (contentOPF.Contains('/'))
                {
                    string[] newContent = contentOPF.Split('/');
                    newFolder = await FindContentOPFFolder(contentOPF, newContent);
                    newContentOPF = newContent[newContent.Length -1];
                }
                else
                {
                    newFolder = appFolder;
                }
                var file = await
                newFolder.GetFileAsync(newContentOPF);
                
                xdoc = await XmlDocument.LoadFromFileAsync(file);

                var content = xdoc.GetElementsByTagName("dc:title");
                    
                foreach (var s in content)
                {
                    if (s != null)
                    {
                        bookTitle.Add(s.InnerText);
                    }
                }
            }
            else
            {
                XDocument xdoc1 = XDocument.Load(contentOPF);

                var content = from q in xdoc1.Descendants()
                              select (string)q.Attribute("content");


                foreach (string s in content)
                {
                    if (s != null)
                    {
                        bookTitle.Add(s);
                    }
                }
            }
            // If title is separated by a comma, then remove it and flip the strings
            // useful when title recorded as War of Worlds, The to make The War of Worlds
            if (bookTitle[0].Contains(","))
            {
                if (bookTitle[0].Contains("(") || bookTitle[0].Contains(")"))
                {
                    int indexOfParen = bookTitle[0].IndexOf('(');
                    int indexOfClosingParen = bookTitle[0].IndexOf(')');                    
                    string splitBookTitle = bookTitle[0].Substring(indexOfParen +1);
                    string[] splitTitle = splitBookTitle.Split(',');
                    string[] splitTitleTwo = splitTitle[1].Split(' ');
                    return bookTitle[0].Substring(0, indexOfParen +1) + 
                        splitTitleTwo[1].Substring(0, splitTitleTwo[1].Length -1) + " " + 
                        splitTitle[0] + bookTitle[0].Substring(indexOfClosingParen);
                }
                else
                {
                    string[] splitTitle = bookTitle[0].Split(',');
                    string[] splitTitleTwo = splitTitle[1].Split(' ');
                    return splitTitleTwo[1] + " " + splitTitle[0];
                }
            }
            else
            {
                return bookTitle[0];
            }
        }

        /// <summary>
        /// Method to parse the manifest portion of the content.opf
        /// </summary>
        /// <param name="contentOPF">string of content.opf directory</param>
        /// <returns>List</string></returns>
        private async Task<List<ChapterModel>> ParseBookManifest(string contentOPF, string directoryLoc,
            string tocLoc, bool isInStorage)
        {
            List<string> manifestHrefs = new List<string>();
            List<string> manifestIDs = new List<string>();
            List<string> spineIdrefs = new List<string>();
            List<ChapterModel> chapterCollection = new List<ChapterModel>();
            IEnumerable<string> idrefs;
            IEnumerable<string> manifestHref;
            IEnumerable<string> manifestID;
            XDocument xdoc;
            StorageFolder newFolder;
            string newContentOPF = contentOPF;
            if (isInStorage)
            {
                if (contentOPF.Contains('/'))
                {
                    string[] newContent = contentOPF.Split('/');
                    newFolder = await FindContentOPFFolder(contentOPF, newContent);
                    newContentOPF = newContent[newContent.Length - 1];
                }
                else
                {
                    newFolder = appFolder;
                }
                using (var file = await
                    newFolder.OpenStreamForReadAsync(newContentOPF))
                {
                    xdoc = XDocument.Load(file);
                    //parse for idrefs in the manifest section
                     idrefs = from q in xdoc.Descendants()
                                 select (string)q.Attribute("idref");

                    //parse for chapter strings in maniftest
                    manifestHref = from x in xdoc.Descendants()
                                       select (string)x.Attribute("href");

                    //parse for the ids in spine for chapter order
                    manifestID = from x in xdoc.Descendants()
                                     select (string)x.Attribute("id");
                }
            }
            else
            {
                xdoc = XDocument.Load(contentOPF);
                //parse for idrefs in the manifest section
                idrefs = from q in xdoc.Descendants()
                             select (string)q.Attribute("idref");

                //parse for chapter strings in maniftest
                manifestHref = from x in xdoc.Descendants()
                                   select (string)x.Attribute("href");

                //parse for the ids in spine for chapter order
                manifestID = from x in xdoc.Descendants()
                                 select (string)x.Attribute("id");
            }

            foreach (string q in manifestHref)
            {
                if (q != null)
                {
                    manifestHrefs.Add(q);
                }
            }
            
            foreach (string p in manifestID)
            {
                if (p != null)
                {
                    manifestIDs.Add(p);
                }
            }

            //accepts a list of the chapters and their associated html files
            var chapterNames = await GetChapterNamesFromTOC(directoryLoc + tocLoc, isInStorage);

            // Will check id of chapters in the manifest against the ids in the spine
            // to find the correct order of the chapters and then adds them to chaptermodel
            int chapterID = 0;
            foreach (string s in idrefs)
            {
                for (int i = 0; i < manifestIDs.Count(); i++ )
                {
                    if (s == manifestIDs[i])
                    {
                        string chapterName = "";
                        string checkChapterName = manifestHrefs[i -1];

                        if (manifestHrefs[i-1] == "titlepage.xhtml")
                        {
                            chapterName = "Cover Page";
                        }
                        else if (tocLoc == manifestHrefs[i - 1])
                        {
                            chapterName = "Table of Contents";
                        }
                        else
                        {
                            if (checkChapterName.Contains('/'))
                            {

                                string[] manifestBreak = checkChapterName.Split('/');
                                checkChapterName = manifestBreak[manifestBreak.Length - 1];
                            }

                            foreach (string[] t in chapterNames)
                            {
                                if (checkChapterName == t[0])
                                {
                                    chapterName = t[1];
                                }
                            }
                        }
                        chapterCollection.Add(new ChapterModel()
                        {
                            ChapterName = chapterName,
                            ChapterID = chapterID,
                            ChapterString = manifestHrefs[i - 1]
                        });
                    }
                    chapterID++;
                }
            }

            return chapterCollection;
        }

        /// <summary>
        /// Method to remove a book from the library
        /// </summary>
        /// <param name="bookToRemove"></param>
        public async Task RemoveBook(BookModel bookToRemove)
        {
            if (bookToRemove.IsoStore == true)
            {
                await IO.DeleteFolderInLocalFolder(bookToRemove.BookID);
            }
            this.Library.Remove(bookToRemove);
            CallUpdateBooks();
            NotifyPropertyChanged("Items");
        }

        /// <summary>
        /// async void method for updating books, calls the real method
        /// </summary>
        public async void CallLoadData()
        {
            await LoadData();
        }
        
        /// <summary>
        /// async void method for updating books, calls the real method
        /// </summary>
        public async void CallUpdateBooks()
        {
            await UpdateBooks();
        }
        
        /// <summary>
        /// Method to update the library each time we import/delete a book
        /// todo: add a function that saves the page you are on
        /// </summary>
        /// <returns>Task</returns>
        public async Task UpdateBooks()
        {
            StorageFolder appStorageFolder = IO.GetAppStorageFolder();
            await IO.DeleteFileInFolder(appStorageFolder, "librarybooks.xml");
            await IO.DeleteFileInFolder(appStorageFolder, "recentreads.xml");
            string libraryBooksAsXML = IO.SerializeToString(this.Library);
            string recentReadsAsXML = IO.SerializeToString(this.RecentReads);
            StorageFile dataFile = await IO.CreateFileInFolder(appStorageFolder, "librarybooks.xml");
            StorageFile recentReadsFile = await IO.CreateFileInFolder(appStorageFolder, "recentreads.xml");
            await IO.WriteStringToFile(dataFile, libraryBooksAsXML);
            await IO.WriteStringToFile(recentReadsFile, recentReadsAsXML);
        }

        /// <summary>
        /// Method to load the data on app startup,
        /// checks for current file, creates a default if doesn't exist
        /// </summary>
        /// <returns>Task</returns>
        public async Task LoadData()
        {
            //uncomment below to clear the library
            await IO.DeleteAllFilesInLocalFolder();
            StorageFolder appStorageFolder = ApplicationData.Current.LocalFolder;
            StorageFile dataFile = await IO.GetFileInFolder(appStorageFolder, "librarybooks.xml");
            StorageFile recentReadsFile = await IO.GetFileInFolder(appStorageFolder, "recentreads.xml");
            
            if (dataFile != null)
            {
                
                if (!IsDataLoaded)
                {
                    string itemsAsXML = await IO.ReadStringFromFile(dataFile);
                    this.Library = IO.SerializeFromString<ObservableCollection<BookModel>>(itemsAsXML);
                    this.SortedBooks = new ListCollectionView(this.Library);
                }
                 
            }
            else
            {
            
                if (!IsDataLoaded)
                {
                    
                    this.Library = new ObservableCollection<BookModel>();
                    this.SortedBooks = new ListCollectionView(this.Library);
                    this.ReadingFontSize = 20;
                    this.ReadingFontFamily = "Segoe UI";
                    this.ReadingFontColor = new SolidColorBrush(Colors.Black);
                    this.BackgroundReadingColor = new SolidColorBrush(Colors.White);
                    ImportBook("Pride and Prejudice - Jane Austen_6590", false);
                    
                }
            }

            if (recentReadsFile != null)
            {

                if (!IsDataLoaded)
                {
                    string recentReadsAsXML = await IO.ReadStringFromFile(dataFile);
                    this.RecentReads = IO.SerializeFromString<ObservableCollection<BookModel>>(recentReadsAsXML);
                    this.RecentBooks = new ListCollectionView(this.RecentReads);
                }
            }
            else
            {

                if (!IsDataLoaded)
                {

                    this.RecentReads = new ObservableCollection<BookModel>();
                    this.RecentBooks = new ListCollectionView(this.RecentReads);
                    
                }
            }
            CallUpdateBooks();
            NotifyPropertyChanged("Items");
            this.IsDataLoaded = true;
        }
    }
}
