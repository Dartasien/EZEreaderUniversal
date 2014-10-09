using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Data.Html;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using EZEreaderUniversal.Common;
using EZEreaderUniversal.DataModels;
using HtmlAgilityPack;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace EZEreaderUniversal
{
    /// <summary>
    ///     An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ReadingPage
    {
        private readonly StorageFolder _appFolder = ApplicationData.Current.LocalFolder;
        private readonly MainPage _rootPage = MainPage.Current;
        private readonly ObservableDictionary _defaultViewModel = new ObservableDictionary();
        private readonly NavigationHelper _navigationHelper;
        private Dictionary<String, SolidColorBrush> _allColorBrushes;
        private List<string> _chaptersNames;
        private Point _initialPoint;
        private List<RichTextBlockOverflow> _listRtbo = new List<RichTextBlockOverflow>();
        private Run _myRun;
        private BookModel _thisBook;
        private string _chapterText;
        private List<int> _chaptersNumbers;
        private List<TextBlock> _fontBlocks;
        private List<string> _fontSizes;
        private RichTextBlock _myRtb;
        private int _pageNumber;
        private Paragraph _para;

        public ReadingPage()
        {
            InitializeComponent();
            _navigationHelper = new NavigationHelper(this);
            _navigationHelper.LoadState += NavigationHelper_LoadState;
            _navigationHelper.SaveState += NavigationHelper_SaveState;
        }

        /// <summary>
        ///     Gets the <see cref="NavigationHelper" /> associated with this <see cref="Page" />.
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return _navigationHelper; }
        }

        /// <summary>
        ///     Gets the view model for this <see cref="Page" />.
        ///     This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return _defaultViewModel; }
        }

        #region NavigationHelper registration

        /// <summary>
        ///     The methods provided in this section are simply used to allow
        ///     NavigationHelper to respond to the page's navigation methods.
        ///     <para>
        ///         Page specific logic should be placed in event handlers for the
        ///         <see cref="NavigationHelper.LoadState" />
        ///         and <see cref="NavigationHelper.SaveState" />.
        ///         The navigation parameter is available in the LoadState method
        ///         in addition to page state preserved during an earlier session.
        ///     </para>
        /// </summary>
        /// <param name="e">
        ///     Provides data for navigation methods and event
        ///     handlers that cannot cancel the navigation request.
        /// </param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            _navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            _navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        #region Navigation Loads and Saves

        /// <summary>
        ///     Populates the page with content passed during navigation.  Any saved state is also
        ///     provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event; typically <see cref="NavigationHelper" />
        /// </param>
        /// <param name="e">
        ///     Event data that provides both the navigation parameter passed to
        ///     <see cref="Frame.Navigate(Type, Object)" /> when this page was initially requested and
        ///     a dictionary of state preserved by this page during an earlier
        ///     session.  The state will be null the first time a page is visited.
        /// </param>
        private async void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            string errorMessage = "";
            _thisBook = ((BookModel) e.NavigationParameter);
            SetTestTextBlocksText();
            PlaceChaptersInFlyout();
            SetFontSizes();
            FontSizeListBox.Loaded += FontSizeListBox_Loaded;
            GetSystemFonts();
            SetColorsFlyout();
            ReadingBottomBar.Visibility = Visibility.Collapsed;
            DataContext = _thisBook;
            try
            {
                await CreateFirstPage();
            }
            catch (Exception)
            {
                errorMessage = "Error opening book, possible incorrect format.";
            }
            if (errorMessage != "")
            {
                var messageDialog = new MessageDialog(errorMessage);
                messageDialog.Commands.Add(new UICommand("Okay"));
                messageDialog.DefaultCommandIndex = 0;
                messageDialog.CancelCommandIndex = 0;
                await messageDialog.ShowAsync();
                Frame.Navigate(typeof (MainPage));
            }
        }

        /// <summary>
        ///     Preserves state associated with this page in case the application is suspended or the
        ///     page is discarded from the navigation cache.  Values must conform to the serialization
        ///     requirements of <see cref="SuspensionManager.SessionState" />.
        /// </summary>
        /// <param name="sender">The source of the event; typically <see cref="NavigationHelper" /></param>
        /// <param name="e">
        ///     Event data that provides an empty dictionary to be populated with
        ///     serializable state.
        /// </param>
        private async void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
            if (_thisBook.IsCompleted)
            {
                _thisBook.CurrentChapter = 0;
                _thisBook.CurrentPage = 0;
                _rootPage.LibrarySource.RecentReads.Remove(_thisBook);
            }
            await _rootPage.LibrarySource.UpdateBooks();
        }

        #endregion

        #region Flyout Setups

        /// <summary>
        ///     Sets the FontSizeListBox itemssource to a list of font sizes as strings
        /// </summary>
        private void SetFontSizes()
        {
            _fontSizes = new List<string>();
            for (int i = 12; i < 37; i += 2)
            {
                _fontSizes.Add(i.ToString());
            }
            FontSizeListBox.ItemsSource = _fontSizes;
        }

        /// <summary>
        ///     Sets up the ColorsFlyout with the list of SolidColorBrushes available on WP 8.1
        ///     so that the user can make his own choice of text color and backgrounds.
        /// </summary>
        private void SetColorsFlyout()
        {
            var colors = typeof (Colors).GetRuntimeProperties().ToList();
            _allColorBrushes = new Dictionary<string, SolidColorBrush>();
            foreach (PropertyInfo color in colors)
            {
                var testColor = (Color) color.GetValue(null, null);
                string colorName = color.Name;
                var brush = new SolidColorBrush(testColor);
                _allColorBrushes.Add(colorName, brush);
            }
            var allColorNames = _allColorBrushes.Keys.ToList();
            BackgroundColorListBox.ItemsSource = allColorNames;
            FontColorListBox.ItemsSource = allColorNames;
        }

        /// <summary>
        ///     Sets the text of the chapterflyout and colorflyout to the same easily seen
        ///     text for easy sampling on the user's choices of fonts/colors.
        /// </summary>
        private void SetTestTextBlocksText()
        {
            const string testText = "The quick brown fox jumps over the lazy dog. The quick brown fox jumps over the lazy dog." +
                                    "The quick brown fox jumps over the lazy dog. The quick brown fox jumps over the lazy dog." +
                                    "The quick brown fox jumps over the lazy dog. The quick brown fox jumps over the lazy dog." +
                                    "The quick brown fox jumps over the lazy dog. The quick brown fox jumps over the lazy dog." +
                                    "The quick brown fox jumps over the lazy dog. The quick brown fox jumps over the lazy dog." +
                                    "The quick brown fox jumps over the lazy dog. The quick brown fox jumps over the lazy dog." +
                                    "The quick brown fox jumps over the lazy dog. The quick brown fox jumps over the lazy dog." +
                                    "The quick brown fox jumps over the lazy dog. The quick brown fox jumps over the lazy dog." +
                                    "The quick brown fox jumps over the lazy dog. The quick brown fox jumps over the lazy dog.";
            ColorTextBlock.Text = testText;
            FontCheckerBlock.Text = testText;
            ColorTextBlock.Foreground = _rootPage.LibrarySource.ReadingFontColor;
            ColorTextBlockGrid.Background = _rootPage.LibrarySource.BackgroundReadingColor;
        }

        /// <summary>
        ///     puts the chapters into a flyout for chapter selection by the user
        /// </summary>
        private void PlaceChaptersInFlyout()
        {
            _chaptersNames = new List<string>();
            _chaptersNumbers = new List<int>();
            foreach (ChapterModel chapter in _thisBook.Chapters)
            {
                if (chapter.ChapterName != "")
                {
                    _chaptersNames.Add(chapter.ChapterName);
                    _chaptersNumbers.Add(chapter.ChapterID);
                }
            }
            ChaptersListBox.ItemsSource = _chaptersNames;
            ChaptersListBox.Loaded += ChaptersListBox_Loaded;
            ChaptersListBox.SelectionChanged += ChaptersListBox_SelectionChanged;
        }

        /// <summary>
        ///     Adds the list of fontfamily names to the listblock for font selection
        /// </summary>
        private void GetSystemFonts()
        {
            _fontBlocks = new List<TextBlock>();
            string[] fonts =
            {
                "Arial", "Arial Black", "Arial Unicode MS", "Calibri", "Cambria",
                "Cambria Math", "Comic Sans MS", "Candara", "Consolas", "Constantia",
                "Corbel", "Courier New", "Georgia", "Lucida Sans Unicode", "Segoe UI",
                "Symbol", "Tahoma", "Times New Roman", "Trebuchet MS", "Verdana"
            };

            for (int i = 0; i < fonts.Length; i++)
            {
                _fontBlocks.Add(new TextBlock());
                _fontBlocks[i].Text = fonts[i];
                _fontBlocks[i].FontFamily = new FontFamily(fonts[i]);
            }
            FontFamilyListBox.ItemsSource = _fontBlocks;
        }

        #endregion

        #region Page creation and turning methods

        /// <summary>
        ///     Takes the chapters full html file and loads it, then converts to text, and finally
        ///     loads that into a RichTextBox and adds that to the grid
        /// </summary>
        private async Task CreateFirstPage()
        {
            var htmlDoc = new HtmlDocument();
            var image = new Image();
            var containers = new InlineUIContainer();

            if (_thisBook.CurrentChapter == 0)
            {
                //adds image to the first page of each book from assets or storage

                if ((_thisBook.CoverPic.Length > 9) &&
                    (_thisBook.CoverPic.ToLower().Substring(0, 9).Equals("isostore:")))
                {
                    string imageString = _thisBook.CoverPic.Substring(9);
                    await GetPicFromStorage(imageString, image, containers);
                }
                else
                {
                    GetPicFromAssets(image, containers);
                }
                _para = new Paragraph();
                _para.Inlines.Add(containers);
                SetmyRtb();
                _pageNumber = 0;
                _myRtb.Blocks.Add(_para);
                LayoutRoot.Children.Add(_myRtb);
                CreateAdditionalPages();
                ReturnToCurrentPage();
            }
            else
            {
                if (_thisBook.IsoStore)
                {
                    await GetChapterFromStorage(htmlDoc);
                }
                else
                {
                    string chapterPath = _thisBook.MainDirectory +
                                         _thisBook.Chapters[_thisBook.CurrentChapter].ChapterString;
                    var chapterUri = new Uri("ms-appx:///" + chapterPath, UriKind.Absolute);
                    StorageFile chapterFile = await StorageFile.GetFileFromApplicationUriAsync(chapterUri);
                    using (Stream chapterStream = await chapterFile.OpenStreamForReadAsync())
                    {
                        htmlDoc.Load(chapterStream);
                        _chapterText = HtmlUtilities.ConvertToText(htmlDoc.DocumentNode.InnerHtml);
                    }
                }

                _myRun = new Run();
                if (string.IsNullOrWhiteSpace(_chapterText))
                {
                    string newImage = await GetPicFromHtml();
                    await GetPicFromStorage(newImage, image, containers);
                    if (newImage != "")
                    {
                        _para = new Paragraph();
                        _para.Inlines.Add(containers);
                    }
                    else
                    {
                        _chapterText = " ";
                        _myRun.Text = _chapterText;
                        _para = new Paragraph();
                        _para.Inlines.Add(_myRun);
                    }
                }
                else
                {
                    _myRun.Text = _chapterText;
                    _para = new Paragraph();
                    _para.Inlines.Add(_myRun);
                }

                SetmyRtb();
                _pageNumber = 0;
                _myRtb.Blocks.Add(_para);
                LayoutRoot.Children.Add(_myRtb);
                CreateAdditionalPages();
                ReturnToCurrentPage();
            }
        }

        /// <summary>
        ///     Gets the picture from the titlepage
        /// </summary>
        /// loader
        /// <returns></returns>
        private async Task<string> GetPicFromHtml()
        {
            var imageString = "";
            var contentLoc = _thisBook.ContentDirectory;
            if (_thisBook.IsoStore)
            {
                string fullChapterString;
                if (_thisBook.ContentDirectory.Contains('/'))
                {
                    string[] st = contentLoc.Split('/');
                    contentLoc = "";
                    for (int i = 0; i < st.Length - 1; i++)
                    {
                        contentLoc += st[i];
                    }
                    fullChapterString = _thisBook.MainDirectory + contentLoc + "/" +
                                        _thisBook.Chapters[_thisBook.CurrentChapter].ChapterString;
                }
                else
                {
                    fullChapterString = _thisBook.MainDirectory +
                                        _thisBook.Chapters[_thisBook.CurrentChapter].ChapterString;
                }

                string[] fullChapterStrings = fullChapterString.Split('/');
                string chapterString = fullChapterStrings[fullChapterStrings.Length - 1];
                string[] chapterStringLoc = fullChapterString.Split('/');
                StorageFolder chapterFolder = await Io.CreateOrGetFolders(_appFolder, chapterStringLoc);

                using (Stream file = await chapterFolder.OpenStreamForReadAsync(chapterString))
                {
                    XDocument xdoc = XDocument.Load(file);
                    XNamespace ns = "http://www.w3.org/1999/xhtml";

                    IEnumerable<string> picLoc = from x in xdoc.Descendants()
                        select (string) x.Attribute("src");

                    foreach (string src in picLoc)
                    {
                        if (src != null)
                        {
                            imageString = src;
                        }
                    }

                    if (imageString == "")
                    {
                        IEnumerable<string> picLocHref = from x in xdoc.Descendants()
                            select (string) x.Attribute("href");

                        foreach (string href in picLocHref)
                        {
                            if (href != null)
                            {
                                imageString = href;
                            }
                        }
                    }
                    imageString = GetHtmlPicFromString(imageString, contentLoc);
                }
            }
            return imageString;
        }

        /// <summary>
        ///     Gets the actual location of a pic contained in a pic only html chapter
        /// </summary>
        /// <param name="imageString">image location from html file</param>
        /// <param name="contentLoc">location of most content</param>
        /// <returns></returns>
        private string GetHtmlPicFromString(string imageString, string contentLoc)
        {
            if (imageString != "")
            {
                if (imageString.Contains(".jpeg") || imageString.Contains(".jpg") ||
                    imageString.Contains(".png"))
                {
                    if (imageString.Contains("/"))
                    {
                        string[] imageStringSplit = imageString.Split('/');
                        if (_thisBook.ContentDirectory.Contains("/"))
                        {
                            imageString = _thisBook.MainDirectory + contentLoc + "/";
                        }
                        else
                        {
                            imageString = _thisBook.MainDirectory;
                        }
                        if (!imageStringSplit[0].Contains("."))
                        {
                            for (int i = 0; i < imageStringSplit.Length - 1; i++)
                            {
                                imageString += imageStringSplit[i] + "/";
                            }
                            imageString += imageStringSplit[imageStringSplit.Length - 1];
                        }
                        else
                        {
                            for (int i = 1; i < imageStringSplit.Length - 1; i++)
                            {
                                imageString += imageStringSplit[i] + "/";
                            }
                            imageString += imageStringSplit[imageStringSplit.Length - 1];
                        }
                    }
                    else
                    {
                        string fullImageString;
                        if (_thisBook.ContentDirectory.Contains("/"))
                        {
                            fullImageString = _thisBook.MainDirectory + contentLoc + "/" + imageString;
                        }
                        else
                        {
                            fullImageString = _thisBook.MainDirectory + imageString;
                        }
                        imageString = fullImageString;
                    }
                }
            }
            return imageString;
        }


        /// <summary>
        ///     getting picture from assets instead of storage and converting it into an image
        /// </summary>
        /// <param name="image"></param>
        /// the image to be sent to the container
        /// <param name="containers"></param>
        /// the inlineuicontainer to be added to the paragraph
        private void GetPicFromAssets(Image image, InlineUIContainer containers)
        {
            var testUri = new Uri("ms-appx:///" + _thisBook.CoverPic, UriKind.Absolute);
            var img = new BitmapImage(testUri);
            image.Source = img;
            containers.Child = image;
        }


        /// <summary>
        ///     Load the picture from storage and convert into an image to be displayed in rtb
        /// </summary>
        /// <param name="imageString"></param>
        /// <param name="image"></param>
        /// the image to be sent to the container
        /// <param name="containers"></param>
        /// the inlineuicontainer to be added to the paragraph
        /// <returns></returns>
        private async Task GetPicFromStorage(string imageString, Image image, InlineUIContainer containers)
        {
            StorageFile imageFile = null;
            if (_thisBook.IsoStore)
            {
                string[] folders = imageString.Split('/');
                try
                {
                    StorageFolder appBaseFolder = ApplicationData.Current.LocalFolder;
                    StorageFolder imageFolder = await Io.CreateOrGetFolders(appBaseFolder, folders);
                    imageFile = await imageFolder.GetFileAsync(folders[folders.Length - 1]);
                }
                catch (Exception)
                {
                }
            }
            else
            {
                try
                {
                    var chapterUri = new Uri("ms-appx:///" + imageString, UriKind.Absolute);
                    StorageFile chapterFile = await StorageFile.GetFileFromApplicationUriAsync(chapterUri);
                    using (IRandomAccessStreamWithContentType chapterStream = await chapterFile.OpenReadAsync())
                    {
                        var img = new BitmapImage();
                        await img.SetSourceAsync(chapterStream);
                        image.Source = img;
                        containers.Child = image;
                    }
                }
                catch (Exception)
                {
                }
            }
            if (imageFile != null)
            {
                using (IRandomAccessStreamWithContentType fileStream = await imageFile.OpenReadAsync())
                {
                    if (fileStream.CanRead)
                    {
                        var img = new BitmapImage();
                        await img.SetSourceAsync(fileStream);
                        image.Source = img;
                        containers.Child = image;
                    }
                }
            }
        }

        /// <summary>
        ///     Gets the HTML of the chapter from windows storage so it can be loaded
        /// </summary>
        /// <param name="htmlDoc"></param>
        /// loader
        /// <returns></returns>
        private async Task GetChapterFromStorage(HtmlDocument htmlDoc)
        {
            string fullChapterString;
            string contentLoc = _thisBook.ContentDirectory;
            if (_thisBook.ContentDirectory.Contains('/'))
            {
                string[] st = contentLoc.Split('/');
                contentLoc = "";
                for (int i = 0; i < st.Length - 1; i++)
                {
                    contentLoc += st[i];
                }
                fullChapterString = _thisBook.MainDirectory + contentLoc + "/" +
                                    _thisBook.Chapters[_thisBook.CurrentChapter].ChapterString;
            }
            else
            {
                fullChapterString = _thisBook.MainDirectory +
                                    _thisBook.Chapters[_thisBook.CurrentChapter].ChapterString;
            }

            string[] fullChapterStrings = fullChapterString.Split('/');
            string chapterString = fullChapterStrings[fullChapterStrings.Length - 1];
            string[] chapterStringLoc =
                fullChapterString.Split('/');

            StorageFolder chapterFolder = await Io.CreateOrGetFolders(_appFolder, chapterStringLoc);
            using (Stream file = await chapterFolder.OpenStreamForReadAsync(chapterString))
            {
                htmlDoc.Load(file);
                _chapterText = HtmlUtilities.ConvertToText(htmlDoc.DocumentNode.InnerHtml);
            }
        }

        /// <summary>
        ///     Set the first page using RichTextBlock so that we can use the overflow
        ///     for going forwards.
        /// </summary>
        private void SetmyRtb()
        {
            _myRtb = new RichTextBlock {IsTextSelectionEnabled = false};
            _myRtb.Tapped += myRTB_Tapped;
            _myRtb.TextAlignment = TextAlignment.Justify;
            _myRtb.Margin = new Thickness(5, 0, 5, 0);
            _myRtb.FontSize = _rootPage.LibrarySource.ReadingFontSize;
            _myRtb.FontFamily = new FontFamily(_rootPage.LibrarySource.ReadingFontFamily);
            _myRtb.Foreground = _rootPage.LibrarySource.ReadingFontColor;
            LayoutRoot.Background = _rootPage.LibrarySource.BackgroundReadingColor;
            _myRtb.ManipulationStarted += LayoutRoot_ManipulationStarted;
            _myRtb.ManipulationDelta += LayoutRoot_ManipulationDelta;
            _myRtb.ManipulationMode = ManipulationModes.All;
            Thickness margin = _myRtb.Margin;
            _myRtb.Visibility = Visibility.Visible;
            margin.Left = 10;
            margin.Right = 10;
            margin.Top = 10;
            margin.Bottom = 10;
        }

        /// <summary>
        ///     Adds any additional pages that are overflowing to the layout grid
        /// </summary>
        private void CreateAdditionalPages()
        {
            _listRtbo = new List<RichTextBlockOverflow>();
            LayoutRoot.UpdateLayout();
            if (_myRtb.HasOverflowContent)
            {
                _listRtbo.Add(new RichTextBlockOverflow());
                _myRtb.OverflowContentTarget = _listRtbo[_pageNumber];
                _listRtbo[_pageNumber].Visibility = Visibility.Visible;
                _listRtbo[_pageNumber].Margin = _myRtb.Margin;
                _listRtbo[_pageNumber].Tapped += myRTB_Tapped;
                _listRtbo[_pageNumber].ManipulationStarted += LayoutRoot_ManipulationStarted;
                _listRtbo[_pageNumber].ManipulationDelta += LayoutRoot_ManipulationDelta;
                _listRtbo[_pageNumber].ManipulationMode = ManipulationModes.All;
                _pageNumber++;
                LayoutRoot.Children.Add(_listRtbo[_pageNumber - 1]);
                _myRtb.Visibility = Visibility.Collapsed;
                LayoutRoot.UpdateLayout();

                //if theres any overflow, add it to a list of overflows
                while (_listRtbo[_pageNumber - 1].HasOverflowContent)
                {
                    _listRtbo.Add(new RichTextBlockOverflow());
                    _listRtbo[_pageNumber - 1].OverflowContentTarget = _listRtbo[_pageNumber];
                    _listRtbo[_pageNumber - 1].Visibility = Visibility.Collapsed;
                    _listRtbo[_pageNumber].Visibility = Visibility.Visible;
                    _listRtbo[_pageNumber].Margin = _myRtb.Margin;
                    _listRtbo[_pageNumber].Tapped += myRTB_Tapped;
                    _listRtbo[_pageNumber].ManipulationStarted += LayoutRoot_ManipulationStarted;
                    _listRtbo[_pageNumber].ManipulationDelta += LayoutRoot_ManipulationDelta;
                    _listRtbo[_pageNumber].ManipulationMode = ManipulationModes.All;
                    LayoutRoot.Children.Add(_listRtbo[_pageNumber]);
                    _pageNumber++;
                    LayoutRoot.UpdateLayout();
                }
            }
        }

        /// <summary>
        ///     Sets the first page of the chapter to visible
        /// </summary>
        private void ReturnToFirstPage()
        {
            LayoutRoot.Children.Last().Visibility = Visibility.Collapsed;
            LayoutRoot.Children.First().Visibility = Visibility.Visible;
        }

        /// <summary>
        ///     sets the page to the previously saved page if the book was opened before
        /// </summary>
        private void ReturnToCurrentPage()
        {
            if (_thisBook.CurrentPage == 0)
            {
                ReturnToFirstPage();
            }
            else
            {
                if (_thisBook.Chapters[_thisBook.CurrentChapter].PageCount > 0)
                {
                    if (LayoutRoot.Children.Count !=
                        _thisBook.Chapters[_thisBook.CurrentChapter].PageCount)
                    {
                        // ReSharper disable once PossibleLossOfFraction
                        double pagePercentage = (_thisBook.CurrentPage/
                                                 _thisBook.Chapters[_thisBook.CurrentChapter].PageCount);
                        _thisBook.CurrentPage = (int) (pagePercentage*
                                                       LayoutRoot.Children.Count);
                    }
                }
                LayoutRoot.Children.Last().Visibility = Visibility.Collapsed;
                LayoutRoot.Children.ElementAt(_thisBook.CurrentPage).Visibility =
                    Visibility.Visible;
            }
        }

        /// <summary>
        ///     Will turn the page forward if called, if needed
        /// </summary>
        /// <returns></returns>
        private async Task PageTurnForwards()
        {
            LayoutRoot.Children.ElementAt(_thisBook.CurrentPage).Visibility = Visibility.Collapsed;
            if (_thisBook.CurrentPage + 1 >= LayoutRoot.Children.Count)
            {
                if (_thisBook.CurrentChapter + 1 >= _thisBook.Chapters.Count)
                {
                    _thisBook.IsCompleted = true;
                    _thisBook.IsStarted = false;
                }
                else
                {
                    _thisBook.CurrentChapter++;
                    _thisBook.CurrentPage = 0;
                    LayoutRoot.Children.Clear();
                    await CreateFirstPage();
                }
            }
            else
            {
                LayoutRoot.Children.ElementAt(_thisBook.CurrentPage).Visibility = Visibility.Collapsed;
                _thisBook.CurrentPage++;
                LayoutRoot.Children.ElementAt(_thisBook.CurrentPage).Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        ///     Will turn the page forwards if called, if needed
        /// </summary>
        /// <returns></returns>
        private async Task PageTurnBack()
        {
            if (_thisBook.CurrentPage > 0)
            {
                LayoutRoot.Children.ElementAt(_thisBook.CurrentPage).Visibility =
                    Visibility.Collapsed;
                _thisBook.CurrentPage--;
                LayoutRoot.Children.ElementAt(_thisBook.CurrentPage).Visibility =
                    Visibility.Visible;
            }
            else
            {
                await CreateBackwardsPages();
                _thisBook.CurrentPage = LayoutRoot.Children.Count - 1;
                _thisBook.Chapters[_thisBook.CurrentChapter].PageCount
                    = LayoutRoot.Children.Count;
            }
        }

        /// <summary>
        ///     Creates the pages for going backwards to a previous chapter on tap
        /// </summary>
        private async Task CreateBackwardsPages()
        {
            var image = new Image();
            var containers = new InlineUIContainer();
            if (_thisBook.CurrentChapter > 1)
            {
                _thisBook.CurrentChapter--;
                LayoutRoot.Children.Clear();
                var htmlDoc = new HtmlDocument();
                if (_thisBook.IsoStore)
                {
                    await GetChapterFromStorage(htmlDoc);
                }
                else
                {
                    string chapterPath = _thisBook.MainDirectory +
                                         _thisBook.Chapters[_thisBook.CurrentChapter].ChapterString;
                    var chapterUri = new Uri("ms-appx:///" + chapterPath, UriKind.Absolute);
                    StorageFile chapterFile = await StorageFile.GetFileFromApplicationUriAsync(chapterUri);
                    using (Stream chapterStream = await chapterFile.OpenStreamForReadAsync())
                    {
                        htmlDoc.Load(chapterStream);
                        _chapterText = HtmlUtilities.ConvertToText(htmlDoc.DocumentNode.InnerHtml);
                    }
                }
                _myRun = new Run();
                _myRun = new Run();
                if (_chapterText == "")
                {
                    string newImage = await GetPicFromHtml();
                    await GetPicFromStorage(newImage, image, containers);
                    if (newImage != "")
                    {
                        _para = new Paragraph();
                        _para.Inlines.Add(containers);
                    }
                    else
                    {
                        _chapterText = " ";
                        _myRun.Text = _chapterText;
                        _para = new Paragraph();
                        _para.Inlines.Add(_myRun);
                    }
                }
                else
                {
                    _myRun.Text = _chapterText;
                    _para = new Paragraph();
                    _para.Inlines.Add(_myRun);
                }

                SetmyRtb();
                _myRtb.Blocks.Clear();
                _myRtb.Blocks.Add(_para);
                LayoutRoot.Children.Add(_myRtb);
                _pageNumber = 0;
                CreateAdditionalPages();
                _thisBook.CurrentPage = _pageNumber + 1;
            }
            else if (_thisBook.CurrentChapter == 1)
            {
                _thisBook.CurrentChapter--;
                LayoutRoot.Children.Clear();
                await CreateFirstPage();
                ReturnToFirstPage();
            }
        }

        #endregion

        #region Touch and Tap events

        /// <summary>
        ///     switches to a new page in the chapter upon tap and switches to
        ///     a new chapter if we hit the last page
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void myRTB_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Point eTap = e.GetPosition(LayoutRoot.Children.ElementAt(_thisBook.CurrentPage));

            if (ReadingBottomBar.Visibility == Visibility.Collapsed)
            {
                //tap on rightside of the screen makes page turn forwards
                if (eTap.X > LayoutRoot.ActualWidth*.6)
                {
                    await PageTurnForwards();
                }
                    //tap on left side of the screen makes the page turn backwards
                else if (eTap.X < LayoutRoot.ActualWidth*.4)
                {
                    await PageTurnBack();
                }
                else
                {
                    ReadingBottomBar.Visibility = Visibility.Visible;
                }
            }
            else
            {
                ReadingBottomBar.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        ///     sets the initial point for swipe detection
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LayoutRoot_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            _initialPoint = e.Position;
        }

        /// <summary>
        ///     handler to page turn depending upon the direction of swipes on the screen
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void LayoutRoot_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (e.IsInertial)
            {
                Point currentpoint = e.Position;
                if (currentpoint.X - _initialPoint.X >= 100)
                {
                    await PageTurnBack();
                    e.Complete();
                }
                else if (_initialPoint.X - currentpoint.X >= 100)
                {
                    await PageTurnForwards();
                    e.Complete();
                }
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        ///     Sets the FontSizeListBox to the already chosen font when opened
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FontSizeListBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (_fontSizes.Contains(_rootPage.LibrarySource.ReadingFontSize.ToString()))
            {
                FontSizeListBox.SelectionChanged -= FontSizeListBox_SelectionChanged;
                FontSizeListBox.SelectedItem = _rootPage.LibrarySource.ReadingFontSize.ToString();
                FontSizeListBox.SelectionChanged += FontSizeListBox_SelectionChanged;
                FontSizeListBox.ScrollIntoView(FontSizeListBox.SelectedItem);
            }
        }

        /// <summary>
        ///     Shows the different font sizes in the textblock below the list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FontSizeListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = sender as ListBox;
            if (listBox != null)
            {
                var newFont = (string) (listBox.SelectedItem);
                FontCheckerBlock.FontSize = Convert.ToInt32(newFont);
            }
        }

        /// <summary>
        ///     Sets the currently selected fontfamily as the selected item in FontFamilyListBox
        ///     so that the user knows which is current.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FontFamilyListBox_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (TextBlock item in FontFamilyListBox.Items)
            {
                if (item.Text == _rootPage.LibrarySource.ReadingFontFamily)
                {
                    FontFamilyListBox.SelectionChanged -= FontFamilyListBox_SelectionChanged;
                    var listBox = sender as ListBox;
                    if (listBox != null)
                    {
                        listBox.SelectedItem = item;
                    }
                    FontFamilyListBox.SelectionChanged += FontFamilyListBox_SelectionChanged;
                    FontFamilyListBox.ScrollIntoView(FontFamilyListBox.SelectedItem);
                }
            }
        }

        /// <summary>
        ///     Shows the different font families in the textblock below the list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FontFamilyListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = sender as ListBox;
            if (listBox != null && listBox.SelectedItem != null)
            {
                var newFontFamily = (TextBlock) ((sender as ListBox).SelectedItem);
                if (newFontFamily != null)
                {
                    FontCheckerBlock.FontFamily = new FontFamily(newFontFamily.Text);
                }
                FontCheckerBlock.UpdateLayout();
            }
        }

        /// <summary>
        ///     Takes the selected font size and font families and applies them to the reading page
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void FontFlyoutAcceptButton_Click(object sender, RoutedEventArgs e)
        {
            _rootPage.LibrarySource.ReadingFontSize = (int) FontCheckerBlock.FontSize;
            _rootPage.LibrarySource.ReadingFonts.ReadingFontSize = _rootPage.LibrarySource.ReadingFontSize;
            var newFontFamily = (TextBlock) FontFamilyListBox.SelectedItem;
            if (newFontFamily != null)
            {
                _rootPage.LibrarySource.ReadingFontFamily = newFontFamily.Text;
                _rootPage.LibrarySource.ReadingFonts.ReadingFontFamily = newFontFamily.Text;
            }
            BottomAppBar.Visibility = Visibility.Collapsed;
            LayoutRoot.Children.Clear();
            await CreateFirstPage();
            FontFlyout.Hide();
        }

        /// <summary>
        ///     closes font flyout when clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FontFlyoutCancelButton_Click(object sender, RoutedEventArgs e)
        {
            BottomAppBar.Visibility = Visibility.Collapsed;
            FontFlyout.Hide();
        }

        /// <summary>
        ///     Sets the font of the textblock used to check if a user would like to change their fonts
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FontCheckerBlock_Loaded(object sender, RoutedEventArgs e)
        {
            FontCheckerBlock.FontSize = _rootPage.LibrarySource.ReadingFontSize;
            FontCheckerBlock.FontFamily = new FontFamily(_rootPage.LibrarySource.ReadingFontFamily);
        }

        /// <summary>
        ///     Resets the fonts to program defaults for the user
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void FontsDefaultButton_Click(object sender, RoutedEventArgs e)
        {
            _rootPage.LibrarySource.ReadingFontSize = 20;
            _rootPage.LibrarySource.ReadingFonts.ReadingFontSize = 20;
            _rootPage.LibrarySource.ReadingFontFamily = "Segoe UI";
            _rootPage.LibrarySource.ReadingFonts.ReadingFontFamily = "Segoe UI";
            BottomAppBar.Visibility = Visibility.Collapsed;
            LayoutRoot.Children.Clear();
            await CreateFirstPage();
            FontFlyout.Hide();
        }

        /// <summary>
        ///     Closes the fontflyout if the appbarbutton is clicked again while its open
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FontButton_Click(object sender, RoutedEventArgs e)
        {
            if (FontCheckerBlock.Visibility == Visibility.Visible)
            {
                FontFlyoutGrid.UpdateLayout();
                FontFlyout.Hide();
            }
        }

        /// <summary>
        ///     Updates the chapterslistbox of chapternames to the currently opened chapter
        ///     whenever the chapterslistbox is opened.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChaptersListBox_Loaded(object sender, RoutedEventArgs e)
        {
            var listBox = sender as ListBox;
            if (listBox != null && _thisBook.Chapters[_thisBook.CurrentChapter].ChapterName != listBox.SelectedItem as string)
            {
                for (int i = 0; i < ChaptersListBox.Items.Count; i++)
                {
                    if ((string) ChaptersListBox.Items[i] ==
                        _thisBook.Chapters[_thisBook.CurrentChapter].ChapterName &&
                        _thisBook.Chapters[_thisBook.CurrentChapter].ChapterID == _chaptersNumbers[i])
                    {
                        ChaptersListBox.SelectionChanged -= ChaptersListBox_SelectionChanged;
                        (sender as ListBox).SelectedIndex = i;
                        ChaptersListBox.SelectionChanged += ChaptersListBox_SelectionChanged;
                    }
                }
            }
        }

        /// <summary>
        ///     changes chapters when selected
        /// </summary>
        /// <param name="sender">ListBox</param>
        /// <param name="e"></param>
        private async void ChaptersListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool chapterChanged = false;
            var listBox = sender as ListBox;
            if (listBox != null)
            {
                var newChapter = (string) listBox.SelectedItem;
                for (int i = 0; i < _thisBook.Chapters.Count; i++)
                {
                    if (newChapter == _thisBook.Chapters[i].ChapterName &&
                        _chaptersNumbers[ChaptersListBox.SelectedIndex] ==
                        _thisBook.Chapters[i].ChapterID)
                    {
                        _thisBook.CurrentChapter = i;
                        chapterChanged = true;
                    }
                }
            }
            ChaptersFlyout.Hide();
            if (chapterChanged)
            {
                _thisBook.CurrentPage = 0;
                LayoutRoot.Children.Clear();
                await CreateFirstPage();
            }
        }

        /// <summary>
        ///     Hides the ChapterFlyout without accepting any changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChaptersButton_Click(object sender, RoutedEventArgs e)
        {
            if (ChaptersListBox.Visibility == Visibility.Visible)
            {
                ChaptersFlyout.Hide();
            }
        }

        /// <summary>
        ///     Hides the ChaptersFlyout without accepting any changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChaptersFlyoutButton_Click(object sender, RoutedEventArgs e)
        {
            ChaptersFlyout.Hide();
            if ((string) BackgroundColorListBox.SelectedItem != _rootPage.LibrarySource.BackgroundReadingColorName)
            {
                BackgroundColorListBox.SelectionChanged -= BackgroundColorListBox_SelectionChanged;
                BackgroundColorListBox.SelectedItem = _rootPage.LibrarySource.BackgroundReadingColorName;
                BackgroundColorListBox.SelectionChanged += BackgroundColorListBox_SelectionChanged;
            }

            if ((string) FontColorListBox.SelectedItem != _rootPage.LibrarySource.ReadingFontColorName)
            {
                FontColorListBox.SelectionChanged -= FontColorListBox_SelectionChanged;
                FontColorListBox.SelectedItem = _rootPage.LibrarySource.ReadingFontColorName;
                FontColorListBox.SelectionChanged += FontColorListBox_SelectionChanged;
            }
        }

        /// <summary>
        ///     Sets the FontColorListBox selected item to the correct color when opened
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FontColorListBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (FontColorListBox.Items.Contains(_rootPage.LibrarySource.ReadingFontColorName))
            {
                FontColorListBox.SelectionChanged -= FontColorListBox_SelectionChanged;
                FontColorListBox.SelectedItem = _rootPage.LibrarySource.ReadingFontColorName;
                FontColorListBox.SelectionChanged += FontColorListBox_SelectionChanged;
                FontColorListBox.ScrollIntoView(FontColorListBox.Items.First());
                FontColorListBox.UpdateLayout();
            }
            FontColorListBox.ScrollIntoView(FontColorListBox.SelectedItem);
        }

        /// <summary>
        ///     Changes the ColorTextBlock text colors to give a demonstration to the user what their
        ///     color choice will look like.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FontColorListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = sender as ListBox;
            if (listBox != null && listBox.SelectedItem != null)
            {
                var newForegroundColor = (sender as ListBox).SelectedItem as string;
                if (_allColorBrushes.ContainsKey(newForegroundColor))
                {
                    ColorTextBlock.Foreground = _allColorBrushes[newForegroundColor];
                    ColorTextBlockGrid.UpdateLayout();
                }
            }
        }

        /// <summary>
        ///     Sets the BackgroundColorListBox selected item to the correct color when opened.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BackgroundColorListBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (BackgroundColorListBox.Items.Contains(_rootPage.LibrarySource.BackgroundReadingColorName))
            {
                BackgroundColorListBox.SelectionChanged -= BackgroundColorListBox_SelectionChanged;
                BackgroundColorListBox.SelectedItem = _rootPage.LibrarySource.BackgroundReadingColorName;
                BackgroundColorListBox.SelectionChanged += BackgroundColorListBox_SelectionChanged;
                BackgroundColorListBox.ScrollIntoView(BackgroundColorListBox.Items.First());
                BackgroundColorListBox.UpdateLayout();
                BackgroundColorListBox.ScrollIntoView(BackgroundColorListBox.SelectedItem);
            }
        }

        /// <summary>
        ///     Changes the ColorTextBlock background to give a demonstration to the user what their
        ///     color choice will look like.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BackgroundColorListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = sender as ListBox;
            if (listBox != null && listBox.SelectedItem != null)
            {
                var newBackgroundColor = (sender as ListBox).SelectedItem as string;
                if (_allColorBrushes.ContainsKey(newBackgroundColor))
                {
                    ColorTextBlockGrid.Background = _allColorBrushes[newBackgroundColor];
                    ColorTextBlockGrid.UpdateLayout();
                }
            }
        }

        /// <summary>
        ///     Hides the ColorFlyout and accepts the changes, updating the layoutroot and its objects
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void AcceptColorButton_Click(object sender, RoutedEventArgs e)
        {
            _rootPage.LibrarySource.ReadingFontColorName = (string) FontColorListBox.SelectedItem;
            _rootPage.LibrarySource.ReadingFonts.ReadingFontColorName = (string) FontColorListBox.SelectedItem;
            _rootPage.LibrarySource.ReadingFontColor = _allColorBrushes[(string) FontColorListBox.SelectedItem];
            _rootPage.LibrarySource.BackgroundReadingColorName = (string) BackgroundColorListBox.SelectedItem;
            _rootPage.LibrarySource.ReadingFonts.BackgroundReadingColorName =
                (string) BackgroundColorListBox.SelectedItem;
            _rootPage.LibrarySource.BackgroundReadingColor =
                _allColorBrushes[(string) BackgroundColorListBox.SelectedItem];
            BottomAppBar.Visibility = Visibility.Collapsed;
            LayoutRoot.Children.Clear();
            await CreateFirstPage();
            ColorsFlyout.Hide();
        }

        /// <summary>
        ///     Hides the ColorFlyout without accepting any changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CancelColorButton_Click(object sender, RoutedEventArgs e)
        {
            BottomAppBar.Visibility = Visibility.Collapsed;
            ColorsFlyout.Hide();
        }

        /// <summary>
        ///     Sets the default color scheme back to the original for the user
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ColorsDefaultButton_Click(object sender, RoutedEventArgs e)
        {
            _rootPage.LibrarySource.BackgroundReadingColorName = "White";
            _rootPage.LibrarySource.BackgroundReadingColor = new SolidColorBrush(Colors.White);
            _rootPage.LibrarySource.ReadingFontColorName = "Black";
            _rootPage.LibrarySource.ReadingFontColor = new SolidColorBrush(Colors.Black);
            BottomAppBar.Visibility = Visibility.Collapsed;
            LayoutRoot.Children.Clear();
            await CreateFirstPage();
            ColorsFlyout.Hide();
        }

        /// <summary>
        ///     Sets the ColorTextBlock foreground to the correct color when opened.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ColorTextBlock_Loaded(object sender, RoutedEventArgs e)
        {
            if (ColorTextBlock != null && ColorTextBlock.Foreground != _rootPage.LibrarySource.ReadingFontColor)
            {
                ColorTextBlock.Foreground = _rootPage.LibrarySource.ReadingFontColor;
            }
        }

        /// <summary>
        ///     Sets the ColorTextBlockGrid background to the correct color when opened.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ColorTextBlockGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (ColorTextBlockGrid != null && ColorTextBlockGrid.Background != _rootPage.LibrarySource.BackgroundReadingColor)
            {
                ColorTextBlockGrid.Background = _rootPage.LibrarySource.BackgroundReadingColor;
            }
        }

        /// <summary>
        ///     Hides the ColorsFlyout if the appbarbutton is clicked while it's already opened
        ///     and accepts no changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ColorsButton_Click(object sender, RoutedEventArgs e)
        {
            if (ColorTextBlockGrid.Visibility == Visibility.Visible)
            {
                ColorsFlyout.Hide();
            }
        }

        #endregion
    }
}