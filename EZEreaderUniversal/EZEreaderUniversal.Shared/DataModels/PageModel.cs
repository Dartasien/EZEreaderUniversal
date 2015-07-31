using System.ComponentModel;


namespace EZEreaderUniversal.DataModels
{
    public class PageModel : INotifyPropertyChanged
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

        //declare a string to identify the page/chapter in the book
        private int _pageid;

        public int PageID
        {
            get
            {
                return _pageid;
            }
            set
            {
                if (value != _pageid)
                {
                    _pageid = value;
                    NotifyPropertyChanged("PageID");
                }
            }
        }
        // declare a string to hold the location of the page/chapter that goes to the id
        private string _pagestring;

        public string PageString
        {
            get
            {
                return _pagestring;
            }
            set
            {
                if (value != _pagestring)
                {
                    _pagestring = value;
                    NotifyPropertyChanged("PageString");
                }
            }
        }
    }
}
