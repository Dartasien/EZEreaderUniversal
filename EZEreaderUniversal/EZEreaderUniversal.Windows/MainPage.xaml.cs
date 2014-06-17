using EZEreaderUniversal.DataModels;
using EZEreaderUniversal.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
    public sealed partial class MainPage : Page
    {
        public BooksModel LibrarySource;
        public static MainPage Current;
        public CollectionViewSource LibraryViewSource;
        BookModel ourBook;
        bool IsRecentReads;

        public MainPage()
        {
            this.InitializeComponent();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private FileActivatedEventArgs _fileEventArgs = null;
        public FileActivatedEventArgs FileEvent
        {
            get { return _fileEventArgs; }
            set { _fileEventArgs = value; }
        }

        internal void NavigateToFilePage()
        {
            this.Frame.Navigate(typeof(LoadingPage));
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
 	        base.OnNavigatedTo(e);
            await RetrieveLibrary();
        }

        private async System.Threading.Tasks.Task RetrieveLibrary()
        {
            if (LibrarySource == null)
            {
                LibrarySource = new BooksModel();
                if (!LibrarySource.IsDataLoaded)
                {
                    await LibrarySource.LoadData();
                }
                this.DataContext = LibrarySource;
                RecentReadsListView.ItemsSource = LibrarySource.SortedBooks;
                LibraryGridView.ItemsSource = LibrarySource.SortedBooks;
            }
        }

        private void LibraryGridView_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var gridViewItem = sender as GridViewItem;
            if (gridViewItem != null)
            {
                ourBook = gridViewItem.DataContext as BookModel;
            }
            this.LibraryGridView.SelectedItem = null;
            if (ourBook != null)
            {
                if (ourBook.IsStarted != true)
                {
                    ourBook.IsStarted = true;
                    this.LibrarySource.RecentReads.Add(ourBook);
                }
                ourBook.OpenedRecentlyTime = DateTime.Now.Ticks;
                LibrarySource.SortBooksByAccessDate();
                this.Frame.Navigate(typeof(ReadingPage), ourBook);
            }
        }
    }
}
