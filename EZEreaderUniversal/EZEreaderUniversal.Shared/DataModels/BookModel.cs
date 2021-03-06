﻿using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace EZEreaderUniversal.DataModels
{
    public class BookModel : INotifyPropertyChanged
    {
        //event handler to notify WP that something has changed
        //and needs to be refreshed on the screen
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string p)
        {
            var handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(p));
            }

        }       
        
        //Declare an id for each book
        private string _bookid;

        public string BookID
        {
            get
            {
                return _bookid;
            }
            set
            {
                if (value != _bookid)
                {
                    _bookid = value;
                    NotifyPropertyChanged("BookID");
                }
            }
        }

        //Declare a name for each book
        private string _bookname;

        public string BookName
        {
            get
            {
                return _bookname;
            }
            set
            {
                if (value != _bookname)
                {
                    _bookname = value;
                    NotifyPropertyChanged("BookName");
                }
            }
        }

        //Declare an author for each book
        private string _authorid;

        public string AuthorID
        {
            get
            {
                return _authorid;
            }
            set
            {
                if (value != _authorid)
                {
                    _authorid = value;
                    NotifyPropertyChanged("AuthorID");
                }
            }
        }

        //Declare a date upon which each book is added to library
        private string _addedDate;

        public string AddedDate
        {
            get
            {
                return _addedDate;
            }
            set
            {
                if (value != _addedDate)
                {
                    _addedDate = value;
                    NotifyPropertyChanged("AddedDate");
                }
            }
        }

        //a time for when the book was last accessed
        // ReSharper disable once InconsistentNaming
        private long _openedrecentlytime;

        public long OpenedRecentlyTime
        {
            get
            {
                return _openedrecentlytime;
            }
            set
            {
                if (value != _openedrecentlytime)
                {
                    _openedrecentlytime = value;
                    NotifyPropertyChanged("OpenedRecentlyTime");
                }
            }
        }
        //Declare the directory of the folder where the content is stored
        private string _maindirectory;

        public String MainDirectory
        {
            get
            {
                return _maindirectory;
            }
            set
            {
                if (value != _maindirectory)
                {
                    _maindirectory = value;
                    NotifyPropertyChanged("ContentDirectory");
                }
            }
        }

        //Declare the jpeg that will work as the cover pic's location
        private string _coverpic;

        public String CoverPic
        {
            get
            {
                 return _coverpic;
            }
            set
            {
                if (value != _coverpic)
                {
                    _coverpic = value;
                    NotifyPropertyChanged("CoverPic");
                }
            }
        }

        //Declare a string to hold the location of the table of contents
        private string _tableofcontents;

        public string TableOfContents
        {
            get
            {
                return _tableofcontents;
            }
            set
            {
                if (value != _tableofcontents)
                {
                    _tableofcontents = value;
                    NotifyPropertyChanged("TableOfContents");
                }
            }
        }

        //Declare an array of the pages so that we know where to go when loading
        private List<ChapterModel> _chapters;

        public List<ChapterModel> Chapters
        {
            get
            {
                return _chapters;
            }
            set
            {
                if (value != _chapters)
                {
                    _chapters = value;
                    NotifyPropertyChanged("Chapters");
                }
            }
        }

        //declare an int to count the total pages in a book
        private int _totalPages;

        public int TotalPages
        {
            get
            {
                return _totalPages;
            }
            set
            {
                if (value != _totalPages)
                {
                    _totalPages = value;
                    NotifyPropertyChanged("TotalPages");
                }
            }
        }

        //declare an int to keep track of the current chapter
        private int _currentchapter;

        public int CurrentChapter
        {
            get
            {
                return _currentchapter;
            }
            set
            {
                if (value != _currentchapter)
                {
                    _currentchapter = value;
                    NotifyPropertyChanged("CurrentChapter");
                }
            }
        }

        //decide whether a book is in isostorage or not
        private bool _isoStore;

        public bool IsoStore
        {
            get { return _isoStore; }
            set
            {
                if (value != _isoStore)
                {
                    _isoStore = value;
                    NotifyPropertyChanged("IsoStore");
                }
            }
        }

        //declare an int to keep track of the current page
        private int _currentpage;

        public int CurrentPage
        {
            get
            {
                return _currentpage;
            }
            set
            {
                if (value != _currentpage)
                {
                    _currentpage = value;
                    NotifyPropertyChanged("CurrentPage");
                }
            }
        }

        //location of most of the content in string format
        private string _contentDirectory;
        public string ContentDirectory 
        {
            get 
            { 
                return _contentDirectory;
            }
            set
            {
                if (value != _contentDirectory)
                {
                    _contentDirectory = value;
                    NotifyPropertyChanged("ContentDirectory");
                }
            }
        }

        //shows if the book has been opened
        private bool _isStarted;

        public bool IsStarted
        {
            get
            {
                return _isStarted;
            }
            set
            {
                if (value != _isStarted)
                {
                    _isStarted = value;
                    NotifyPropertyChanged("IsStarted");
                }
            }
        }

        //shows if a book has been completely read
        private bool _isCompleted;

        public bool IsCompleted
        {
            get
            {
                return _isCompleted;
            }
            set
            {
                if (value != _isCompleted)
                {
                    _isCompleted = value;
                    NotifyPropertyChanged("IsCompleted");
                }
            }
        }
    }
}
