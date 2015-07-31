using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using EZEreaderUniversal.DataModels;
using Windows.ApplicationModel.Activation;
using Windows.UI.Popups;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace EZEreaderUniversal
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage
    {
        public static MainPage Current;
        public BooksModel LibrarySource;
        public CollectionViewSource LibraryViewSource;
        BookModel _ourBook;
        bool _isRecentReads;

        public FileActivatedEventArgs FileEvent { get; set; }

        public MainPage()
        {
            FileEvent = null;
            InitializeComponent();
            Application.Current.Suspending += Current_Suspending;
            NavigationCacheMode = NavigationCacheMode.Required;
        }

        async void Current_Suspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            if (!LibrarySource.IsDataLoaded)
            {
                await LibrarySource.LoadData();
            }

            await LibrarySource.UpdateBooks();
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        async protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            //set which items are visible
            
            BottomBar.Visibility = Visibility.Collapsed;
            RecentReadsListView.Visibility = Visibility.Collapsed;
            LibraryListView.Visibility = Visibility.Visible;
            //set which buttons are currently chosen
            RecentReadsBorder.BorderThickness = new Thickness(0);
            LibraryBorder.BorderThickness = new Thickness(3);
            _isRecentReads = false;
            //open data
            await RetrieveLibrary();
            LibrarySource.SortByBookNameAscending();
            LibraryListView.Focus(FocusState.Programmatic);
            //make no item currently selected
            LibraryListView.SelectedItem = null;
            //set a MainPage for other pages to access this
            Current = this;
        }
        
        /// <summary>
        /// Loads the library if it isn't already.
        /// </summary>
        /// <returns></returns>
        private async System.Threading.Tasks.Task RetrieveLibrary()
        {
            if (LibrarySource == null)
            {
                LibrarySource = new BooksModel();
                if (!LibrarySource.IsDataLoaded)
                {
                    await LibrarySource.LoadData();
                }
                DataContext = LibrarySource;
                LibraryListView.ItemsSource = LibrarySource.SortedBooks;
                RecentReadsListView.ItemsSource = LibrarySource.RecentBooks;
            }
        }

        #region Event Handlers

        /// <summary>
        /// Opens the reading page on the selected book if bottombar or its associated
        /// items are not also open, if they are, it closes them
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LibraryListView_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (BottomBar.Visibility == Visibility.Visible ||
                DetailsGrid.Visibility == Visibility.Visible ||
                BookNameBox.Visibility == Visibility.Visible ||
                AuthorNameBox.Visibility == Visibility.Visible)
            {
                CloseAppBarAndDetailsIfOpen();
            }
            else
            {
                var listViewItem = sender as ListViewItem;
                if (listViewItem != null)
                {
                    _ourBook = listViewItem.DataContext as BookModel;
                }
                LibraryListView.SelectedItem = null;
                if (_ourBook == null) return;
                if (_ourBook.IsStarted != true)
                {
                    _ourBook.IsStarted = true;
                    LibrarySource.RecentReads.Add(_ourBook);
                }
                _ourBook.OpenedRecentlyTime = DateTime.Now.Ticks;
                LibrarySource.SortBooksByAccessDate();
                Frame.Navigate(typeof(ReadingPage), _ourBook);
            }
        }

        /// <summary>
        /// Closes the appbar and the details grid if called
        /// </summary>
        private void CloseAppBarAndDetailsIfOpen()
        {
            BottomBar.Visibility = Visibility.Collapsed;
            DetailsGrid.Visibility = Visibility.Collapsed;
            BookNameBox.Visibility = Visibility.Collapsed;
            AuthorNameBox.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Navigates to the loading page if a new epub is opened by the phone
        /// to be added to the app library
        /// </summary>
        internal void NavigateToFilePage()
        {
            Frame.Navigate(typeof(LoadingPage));
        }

        /// <summary>
        /// Displays the bottom bar on right click or long tap on book
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LibraryListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            var listViewItem = sender as ListViewItem;
            if (listViewItem != null)
            {
                _ourBook = listViewItem.DataContext as BookModel;
            }
            if (BottomBar != null)
            {
                BottomBar.Visibility = Visibility.Visible;
            }
            CloseSearchGrid();
        }

        /// <summary>
        /// Displays the bottom bar on right click or long tap on book
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RecentReadsListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            var listViewItem = sender as ListViewItem;
            if (listViewItem != null)
            {
                _ourBook = listViewItem.DataContext as BookModel;
                _isRecentReads = true;
            }
            if (BottomBar != null)
            {
                BottomBar.Visibility = Visibility.Visible;
            }
            CloseSearchGrid();
        }

        /// <summary>
        /// deletes the currently selected book
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void DeleteBarButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isRecentReads)
            {
                var messageDialog = new MessageDialog("Are you sure you want to delete " + _ourBook.BookName + " by " +
                    _ourBook.AuthorID + " from your Library?");
                messageDialog.Commands.Add(new UICommand("Yes", CommandInvokedHandler));
                messageDialog.Commands.Add(new UICommand("Cancel", CommandInvokedHandler));
                messageDialog.DefaultCommandIndex = 1;
                messageDialog.CancelCommandIndex = 1;
                await messageDialog.ShowAsync();
            }
            else
            {
                var messageDialog = new MessageDialog("Woul you like to remove " + _ourBook.BookName +
                    " by " + _ourBook.AuthorID + " from your Recent list?");
                messageDialog.Commands.Add(new UICommand("Yes", CommandInvokedHandler));
                messageDialog.Commands.Add(new UICommand("Cancel", CommandInvokedHandler));
                messageDialog.DefaultCommandIndex = 1;
                messageDialog.CancelCommandIndex = 1;
                await messageDialog.ShowAsync();
            }
        }

        /// <summary>
        /// Deletes a book if the user confirms the choice and/or removes it from
        /// the recent reads listview if so chosen.
        /// </summary>
        /// <param name="command"></param>
        private async void CommandInvokedHandler(IUICommand command)
        {
            if (command.Label != "Yes") return;
            if (_ourBook == null) return;
            if (!LibrarySource.IsDataLoaded)
            {
                await RetrieveLibrary();
            }
            if (LibrarySource.RecentReads.Contains(_ourBook))
            {
                _ourBook.CurrentPage = 0;
                _ourBook.CurrentChapter = 0;
                _ourBook.IsStarted = false;
                _ourBook.IsCompleted = false;
                LibrarySource.RecentReads.Remove(_ourBook);
            }
            if (!_isRecentReads)
            {
                await LibrarySource.RemoveBook(_ourBook);
            }
        }

        /// <summary>
        /// Displays the currently selected books author and title for editting
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EditBarButton_Click(object sender, RoutedEventArgs e)
        {
            BottomBar.Visibility = Visibility.Collapsed;
            DetailsGrid.Visibility = Visibility.Visible;
            BookNameBox.Visibility = Visibility.Visible;
            AuthorNameBox.Visibility = Visibility.Visible;
            BookNameBox.Text = _ourBook.BookName;
            AuthorNameBox.Text = _ourBook.AuthorID;
            CloseSearchGrid();
        }

        /// <summary>
        /// closes teh bottombar if it sticks
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LibraryListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CloseAppBarAndDetailsIfOpen();
        }

        /// <summary>
        /// closes the bottomappbar if it sticks
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LibraryListView_Tapped_1(object sender, TappedRoutedEventArgs e)
        {
            if (BottomBar.Visibility == Visibility.Visible)
            {
                BottomBar.Visibility = Visibility.Collapsed;
            }
            LibraryListView.SelectedItem = null;
            DetailsGrid.Visibility = Visibility.Collapsed;
            BookNameBox.Visibility = Visibility.Collapsed;
            AuthorNameBox.Visibility = Visibility.Collapsed;
            CloseSearchGrid();
        }

        /// <summary>
        /// Allows the book's name to be editted by the user
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BookNameBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _ourBook.BookName = BookNameBox.Text;
        }

        /// <summary>
        /// Allows the author's name to be editted by the user
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AuthorNameBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _ourBook.AuthorID = AuthorNameBox.Text;
        }

        /// <summary>
        /// Method to allow for sorthing the library by author(first name) from either
        /// ascending or descending order.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SortByAuthor_Click(object sender, RoutedEventArgs e)
        {
            if (LibrarySource.SortedBooks.SortDescriptions.Count > 0)
            {
                if (LibrarySource.SortedBooks.SortDescriptions[0].PropertyName != "AuthorID")
                {
                    LibrarySource.SortedBooks.SortDescriptions[0].PropertyName = "AuthorID";
                    LibrarySource.SortedBooks.SortDescriptions[0].Direction = ListSortDirection.Ascending;
                }
                else if (LibrarySource.SortedBooks.SortDescriptions[0].Direction != ListSortDirection.Ascending)
                {
                    LibrarySource.SortedBooks.SortDescriptions[0].Direction = ListSortDirection.Ascending;
                }
                else
                {
                    LibrarySource.SortedBooks.SortDescriptions[0].Direction = ListSortDirection.Descending;
                }
            }
            else
            {
                LibrarySource.SortedBooks.SortDescriptions.Add(new SortDescription("AuthorID", ListSortDirection.Ascending));
            }

            if (LibraryListView.Visibility == Visibility.Collapsed)
            {
                OpenLibraryListView();
            }
            CloseSearchGrid();
            LibrarySource.SortedBooks.Refresh();
        }


        /// <summary>
        /// Method to allow for sorting the library by Title from either
        /// ascending or descending order.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SortByTitle_Click(object sender, RoutedEventArgs e)
        {
            if (LibrarySource.SortedBooks.SortDescriptions.Count > 0)
            {
                if (LibrarySource.SortedBooks.SortDescriptions[0].PropertyName != "BookName")
                {
                    LibrarySource.SortedBooks.SortDescriptions[0].PropertyName = "BookName";
                    LibrarySource.SortedBooks.SortDescriptions[0].Direction = ListSortDirection.Ascending;
                }
                else if (LibrarySource.SortedBooks.SortDescriptions[0].Direction != ListSortDirection.Ascending)
                {
                    LibrarySource.SortedBooks.SortDescriptions[0].Direction = ListSortDirection.Ascending;
                }
                else
                {
                    LibrarySource.SortedBooks.SortDescriptions[0].Direction = ListSortDirection.Descending;
                }
            }
            else
            {
                LibrarySource.SortedBooks.SortDescriptions.Add(new SortDescription("BookName", ListSortDirection.Descending));
            }

            if (LibraryListView.Visibility == Visibility.Collapsed)
            {
                OpenLibraryListView();
            }
            CloseSearchGrid();
            LibrarySource.SortedBooks.Refresh();
        }

        /// <summary>
        /// Opens the Library ListView for users to select a new book to read.
        /// </summary>
        private void OpenLibraryListView()
        {
            RecentReadsBorder.BorderThickness = new Thickness(0);
            LibraryBorder.BorderThickness = new Thickness(3);
            RecentReadsListView.Visibility = Visibility.Collapsed;
            LibraryListView.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Opens the Recent Reads ListView for user to select a recently opened book
        /// to read.
        /// </summary>
        private void OpenRecentReadsListView()
        {
            LibraryListView.Visibility = Visibility.Collapsed;
            RecentReadsListView.Visibility = Visibility.Visible;
            RecentReadsBorder.BorderThickness = new Thickness(3);
            LibraryBorder.BorderThickness = new Thickness(0);
        }

        /// <summary>
        /// Goes to the book that is selected from the LibraryView ListViewCollection
        /// and opens it up in the reading page.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LibraryView_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (LibraryListView.Visibility != Visibility.Visible)
            {
                OpenLibraryListView();
            }
            CloseSearchGrid();
        }

        /// <summary>
        /// Goes to the new book that is selected from the RecentBooks ListViewCollection
        /// and opens it up in the reading page.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RecentReadsView_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (RecentReadsListView.Visibility != Visibility.Visible)
            {
                OpenRecentReadsListView();
            }
            CloseSearchGrid();
            
        }

        /// <summary>
        /// Closes the Search Textbox and clears it, removing the filter on the list
        /// </summary>
        private void CloseSearchGrid()
        {
            if (SearchGrid.Visibility != Visibility.Visible) return;
            SearchGrid.Visibility = Visibility.Collapsed;
            LibraryTextBlockGrid.Visibility = Visibility.Visible;
            RecentReadsTextBlockGrid.Visibility = Visibility.Visible;
            LibrarySource.SortedBooks.Filter = null;
            SearchBookTextBox.Text = "";
        }
        #endregion

        /// <summary>
        /// Opens the grid with textbox for searching the lists
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SearchLibrary_Click(object sender, RoutedEventArgs e)
        {
            OpenLibraryListView();
            SearchGrid.Visibility = Visibility.Visible;
            SearchBookTextBox.Visibility = Visibility.Visible;
            LibraryTextBlockGrid.Visibility = Visibility.Collapsed;
            RecentReadsTextBlockGrid.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Filters the ListCollectionView by author or title as text is entered
        /// into the box
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SearchBookTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null)
            {
                var filter = textBox.Text;
                LibrarySource.SortedBooks.Filter = x => ((BookModel)x).AuthorID.ToLower().Contains(filter.ToLower()) 
                                                        || ((BookModel)x).BookName.ToLower().Contains(filter.ToLower());
            }
            LibrarySource.SortedBooks.Refresh();
        }

        /// <summary>
        /// Calls the method to close the search grid if the enter key is pressed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SearchBookTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key.Equals(Windows.System.VirtualKey.Enter))
            {
                CloseSearchGrid();
            }
        }
    }
}
