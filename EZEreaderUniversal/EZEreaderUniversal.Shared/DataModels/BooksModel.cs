using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using System.IO;
using System.Xml.Linq;
using Windows.Data.Xml.Dom;
using Windows.UI;
using Windows.UI.Xaml.Media;
using System.Reflection;


namespace EZEreaderUniversal.DataModels
{
    public class BooksModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        readonly StorageFolder _appFolder = ApplicationData.Current.LocalFolder;
        Dictionary<String, SolidColorBrush> _allColorBrushes;

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

        private ReadingFontsAndFamilies _readingfonts;
        public ReadingFontsAndFamilies ReadingFonts
        {
            get
            {
                return _readingfonts;
            }
            set
            {
                if (value != _readingfonts)
                {
                    _readingfonts = value;
                    NotifyPropertyChanged("ReadingFonts");
                }
            }
        }

        private string _readingfontcolorname;
        public string ReadingFontColorName
        {
            get
            {
                return _readingfontcolorname;
            }
            set
            {
                if (value != _readingfontcolorname)
                {
                    _readingfontcolorname = value;
                    NotifyPropertyChanged("ReadingFontColorName");
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

        private string _backgroundreadingcolorname;
        public string BackgroundReadingColorName
        {
            get
            {
                return _backgroundreadingcolorname;
            }
            set
            {
                if (value != _backgroundreadingcolorname)
                {
                    _backgroundreadingcolorname = value;
                    NotifyPropertyChanged("BackgroundReadingColorName");
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
        /// Finds all the available colors located on the phone for saving and loading
        /// the chosen background and font colors by the reader.
        /// </summary>
        private void SetColors()
        {
            var colors = typeof(Colors).GetRuntimeProperties().ToList();
            _allColorBrushes = new Dictionary<string, SolidColorBrush>();
            foreach (PropertyInfo color in colors)
            {
                Color testColor = (Color)color.GetValue(null, null);
                string colorName = color.Name;
                SolidColorBrush brush = new SolidColorBrush(testColor);
                _allColorBrushes.Add(colorName, brush);
            }
            List<string> allColorNames = new List<string>();
            foreach (string key in _allColorBrushes.Keys)
            {
                allColorNames.Add(key);
            }
        }

        /// <summary>
        /// Method to search for a book
        /// </summary>
        /// <param name="bookId"></param>
        /// <returns>BookModel class</returns>
        public BookModel GetItem(string bookId)
        {
            BookModel result = Library.FirstOrDefault(f => f.BookID == bookId);
            return result;
        }

        /// <summary>
        /// imports a book into the datamodel
        /// </summary>
        /// <returns>BookModel class</returns>
        public async Task<bool> ImportBook(string folderName, bool isInStorage)
        {
            bool isInLibrary = false;
            string bookId = folderName;
            string directoryLoc = bookId + "/";
            string contentOpfLoc = await FindContentOpf(directoryLoc, isInStorage);
            string dateKey = DateTime.Now.Ticks.ToString();
            string tableOfContentsNcx = await GetNcxTableOfContents(directoryLoc + contentOpfLoc, isInStorage);
            List<string[]> ncxTableOfContents = await GetChaptersFromNcxtoC(directoryLoc + tableOfContentsNcx, contentOpfLoc, isInStorage);
            //string tableOfContents = await GetTableOfContents(directoryLoc + contentOPFLoc, isInStorage);
            var result = new BookModel
            { 
                BookID = bookId,
                BookName = await FindTitle(directoryLoc + contentOpfLoc, isInStorage), 
                AuthorID = await FindAuthor(directoryLoc + contentOpfLoc, isInStorage),
                TableOfContents = tableOfContentsNcx,
                AddedDate = dateKey,
                CoverPic = await GetCoverPic(directoryLoc, isInStorage, contentOpfLoc),
                MainDirectory = directoryLoc,
                ContentDirectory = contentOpfLoc,
                Chapters = await ParseBookManifest(directoryLoc + contentOpfLoc, ncxTableOfContents, isInStorage),
                CurrentChapter = 0,
                CurrentPage = 0,
                IsoStore = isInStorage,
                IsStarted = false,
                IsCompleted = false
            };
            //checks to see if the book already exists
            if (isInStorage)
            {
                foreach (var book in Library)
                {
                    if (book.BookName == result.BookName)
                    {
                        if (book.AuthorID == result.AuthorID)
                        {
                            isInLibrary = true;
                            //checks to see if its the same folder
                            //if not, delete the new one to clear up space
                            if (result.BookID != book.BookID)
                            {
                                await Io.DeleteFolderInLocalFolder(result.BookID);
                            }

                        }
                    }
                }
            }
            if (!isInLibrary)
            {
                Library.Add(result);
                SortByBookNameAscending();
                //uncomment below line to allow for persistent data
                if (IsDataLoaded)
                {
                    await UpdateBooks();
                }
                return false;
            }
            else
            {
                return true;
            }
            
        }

        /// <summary>
        /// Finds the location and name of the cover pic of the book
        /// </summary>
        /// <param name="directoryLoc">location of book main folder</param>
        /// <param name="isInStorage">boolean if book is in storage or assets</param>
        /// <param name="contentOpfLoc">location of content.opf folder</param>
        /// <returns></returns>
        public async Task<string> GetCoverPic(string directoryLoc, bool isInStorage, string contentOpfLoc)
        {
            string coverPic;
            if (isInStorage)
            {
                coverPic = "isostore:" + await GetStoragePicLocationFromContentOpf(directoryLoc, contentOpfLoc);
            }
            else
            {
               return directoryLoc + "cover.jpeg";
            }
            return coverPic;
        }

        /// <summary>
        /// Method to find the location of the Table of Contents of the book
        /// </summary>
        /// <param name="contentOpf">string location of the content.opf file</param>
        /// <param name="isInStorage">bool that tells if location is assets or storage</param>
        /// <returns></returns>
        public async Task<string> GetTableOfContents(string contentOpf, bool isInStorage)
        {
            XDocument xdoc;
            string tableOfContents = "test";
            string newContentOpf = contentOpf;
            if (isInStorage)
            {
                StorageFolder newFolder;
                if (contentOpf.Contains('/'))
                {
                    string[] newContent = contentOpf.Split('/');
                    newFolder = await FindContentOpfFolder(newContent);
                    newContentOpf = newContent[newContent.Length - 1];
                }
                else
                {
                    newFolder = _appFolder;
                }
                using (var file = await 
                    newFolder.OpenStreamForReadAsync(newContentOpf))
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
                xdoc = XDocument.Load(contentOpf);
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
        /// Method to find the location of the Table of Contents of the book
        /// </summary>
        /// <param name="contentOpf">string location of the content.opf file</param>
        /// <param name="isInStorage">bool that tells if location is assets or storage</param>
        /// <returns></returns>
        public async Task<string> GetNcxTableOfContents(string contentOpf, bool isInStorage)
        {
            XDocument xdoc;
            string tableOfContents = "test";
            StorageFolder newFolder;
            string newContentOpf = contentOpf;
            if (isInStorage)
            {
                if (contentOpf.Contains('/'))
                {
                    string[] newContent = contentOpf.Split('/');
                    newFolder = await FindContentOpfFolder(newContent);
                    newContentOpf = newContent[newContent.Length - 1];
                }
                else
                {
                    newFolder = _appFolder;
                }
                using (var file = await
                    newFolder.OpenStreamForReadAsync(newContentOpf))
                {
                    xdoc = XDocument.Load(file);
                    XNamespace ns = "http://www.idpf.org/2007/opf";
                    var tocLoc = from x in xdoc.Descendants()
                                 where (string)x.Attribute("id") == "ncx"
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
                xdoc = XDocument.Load(contentOpf);
                XNamespace ns = "http://www.idpf.org/2007/opf";
                var tocLoc = from q in xdoc.Descendants()
                             where (string)q.Attribute("id") == "ncx"
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
        /// Grabs the chapter names and order of chapters from the Table of Contents
        /// </summary>
        /// <param name="ncxToCLoc">location of toc.ncx file</param>
        /// <param name="contentOpfLoc">location of content.opf file</param>
        /// <param name="isInStorage">boolean if book is in storage</param>
        /// <returns>list of strings[] with chaptername and chapterlocation</returns>
        private async Task<List<string[]>> GetChaptersFromNcxtoC(string ncxToCLoc, string contentOpfLoc, bool isInStorage)
        {
            List<string[]> tocIndex = new List<string[]>();
            XDocument xdoc;
            string[] arrayOfChapter;
            List<string> fullTocLoc;
            StorageFolder storageFolder;
            string newTocLoc = ncxToCLoc;
            if (isInStorage)
            {
                if (newTocLoc.Contains('/'))
                {
                    string[] splitTocLoc = newTocLoc.Split('/');
                    if (contentOpfLoc.Contains('/'))
                    {
                        fullTocLoc = new List<string>();
                        string[] contentOpfDir = contentOpfLoc.Split('/');
                        for (int i = 0; i < splitTocLoc.Length - 1; i++)
                        {
                            fullTocLoc.Add(splitTocLoc[i]);
                        }
                        for (int i = 0; i < contentOpfDir.Length - 1; i++)
                        {
                            fullTocLoc.Add(contentOpfDir[i]);
                        }
                        fullTocLoc.Add(splitTocLoc[splitTocLoc.Length - 1]);
                        splitTocLoc = fullTocLoc.ToArray();
                    }
                    storageFolder = await FindContentOpfFolder(splitTocLoc);
                    newTocLoc = splitTocLoc[splitTocLoc.Length - 1];
                }
                else
                {
                    storageFolder = _appFolder;
                }
                using (var file = await storageFolder.OpenStreamForReadAsync(newTocLoc))
                {
                    xdoc = XDocument.Load(file);
                    XNamespace ns = "http://www.daisy.org/z3986/2005/ncx/";

                    var chapterNames = from x in xdoc.Root.Element(ns + "navMap").Descendants(ns + "navLabel")
                                       select new ChapterToC
                                       (
                                           x.Value,
                                           (string)x.Attribute("src")
                                       );
                    var chapterStrings = from q in xdoc.Root.Element(ns + "navMap").Descendants()
                                         select (string)q.Attribute("src");

                    var chapterToCs = chapterNames as ChapterToC[] ?? chapterNames.ToArray();
                    var chaptersNames = chapterToCs.ToArray();
                    List<string> chaptersString = new List<string>();
                    foreach (var newString in chapterStrings)
                    {
                        if (newString != null)
                        {
                            chaptersString.Add(newString);
                        }
                    }
                    
                    for (int i = 0; i < chapterToCs.Count(); i++)
                    {
                        chaptersNames[i].ChapterString = chaptersString[i];
                    }

                    foreach (var test in chaptersNames)
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
                        tocIndex.Add(arrayOfChapter);
                    }
                }
                
            }
            else
            {
                xdoc = XDocument.Load(ncxToCLoc);
                XNamespace ns = "http://www.daisy.org/z3986/2005/ncx/";

                var chapterNames = from x in xdoc.Root.Element(ns + "navMap").Descendants(ns + "navLabel")
                                   select new ChapterToC
                                   (
                                       x.Value,
                                       (string)x.Attribute("src")
                                   );
                var chapterStrings = from q in xdoc.Root.Element(ns + "navMap").Descendants()
                                     select (string)q.Attribute("src");

                var chapterToCs = chapterNames as ChapterToC[] ?? chapterNames.ToArray();
                var chaptersNames = chapterToCs.ToArray();
                var chaptersString = chapterStrings.Where(newString => newString != null).ToList();

                for (var i = 0; i < chapterToCs.Count(); i++)
                {
                    chaptersNames[i].ChapterString = chaptersString[i];
                }

                foreach (var test in chaptersNames)
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
                    tocIndex.Add(arrayOfChapter);
                }
            }

            return tocIndex;
        }
        /*
        /// <summary>
        /// Reads the table of contents to find out which chapters are associated
        /// with which html files.
        /// </summary>
        /// <param name="tocLoc"></param>
        /// <param name="isInStrage"></param>
        /// <returns></returns>
        private async Task<List<string[]>> GetChapterNamesFromTOC(string tocLoc, bool isInStorage)
        {
            XDocument xdoc;
            List<string[]> chapters = new List<string[]>();
            string[] arrayOfChapter;
            StorageFolder storageFolder;
            string newTocLoc = tocLoc;
            if (isInStorage)
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
        */

        /// <summary>
        /// parse the content.opf file for the location/name of the cover pic if the book is in storage
        /// </summary>
        /// <param name="directoryLoc">location of book</param>
        /// <param name="contentOpfLoc">location of content.opf file</param>
        /// <returns>string of the location of cover picture</returns>
        public async Task<string> GetStoragePicLocationFromContentOpf(string directoryLoc, string contentOpfLoc)
        {
            XDocument xdoc;
            string contentOpf = directoryLoc + contentOpfLoc;
            StorageFolder newFolder;
            string newContentOpf = contentOpf;
            string coverPic = "test";
            if (contentOpf.Contains('/'))
            {
                string[] newContent = contentOpf.Split('/');
                newFolder = await FindContentOpfFolder(newContent);
                newContentOpf = newContent[newContent.Length - 1];
                coverPic = "";
                for(int i = 0; i < newContent.Length -1; i ++)
                {
                    coverPic += newContent[i];
                    coverPic += "/";
                }
            }
            else
            {
                newFolder = _appFolder;
            }
            using (var file = await
                newFolder.OpenStreamForReadAsync(newContentOpf))
            {
                xdoc = XDocument.Load(file);
                XNamespace ns = "http://www.idpf.org/2007/opf";
                var manifestHref = from x in xdoc.Descendants()
                                   where (string)x.Attribute("name") == "cover"
                                   select (string)x.Attribute("content");
                string metaCoverName = "";
                foreach (string s in manifestHref)
                {
                    if (s != null)
                    {
                        metaCoverName = s;
                    }
                }
                if (metaCoverName != "")
                {
                    var findCoverPic = from q in xdoc.Descendants()
                                       where (string)q.Attribute("id") == metaCoverName
                                       select (string)q.Attribute("href");
                    foreach (string test in findCoverPic)
                    {
                        if (test != null)
                        {
                            coverPic += test;
                        }
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
        /// <param name="isInStorage"></param>
        /// <returns></returns>
        private async Task<string> FindContentOpf(string directoryLoc, bool isInStorage)
        {
            XDocument xdoc;
            if (isInStorage)
            {
                string newDirectory = directoryLoc.Substring(0, directoryLoc.Length - 1);
                StorageFolder folder = await _appFolder.GetFolderAsync(newDirectory);
                StorageFolder metaFolder = await folder.GetFolderAsync("META-INF");
                StorageFile file = await metaFolder.GetFileAsync("container.xml");

                using (Stream fileStream = await file.OpenStreamForReadAsync())
                {
                    xdoc = XDocument.Load(fileStream);

                    var contentOpfLoc = from q in xdoc.Descendants()
                                        select (string)q.Attribute("full-path");
                    string contentOpf = "test";
                    foreach (string s in contentOpfLoc)
                    {
                        if (s != null)
                        {
                            contentOpf = s;
                        }
                    }

                    return contentOpf;
                }
                
            }
            else
            {
                xdoc = XDocument.Load(directoryLoc + "META-INF/container.xml");

                var contentOpfLoc = from q in xdoc.Descendants()
                                    select (string)q.Attribute("full-path");
                string contentOpf = "test";
                foreach (string s in contentOpfLoc)
                {
                    if (s != null)
                    {
                        contentOpf = s;
                    }
                }

                return contentOpf;
            }
        }

        /// <summary>
        /// Parse the content.opf xml file for the authors name and arrange it to show
        /// the first name then the last name
        /// </summary>
        /// <param name="contentOpf">directory of the content.opf file</param>
        /// <param name="isInStorage"></param>
        /// <returns>First name and last name as a single string</returns>
        private async Task<string> FindAuthor(string contentOpf, bool isInStorage)
        {
            string authorName = "test";
            string newContentOpf = contentOpf;
            if (isInStorage)
            {
                StorageFolder newFolder;
                if (contentOpf.Contains('/'))
                {
                    string[] newContent = contentOpf.Split('/');
                    newFolder = await FindContentOpfFolder(newContent);
                    newContentOpf = newContent[newContent.Length - 1];
                }
                else
                {
                    newFolder = _appFolder;
                }
                var file = await
                newFolder.GetFileAsync(newContentOpf);
                XNamespace ns = "http://www.idpf.org/2007/opf";
                XmlDocument xdoc = await XmlDocument.LoadFromFileAsync(file);

                var author = xdoc.GetElementsByTagName("dc:creator");

                foreach (var s in author)
                {
                    if (s != null)
                    {
                        authorName = s.InnerText;
                    }
                }
                
            }
            else
            {
                XDocument xdoc1 = XDocument.Load(contentOpf);
                XNamespace ns = "http://www.idpf.org/2007/opf";
                var author = from q in xdoc1.Descendants()
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
        /// <param name="newContent"></param>
        /// <returns></returns>
        private async Task<StorageFolder> FindContentOpfFolder(string[] newContent)
        {
            StorageFolder directoryFolder;
            StorageFolder folderOne;
            StorageFolder folderTwo;
            StorageFolder folderThree;

            switch (newContent.Length)
            {
                case 2:
                    return await Io.CreateOrGetFolder(newContent[0], _appFolder);
                case 3:
                    directoryFolder = await Io.CreateOrGetFolder(newContent[0], _appFolder);
                    return await Io.CreateOrGetFolder(newContent[1], directoryFolder);
                case 4:
                    directoryFolder = await Io.CreateOrGetFolder(newContent[0], _appFolder);
                    folderOne = await Io.CreateOrGetFolder(newContent[1], directoryFolder);
                    return await Io.CreateOrGetFolder(newContent[2], folderOne);
                case 5:
                    directoryFolder = await Io.CreateOrGetFolder(newContent[0], _appFolder);
                    folderOne = await Io.CreateOrGetFolder(newContent[1], directoryFolder);
                    folderTwo = await Io.CreateOrGetFolder(newContent[2], folderOne);
                    return await Io.CreateOrGetFolder(newContent[3], folderTwo);
                default:
                    directoryFolder = await Io.CreateOrGetFolder(newContent[0], _appFolder);
                    folderOne = await Io.CreateOrGetFolder(newContent[1], directoryFolder);
                    folderTwo = await Io.CreateOrGetFolder(newContent[2], folderOne);
                    folderThree = await Io.CreateOrGetFolder(newContent[3], folderTwo);
                    return await Io.CreateOrGetFolder(newContent[4], folderThree);
            }
        }

        /// <summary>
        /// Find the book's title from the content.opf xml by parsing
        /// </summary>
        /// <param name="contentOpf">content.opf file location string</param>
        /// <param name="isInStorage"></param>
        /// <returns>title of the book as a string</returns>
        private async Task<string> FindTitle(string contentOpf, bool isInStorage)
        {
            var bookTitle = new List<string>();
            string newContentOpf = contentOpf;
            if (isInStorage)
            {
                StorageFolder newFolder;
                if (contentOpf.Contains('/'))
                {
                    string[] newContent = contentOpf.Split('/');
                    newFolder = await FindContentOpfFolder(newContent);
                    newContentOpf = newContent[newContent.Length -1];
                }
                else
                {
                    newFolder = _appFolder;
                }
                var file = await
                newFolder.GetFileAsync(newContentOpf);
                
                XmlDocument xdoc = await XmlDocument.LoadFromFileAsync(file);

                var content = xdoc.GetElementsByTagName("dc:title");

                bookTitle.AddRange(from s in content where s != null select s.InnerText);
            }
            else
            {
                XDocument xdoc1 = XDocument.Load(contentOpf);

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
        /// <param name="contentOpf">string of content.opf directory</param>
        /// <param name="chapterNames"></param>
        /// <param name="isInStorage"></param>
        /// <returns>List</returns>
        private async Task<List<ChapterModel>> ParseBookManifest(string contentOpf, List<string[]> chapterNames, bool isInStorage)
        {
            var manifestHrefs = new List<string>();
            var manifestIDs = new List<string>();
            var chapterCollection = new List<ChapterModel>();
            IEnumerable<string> idrefs;
            IEnumerable<string> manifestHref;
            IEnumerable<string> manifestId;
            XDocument xdoc;
            string newContentOpf = contentOpf;
            if (isInStorage)
            {
                StorageFolder newFolder;
                if (contentOpf.Contains('/'))
                {
                    string[] newContent = contentOpf.Split('/');
                    newFolder = await FindContentOpfFolder(newContent);
                    newContentOpf = newContent[newContent.Length - 1];
                }
                else
                {
                    newFolder = _appFolder;
                }
                using (var file = await
                    newFolder.OpenStreamForReadAsync(newContentOpf))
                {
                    xdoc = XDocument.Load(file);
                    //parse for idrefs in the manifest section
                     idrefs = from q in xdoc.Descendants()
                                 select (string)q.Attribute("idref");

                    //parse for chapter strings in maniftest
                    manifestHref = from x in xdoc.Descendants()
                                       select (string)x.Attribute("href");

                    //parse for the ids in spine for chapter order
                    manifestId = from x in xdoc.Descendants()
                                     select (string)x.Attribute("id");
                }
            }
            else
            {
                xdoc = XDocument.Load(contentOpf);
                //parse for idrefs in the manifest section
                idrefs = from q in xdoc.Descendants()
                             select (string)q.Attribute("idref");

                //parse for chapter strings in maniftest
                manifestHref = from x in xdoc.Descendants()
                                   select (string)x.Attribute("href");

                //parse for the ids in spine for chapter order
                manifestId = from x in xdoc.Descendants()
                                 select (string)x.Attribute("id");
            }

            foreach (string q in manifestHref)
            {
                if (q != null)
                {
                    manifestHrefs.Add(q);
                }
            }
            
            foreach (string p in manifestId)
            {
                if (p != null)
                {
                    manifestIDs.Add(p);
                }
            }

            //accepts a list of the chapters and their associated html files
            //var chapterNames = await GetChaptersFromNCXToC(directoryLoc + tocLoc, contentOPFLoc, isInStorage);

            // Will check id of chapters in the manifest against the ids in the spine
            // to find the correct order of the chapters and then adds them to chaptermodel
            var chapterId = 0;
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
                        else if (manifestHrefs[i - 1].Contains("toc"))
                        {
                            chapterName = "Table of Contents";
                        }
                        else
                        {
                            /*
                            if (checkChapterName.Contains('/'))
                            {

                                string[] manifestBreak = checkChapterName.Split('/');
                                checkChapterName = manifestBreak[manifestBreak.Length - 1];
                            }
                            */
                            foreach (string[] t in chapterNames)
                            {
                                if (checkChapterName == t[0])
                                {
                                    chapterName = t[1];
                                }
                            }
                        }
                        chapterCollection.Add(new ChapterModel
                        {
                            ChapterName = chapterName,
                            ChapterID = chapterId,
                            ChapterString = manifestHrefs[i - 1]
                        });
                    }
                    chapterId++;
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
            if (bookToRemove.IsoStore)
            {
                await Io.DeleteFolderInLocalFolder(bookToRemove.BookID);
            }
            Library.Remove(bookToRemove);
            await UpdateBooks();
            NotifyPropertyChanged("Items");
        }

        /// <summary>
        /// Method to update the library each time we import/delete a book
        /// todo: add a function that saves the page you are on
        /// </summary>
        /// <returns>Task</returns>
        public async Task UpdateBooks()
        {
            StorageFolder appStorageFolder = Io.GetAppStorageFolder();
            await Io.DeleteFileInFolder(appStorageFolder, "librarybooks.xml");
            await Io.DeleteFileInFolder(appStorageFolder, "recentreads.xml");
            await Io.DeleteFileInFolder(appStorageFolder, "fontfile.xml");
            string libraryBooksAsXml = Io.SerializeToString(Library);
            string recentReadsAsXml = Io.SerializeToString(RecentReads);
            string fontsAsXml = Io.SerializeToString(ReadingFonts);
            StorageFile dataFile = await Io.CreateFileInFolder(appStorageFolder, "librarybooks.xml");
            StorageFile recentReadsFile = await Io.CreateFileInFolder(appStorageFolder, "recentreads.xml");
            StorageFile fontFile = await Io.CreateFileInFolder(appStorageFolder, "fontfile.xml");
            await Io.WriteStringToFile(dataFile, libraryBooksAsXml);
            await Io.WriteStringToFile(recentReadsFile, recentReadsAsXml);
            await Io.WriteStringToFile(fontFile, fontsAsXml);
        }

        /// <summary>
        /// Method to load the data on app startup,
        /// checks for current file, creates a default if doesn't exist
        /// </summary>
        /// <returns>Task</returns>
        public async Task LoadData()
        {
            //uncomment below to clear the library
            //await IO.DeleteAllFilesInLocalFolder();
            StorageFolder appStorageFolder = ApplicationData.Current.LocalFolder;
            StorageFile dataFile = await Io.GetFileInFolder(appStorageFolder, "librarybooks.xml");
            StorageFile fontFile = await Io.GetFileInFolder(appStorageFolder, "fontfile.xml");
            StorageFile recentReadsFile = await Io.GetFileInFolder(appStorageFolder, "recentreads.xml");
            SetColors();

            if (dataFile != null)
            {
                if (!IsDataLoaded)
                {
                    string itemsAsXml = await Io.ReadStringFromFile(dataFile);
                    Library = Io.SerializeFromString<ObservableCollection<BookModel>>(itemsAsXml);
                    SortedBooks = new ListCollectionView(Library);
                    if (fontFile == null)
                    {
                        CreateNewFonts();
                    }
                    else
                    {
                        await SetFonts(fontFile);
                    }
                }
            }
            else
            {
            
                if (!IsDataLoaded)
                {
                    
                    Library = new ObservableCollection<BookModel>();
                    SortedBooks = new ListCollectionView(Library);
                    if (fontFile == null)
                    {
                        CreateNewFonts();
                    }
                    else
                    {
                        await SetFonts(fontFile);
                    }
                    bool test = await ImportBook("Pride and Prejudice - Jane Austen_6590", false);
                }
            }

            if (recentReadsFile != null)
            {

                if (!IsDataLoaded)
                {
                    var recentReadsAsXml = await Io.ReadStringFromFile(dataFile);
                    RecentReads = Io.SerializeFromString<ObservableCollection<BookModel>>(recentReadsAsXml);
                    RecentBooks = new ListCollectionView(RecentReads);
                }
            }
            else
            {

                if (!IsDataLoaded)
                {

                    RecentReads = new ObservableCollection<BookModel>();
                    RecentBooks = new ListCollectionView(RecentReads);
                    
                }
            }
            NotifyPropertyChanged("Items");
            IsDataLoaded = true;
        }

        /// <summary>
        /// reads the saved fonts and sets up the readingpage for it
        /// </summary>
        /// <param name="fontFile"></param>
        /// <returns></returns>
        private async Task SetFonts(StorageFile fontFile)
        {
            string fontsAsXml = await Io.ReadStringFromFile(fontFile);
            ReadingFonts = Io.SerializeFromString<ReadingFontsAndFamilies>(fontsAsXml);
            ReadingFontSize = ReadingFonts.ReadingFontSize;
            ReadingFontFamily = ReadingFonts.ReadingFontFamily;
            ReadingFontColorName = ReadingFonts.ReadingFontColorName;
            ReadingFontColor = _allColorBrushes[ReadingFontColorName];
            BackgroundReadingColorName = ReadingFonts.BackgroundReadingColorName;
            BackgroundReadingColor = _allColorBrushes[BackgroundReadingColorName];
        }

        /// <summary>
        /// Creates a new font class and sets up the defaults
        /// </summary>
        private void CreateNewFonts()
        {
            ReadingFonts = new ReadingFontsAndFamilies
            {
                ReadingFontSize = 20,
                ReadingFontFamily = "Segoe UI",
                ReadingFontColorName = "Black",
                BackgroundReadingColorName = "White"
            };
            ReadingFontSize = ReadingFonts.ReadingFontSize;
            ReadingFontFamily = ReadingFonts.ReadingFontFamily;
            ReadingFontColorName = ReadingFonts.ReadingFontColorName;
            ReadingFontColor = _allColorBrushes[ReadingFontColorName];
            BackgroundReadingColorName = ReadingFonts.BackgroundReadingColorName;
            BackgroundReadingColor = _allColorBrushes[BackgroundReadingColorName];
        }
    }
}
