using EZEreaderUniversal.Common;
using EZEreaderUniversal.DataModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Data.Html;
using Windows.UI.Xaml.Documents;
using Windows.Storage;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Popups;
using System.Reflection;
using HtmlAgilityPack;
using Windows.UI;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace EZEreaderUniversal
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ReadingPage : Page
    {
        StorageFolder appFolder = ApplicationData.Current.LocalFolder;
        MainPage rootPage = MainPage.Current;
        BookModel thisBook;
        List<RichTextBlockOverflow> listRTBO = new List<RichTextBlockOverflow>();
        private Point InitialPoint;
        Run myRun;
        List<string> chaptersNames;
        List<int> chaptersNumbers;
        int ChaptersListBoxSelectedIndex;
        string chapterText;
        int pageNumber;
        Paragraph para;
        RichTextBlock myRTB;
        List<TextBlock> fontBlocks;
        List<string> fontSizes;
        Dictionary<String, SolidColorBrush> AllColorBrushes;
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        public ReadingPage()
        {
            this.InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;
        }      

        /// <summary>
        /// Gets the <see cref="NavigationHelper"/> associated with this <see cref="Page"/>.
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        /// <summary>
        /// Gets the view model for this <see cref="Page"/>.
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        #region NavigationHelper registration

        /// <summary>
        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// <para>
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="NavigationHelper.LoadState"/>
        /// and <see cref="NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.
        /// </para>
        /// </summary>
        /// <param name="e">Provides data for navigation methods and event
        /// handlers that cannot cancel the navigation request.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        #region Navigation Loads and Saves

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session.  The state will be null the first time a page is visited.</param>
        private async void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {   
            string errorMessage = "";
            thisBook = ((BookModel)e.NavigationParameter);
            SetTestTextBlocksText();
            PlaceChaptersInFlyout();
            SetFontSizes();
            FontSizeListBox.Loaded += FontSizeListBox_Loaded;
            GetSystemFonts();
            SetColorsFlyout();
            ReadingBottomBar.Visibility = Visibility.Collapsed;
            this.DataContext = thisBook;
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
                this.Frame.Navigate(typeof(MainPage));
            }
        }
        
        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">The source of the event; typically <see cref="NavigationHelper"/></param>
        /// <param name="e">Event data that provides an empty dictionary to be populated with
        /// serializable state.</param>
        private async void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
            if (thisBook.IsCompleted == true)
            {
                thisBook.CurrentChapter = 0;
                thisBook.CurrentPage = 0;
                rootPage.LibrarySource.RecentReads.Remove(thisBook);
            }
            await rootPage.LibrarySource.UpdateBooks();
        }

        #endregion

        #region Flyout Setups

        /// <summary>
        /// Sets the FontSizeListBox itemssource to a list of font sizes as strings
        /// </summary>
        private void SetFontSizes()
        {
            fontSizes = new List<string>();
            for (int i = 12; i < 37; i += 2)
            {
                fontSizes.Add(i.ToString());
            }
            FontSizeListBox.ItemsSource = fontSizes;
        }

        /// <summary>
        /// Sets up the ColorsFlyout with the list of SolidColorBrushes available on WP 8.1
        /// so that the user can make his own choice of text color and backgrounds.
        /// </summary>
        private void SetColorsFlyout()
        {
            SolidColorBrush brush = new SolidColorBrush(Colors.White);
            var colors = typeof(Colors).GetRuntimeProperties().ToList();
            AllColorBrushes = new Dictionary<string, SolidColorBrush>();
            foreach (PropertyInfo color in colors)
            {
                Color testColor = (Color)color.GetValue(null, null);
                string colorName = color.Name;
                brush = new SolidColorBrush(testColor);
                AllColorBrushes.Add(colorName, brush);
            }
            List<string> allColorNames = new List<string>();
            foreach (string key in AllColorBrushes.Keys)
            {
                allColorNames.Add(key);
            }
            BackgroundColorListBox.ItemsSource = allColorNames;
            FontColorListBox.ItemsSource = allColorNames;
        }

        /// <summary>
        /// Sets the text of the chapterflyout and colorflyout to the same easily seen
        /// text for easy sampling on the user's choices of fonts/colors.
        /// </summary>
        private void SetTestTextBlocksText()
        {
            string testText = "The quick brown fox jumps over the lazy dog. The quick brown fox jumps over the lazy dog." +
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
            ColorTextBlock.Foreground = rootPage.LibrarySource.ReadingFontColor;
            ColorTextBlockGrid.Background = rootPage.LibrarySource.BackgroundReadingColor;
        }

        /// <summary>
        /// puts the chapters into a flyout for chapter selection by the user
        /// </summary>
        private void PlaceChaptersInFlyout()
        {
            chaptersNames = new List<string>();
            chaptersNumbers = new List<int>();
            foreach (var chapter in thisBook.Chapters)
            {
                if (chapter.ChapterName != "")
                {
                    chaptersNames.Add(chapter.ChapterName);
                    chaptersNumbers.Add(chapter.ChapterID);
                }
            }
            ChaptersListBox.ItemsSource = chaptersNames;
            ChaptersListBox.Loaded += ChaptersListBox_Loaded;
            ChaptersListBox.SelectionChanged += ChaptersListBox_SelectionChanged;
        }

        /// <summary>
        /// Adds the list of fontfamily names to the listblock for font selection
        /// </summary>
        private void GetSystemFonts()
        {
            fontBlocks = new List<TextBlock>();
            string[] fonts = {"Arial", "Arial Black", "Arial Unicode MS", "Calibri", "Cambria",
                                 "Cambria Math", "Comic Sans MS", "Candara", "Consolas", "Constantia",
                                 "Corbel", "Courier New", "Georgia", "Lucida Sans Unicode", "Segoe UI",
                                 "Symbol", "Tahoma", "Times New Roman", "Trebuchet MS", "Verdana"};

            for (int i = 0; i < fonts.Length; i++)
            {
                fontBlocks.Add(new TextBlock());
                fontBlocks[i].Text = fonts[i];
                fontBlocks[i].FontFamily = new FontFamily(fonts[i]);
            }
            FontFamilyListBox.ItemsSource = fontBlocks;
        }

        #endregion

        #region Page creation and turning methods

        /// <summary>
        /// Takes the chapters full html file and loads it, then converts to text, and finally
        /// loads that into a RichTextBox and adds that to the grid
        /// </summary>
        private async Task CreateFirstPage()
        {
            HtmlDocument htmlDoc = new HtmlDocument();
            Image image = new Image();
            InlineUIContainer containers = new InlineUIContainer();
            
            if (thisBook.CurrentChapter == 0)
            {
                //adds image to the first page of each book from assets or storage
                
                if ((thisBook.CoverPic.Length > 9) && (thisBook.CoverPic.ToLower().Substring(0, 9).Equals("isostore:")))
                {
                    await GetPicFromStorage(image, containers);
                }
                else
                {
                    GetPicFromAssets(image, containers);
                }
                para = new Paragraph();
                para.Inlines.Add(containers);
                SetmyRTB();
                pageNumber = 0;
                myRTB.Blocks.Add(para);
                LayoutRoot.Children.Add(myRTB);
                CreateAdditionalPages();
                ReturnToCurrentPage();
            }
            else
            {
                if (thisBook.IsoStore)
                {
                    await GetChapterFromStorage(htmlDoc);
                }
                else
                {
                    htmlDoc.Load(thisBook.MainDirectory +
                        thisBook.Chapters[thisBook.CurrentChapter].ChapterString);
                    chapterText = HtmlUtilities.ConvertToText(htmlDoc.DocumentNode.InnerHtml);
                }
                
                myRun = new Run();
                if (chapterText == "")
                {
                    chapterText = " ";
                }
                myRun.Text = chapterText;
                para = new Paragraph();
                para.Inlines.Add(myRun);
                SetmyRTB();
                pageNumber = 0;
                myRTB.Blocks.Add(para);
                LayoutRoot.Children.Add(myRTB);
                CreateAdditionalPages();
                ReturnToCurrentPage();
            }
            
        }

        /* partial implementation working on html parsing for images
        /// <summary>
        /// Gets the picture from the titlepage
        /// </summary>
        /// <param name="htmlDoc"></param>loader
        /// <returns></returns>
        private async Task GetPicFromHTML(HtmlDocument htmlDoc, Image image, InlineUIContainer containers)
        {
            StorageFolder chapterFolder;
            string fullChapterString;
            string imageString = "";
            string[] st;
            string contentLoc = thisBook.ContentDirectory;
            if (thisBook.ContentDirectory.Contains('/'))
            {
                st = contentLoc.Split('/');
                contentLoc = "";
                for (int i = 0; i < st.Length - 1; i++)
                {
                    contentLoc += st[i];
                }
                fullChapterString = thisBook.MainDirectory + contentLoc + "/" +
                    thisBook.Chapters[thisBook.CurrentChapter].ChapterString;
            }
            else
            {
                fullChapterString = thisBook.MainDirectory +
                thisBook.Chapters[thisBook.CurrentChapter].ChapterString;
            }

            string[] fullChapterStrings = fullChapterString.Split('/');
            string chapterString = fullChapterStrings[fullChapterStrings.Length - 1];
            string[] chapterStringLoc =
                fullChapterString.Split('/');

            chapterFolder =
                await IO.CreateOrGetFolders(appFolder, chapterStringLoc);
            using (var file = await chapterFolder.OpenStreamForReadAsync(chapterString))
            {
                htmlDoc.Load(file);
                foreach (HtmlNode img in htmlDoc.DocumentNode.Descendants())
                {
                    imageString = img.Attributes["src"].Value;
                }

                if (imageString == "")
                {
                    foreach (HtmlNode img in htmlDoc.DocumentNode.Descendants())
                    {
                        imageString = img.Attributes["href"].Value;
                    }
                }
                //chapterText = HtmlUtilities.ConvertToText(htmlDoc.DocumentNode.InnerHtml);
            }
        }
        */

        /// <summary>
        /// getting picture from assets instead of storage and converting it into an image
        /// </summary>
        /// <param name="image"></param>the image to be sent to the container
        /// <param name="containers"></param>the inlineuicontainer to be added to the paragraph
        private void GetPicFromAssets(Image image, InlineUIContainer containers)
        {
            Uri testUri = new Uri("ms-appx:///" + thisBook.CoverPic, UriKind.Absolute);
            BitmapImage img = new BitmapImage(testUri);
            image.Source = img;
            containers.Child = image;
        }

        /// <summary>
        /// Load the picture from storage and convert into an image to be displayed in rtb
        /// </summary>
        /// <param name="image"></param>the image to be sent to the container
        /// <param name="containers"></param>the inlineuicontainer to be added to the paragraph
        /// <returns></returns>
        private async Task GetPicFromStorage(Image image, InlineUIContainer containers)
        {
            string[] folders = thisBook.CoverPic.Substring(9).Split('/');
            StorageFile imageFile = null;
            try
            {
                StorageFolder appBaseFolder = ApplicationData.Current.LocalFolder;
                StorageFolder imageFolder = await IO.CreateOrGetFolders(appBaseFolder, folders);
                imageFile = await imageFolder.GetFileAsync(folders[folders.Length - 1]);
            }
            catch(Exception)
            { }
            if (imageFile != null)
            {
                using (var fileStream = await imageFile.OpenReadAsync())
                {
                    if (fileStream.CanRead)
                    {
                        BitmapImage img = new BitmapImage();
                        await img.SetSourceAsync(fileStream);
                        image.Source = img;
                        containers.Child = image;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the HTML of the chapter from windows storage so it can be loaded
        /// </summary>
        /// <param name="htmlDoc"></param>loader
        /// <returns></returns>
        private async Task GetChapterFromStorage(HtmlDocument htmlDoc)
        {
            StorageFolder chapterFolder;
            string fullChapterString;
            string[] st;
            string contentLoc = thisBook.ContentDirectory;
            if (thisBook.ContentDirectory.Contains('/'))
            {
                st = contentLoc.Split('/');
                contentLoc = "";
                for (int i = 0; i < st.Length - 1; i++)
                {
                    contentLoc += st[i];
                }
                fullChapterString = thisBook.MainDirectory + contentLoc + "/" +
                    thisBook.Chapters[thisBook.CurrentChapter].ChapterString;
            }
            else
            {
                fullChapterString = thisBook.MainDirectory +
                thisBook.Chapters[thisBook.CurrentChapter].ChapterString;
            }
            
            string[] fullChapterStrings = fullChapterString.Split('/');
            string chapterString = fullChapterStrings[fullChapterStrings.Length - 1];
            string[] chapterStringLoc =
                fullChapterString.Split('/');

            chapterFolder =
                await IO.CreateOrGetFolders(appFolder, chapterStringLoc);
            using (var file = await chapterFolder.OpenStreamForReadAsync(chapterString))
            {
                htmlDoc.Load(file);
                chapterText = HtmlUtilities.ConvertToText(htmlDoc.DocumentNode.InnerHtml);
            }
        }

        /// <summary>
        /// Set the first page using RichTextBlock so that we can use the overflow
        /// for going forwards.
        /// </summary>
        private void SetmyRTB()
        {
            myRTB = new RichTextBlock();
            myRTB.IsTextSelectionEnabled = false;
            myRTB.Tapped += myRTB_Tapped;
            myRTB.TextAlignment = TextAlignment.Justify;
            myRTB.Margin = new Thickness(5, 0, 5, 0);
            myRTB.FontSize = rootPage.LibrarySource.ReadingFontSize;
            myRTB.FontFamily = new FontFamily(rootPage.LibrarySource.ReadingFontFamily);
            myRTB.Foreground = rootPage.LibrarySource.ReadingFontColor;
            LayoutRoot.Background = rootPage.LibrarySource.BackgroundReadingColor;
            myRTB.ManipulationStarted += LayoutRoot_ManipulationStarted;
            myRTB.ManipulationDelta += LayoutRoot_ManipulationDelta;
            myRTB.ManipulationMode = ManipulationModes.All;
            Thickness margin = myRTB.Margin;
            myRTB.Visibility = Visibility.Visible;
            margin.Left = 10;
            margin.Right = 10;
            margin.Top = 10;
            margin.Bottom = 10;

        }

        /// <summary>
        /// Adds any additional pages that are overflowing to the layout grid
        /// </summary>
        private void CreateAdditionalPages()
        {
            listRTBO = new List<RichTextBlockOverflow>();
            LayoutRoot.UpdateLayout();
            if (myRTB.HasOverflowContent)
            {
                listRTBO.Add(new RichTextBlockOverflow());
                myRTB.OverflowContentTarget = listRTBO[pageNumber];
                listRTBO[pageNumber].Visibility = Visibility.Visible;
                listRTBO[pageNumber].Margin = myRTB.Margin;
                listRTBO[pageNumber].Tapped += myRTB_Tapped;
                listRTBO[pageNumber].ManipulationStarted += LayoutRoot_ManipulationStarted;
                listRTBO[pageNumber].ManipulationDelta += LayoutRoot_ManipulationDelta;
                listRTBO[pageNumber].ManipulationMode = ManipulationModes.All;
                pageNumber++;
                LayoutRoot.Children.Add(listRTBO[pageNumber - 1]);
                myRTB.Visibility = Visibility.Collapsed;
                LayoutRoot.UpdateLayout();

                //if theres any overflow, add it to a list of overflows
                while (listRTBO[pageNumber - 1].HasOverflowContent)
                {
                    listRTBO.Add(new RichTextBlockOverflow());
                    listRTBO[pageNumber - 1].OverflowContentTarget = listRTBO[pageNumber];
                    listRTBO[pageNumber - 1].Visibility = Visibility.Collapsed;
                    listRTBO[pageNumber].Visibility = Visibility.Visible;
                    listRTBO[pageNumber].Margin = myRTB.Margin;
                    listRTBO[pageNumber].Tapped += myRTB_Tapped;
                    listRTBO[pageNumber].ManipulationStarted += LayoutRoot_ManipulationStarted;
                    listRTBO[pageNumber].ManipulationDelta += LayoutRoot_ManipulationDelta;
                    listRTBO[pageNumber].ManipulationMode = ManipulationModes.All;
                    LayoutRoot.Children.Add(listRTBO[pageNumber]);
                    pageNumber++;
                    LayoutRoot.UpdateLayout();
                }
            }
        }

        /// <summary>
        /// Sets the first page of the chapter to visible
        /// </summary>
        private void ReturnToFirstPage()
        {
            LayoutRoot.Children.Last().Visibility = Visibility.Collapsed;
            LayoutRoot.Children.First().Visibility = Visibility.Visible;
        }

        /// <summary>
        /// sets the page to the previously saved page if the book was opened before
        /// </summary>
        private void ReturnToCurrentPage()
        {
            if (thisBook.CurrentPage == 0)
            {
                ReturnToFirstPage();
            }
            else
            {
                if (thisBook.Chapters[thisBook.CurrentChapter].PageCount > 0)
                {
                    if (LayoutRoot.Children.Count !=
                        thisBook.Chapters[thisBook.CurrentChapter].PageCount)
                    {
                        double pagePercentage = (thisBook.CurrentPage /
                            thisBook.Chapters[thisBook.CurrentChapter].PageCount);
                        thisBook.CurrentPage = (int)(pagePercentage *
                            LayoutRoot.Children.Count);
                    }
                }
                LayoutRoot.Children.Last().Visibility = Visibility.Collapsed;
                LayoutRoot.Children.ElementAt(thisBook.CurrentPage).Visibility = 
                    Visibility.Visible;
            }
        }

        /// <summary>
        /// Will turn the page forward if called, if needed
        /// </summary>
        /// <returns></returns>
        private async Task PageTurnForwards()
        {
            LayoutRoot.Children.ElementAt(thisBook.CurrentPage).Visibility = Visibility.Collapsed;
            if (thisBook.CurrentPage + 1 >= LayoutRoot.Children.Count)
            {
                if (thisBook.CurrentChapter + 1 >= thisBook.Chapters.Count)
                {
                    thisBook.IsCompleted = true;
                    thisBook.IsStarted = false;
                }
                else
                {
                    thisBook.CurrentChapter++;
                    thisBook.CurrentPage = 0;
                    LayoutRoot.Children.Clear();
                    await CreateFirstPage();
                }

            }
            else
            {
                LayoutRoot.Children.ElementAt(thisBook.CurrentPage).Visibility = Visibility.Collapsed;
                thisBook.CurrentPage++;
                LayoutRoot.Children.ElementAt(thisBook.CurrentPage).Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Will turn the page forwards if called, if needed
        /// </summary>
        /// <returns></returns>
        private async Task PageTurnBack()
        {
            if (thisBook.CurrentPage > 0)
            {
                LayoutRoot.Children.ElementAt(thisBook.CurrentPage).Visibility =
                    Visibility.Collapsed;
                thisBook.CurrentPage--;
                LayoutRoot.Children.ElementAt(thisBook.CurrentPage).Visibility =
                    Visibility.Visible;
            }
            else
            {
                await CreateBackwardsPages();
                thisBook.CurrentPage = LayoutRoot.Children.Count - 1;
                thisBook.Chapters[thisBook.CurrentChapter].PageCount
                    = LayoutRoot.Children.Count;
            }
        }

        /// <summary>
        /// Creates the pages for going backwards to a previous chapter on tap
        /// </summary>
        private async Task CreateBackwardsPages()
        {
            if (thisBook.CurrentChapter > 1)
            {
                thisBook.CurrentChapter--;
                LayoutRoot.Children.Clear();
                HtmlDocument htmlDoc = new HtmlDocument();
                if (thisBook.IsoStore)
                {
                    await GetChapterFromStorage(htmlDoc);
                }
                else
                {
                    htmlDoc.Load(thisBook.MainDirectory +
                        thisBook.Chapters[thisBook.CurrentChapter].ChapterString);
                    chapterText = HtmlUtilities.ConvertToText(htmlDoc.DocumentNode.InnerHtml);
                }
                myRun = new Run();
                if (chapterText == "")
                {
                    chapterText = " ";
                }
                myRun.Text = chapterText;
                para = new Paragraph();
                para.Inlines.Add(myRun);
                SetmyRTB();
                myRTB.Blocks.Clear();
                myRTB.Blocks.Add(para);
                LayoutRoot.Children.Add(myRTB);
                pageNumber = 0;
                CreateAdditionalPages();
                thisBook.CurrentPage = pageNumber +1;
            }
            else if (thisBook.CurrentChapter == 1)
            {
                thisBook.CurrentChapter--;
                LayoutRoot.Children.Clear();
                await CreateFirstPage();
                ReturnToFirstPage();
            }
        }

        #endregion

        #region Touch and Tap events

        /// <summary>
        /// switches to a new page in the chapter upon tap and switches to
        /// a new chapter if we hit the last page
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void myRTB_Tapped(object sender, TappedRoutedEventArgs e)
        {

            Point eTap = e.GetPosition(LayoutRoot.Children.ElementAt(thisBook.CurrentPage));

            if (this.ReadingBottomBar.Visibility == Visibility.Collapsed)
            {
                //tap on rightside of the screen makes page turn forwards
                if (eTap.X > LayoutRoot.ActualWidth * .6)
                {
                    await PageTurnForwards();
                }
                //tap on left side of the screen makes the page turn backwards
                else if (eTap.X < LayoutRoot.ActualWidth * .4)
                {
                    await PageTurnBack();
                }
                else
                {
                    this.ReadingBottomBar.Visibility = Visibility.Visible;
                }
            }
            else
            {
                this.ReadingBottomBar.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// sets the initial point for swipe detection
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LayoutRoot_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            InitialPoint = e.Position;
        }

        /// <summary>
        /// handler to page turn depending upon the direction of swipes on the screen
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void LayoutRoot_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (e.IsInertial)
            {
                Point currentpoint = e.Position;
                if (currentpoint.X - InitialPoint.X >= 100)
                {
                    await PageTurnBack();
                    e.Complete();
                }
                else if (InitialPoint.X - currentpoint.X >= 100)
                {
                    await PageTurnForwards();
                    e.Complete();
                }
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Sets the FontSizeListBox to the already chosen font when opened
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FontSizeListBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (fontSizes.Contains(rootPage.LibrarySource.ReadingFontSize.ToString()))
            {
                FontSizeListBox.SelectionChanged -= FontSizeListBox_SelectionChanged;
                FontSizeListBox.SelectedItem = rootPage.LibrarySource.ReadingFontSize.ToString();
                FontSizeListBox.SelectionChanged += FontSizeListBox_SelectionChanged;
                FontSizeListBox.ScrollIntoView(FontSizeListBox.SelectedItem);
            }
        }

        /// <summary>
        /// Shows the different font sizes in the textblock below the list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FontSizeListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string newFont = (string)((sender as ListBox).SelectedItem);
            FontCheckerBlock.FontSize = Convert.ToInt32(newFont);
        }

        /// <summary>
        /// Sets the currently selected fontfamily as the selected item in FontFamilyListBox
        /// so that the user knows which is current.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void FontFamilyListBox_Loaded(object sender, RoutedEventArgs e)
        {
            TextBlock fontTextBlock = new TextBlock();
            fontTextBlock.Text = rootPage.LibrarySource.ReadingFontFamily;
            fontTextBlock.FontFamily = new FontFamily(rootPage.LibrarySource.ReadingFontFamily);

            foreach (TextBlock item in FontFamilyListBox.Items)
            {
                if (item.Text == rootPage.LibrarySource.ReadingFontFamily)
                {
                    FontFamilyListBox.SelectionChanged -= FontFamilyListBox_SelectionChanged;
                    (sender as ListBox).SelectedItem = item;
                    FontFamilyListBox.SelectionChanged += FontFamilyListBox_SelectionChanged;
                    FontFamilyListBox.ScrollIntoView(FontFamilyListBox.SelectedItem);
                }
            }
        }

        /// <summary>
        /// Shows the different font families in the textblock below the list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FontFamilyListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((sender as ListBox).SelectedItem != null)
            {
                TextBlock newFontFamily = (TextBlock)((sender as ListBox).SelectedItem);
                FontCheckerBlock.FontFamily = new FontFamily(newFontFamily.Text);
                FontCheckerBlock.UpdateLayout();
            }
        }

        /// <summary>
        /// Takes the selected font size and font families and applies them to the reading page
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void FontFlyoutAcceptButton_Click(object sender, RoutedEventArgs e)
        {
            rootPage.LibrarySource.ReadingFontSize = (int)FontCheckerBlock.FontSize;
            rootPage.LibrarySource.ReadingFonts.ReadingFontSize = rootPage.LibrarySource.ReadingFontSize;
            TextBlock newFontFamily = (TextBlock)FontFamilyListBox.SelectedItem as TextBlock;
            rootPage.LibrarySource.ReadingFontFamily = newFontFamily.Text;
            rootPage.LibrarySource.ReadingFonts.ReadingFontFamily = newFontFamily.Text;
            BottomAppBar.Visibility = Visibility.Collapsed;
            LayoutRoot.Children.Clear();
            await CreateFirstPage();
            FontFlyout.Hide();
        }

        /// <summary>
        /// closes font flyout when clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FontFlyoutCancelButton_Click(object sender, RoutedEventArgs e)
        {
            BottomAppBar.Visibility = Visibility.Collapsed;
            FontFlyout.Hide();
        }

        private void FontCheckerBlock_Loaded(object sender, RoutedEventArgs e)
        {
            FontCheckerBlock.FontSize = rootPage.LibrarySource.ReadingFontSize;
            FontCheckerBlock.FontFamily = new FontFamily(rootPage.LibrarySource.ReadingFontFamily);
        }

        /// <summary>
        /// Resets the fonts to program defaults for the user
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void FontsDefaultButton_Click(object sender, RoutedEventArgs e)
        {
            rootPage.LibrarySource.ReadingFontSize = 20;
            rootPage.LibrarySource.ReadingFonts.ReadingFontSize = 20;
            rootPage.LibrarySource.ReadingFontFamily = "Segoe UI";
            rootPage.LibrarySource.ReadingFonts.ReadingFontFamily = "Segoe UI";
            BottomAppBar.Visibility = Visibility.Collapsed;
            LayoutRoot.Children.Clear();
            await CreateFirstPage();
            FontFlyout.Hide();
        }

        /// <summary>
        /// Updates the chapterslistbox of chapternames to the currently opened chapter
        /// whenever the chapterslistbox is opened.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChaptersListBox_Loaded(object sender, RoutedEventArgs e)
        {

            if (thisBook.Chapters[thisBook.CurrentChapter].ChapterName != (sender as ListBox).SelectedItem as string)
            {
                for (int i = 0; i < ChaptersListBox.Items.Count; i++)
                {
                    if ((string)ChaptersListBox.Items[i] == 
                        thisBook.Chapters[thisBook.CurrentChapter].ChapterName &&
                        thisBook.Chapters[thisBook.CurrentChapter].ChapterID == chaptersNumbers[i])
                    {
                        ChaptersListBox.SelectionChanged -= ChaptersListBox_SelectionChanged;
                        (sender as ListBox).SelectedIndex = i;
                        ChaptersListBoxSelectedIndex = i;
                        ChaptersListBox.SelectionChanged += ChaptersListBox_SelectionChanged;
                    }
                }
            }
        }

        /// <summary>
        /// changes chapters when selected
        /// </summary>
        /// <param name="sender">ListBox</param>
        /// <param name="e"></param>
        private async void ChaptersListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool chapterChanged = false;
            string newChapter = (string)(sender as ListBox).SelectedItem as string;
            ChaptersListBoxSelectedIndex = (int)(sender as ListBox).SelectedIndex;
            for (int i = 0; i < thisBook.Chapters.Count; i++)
            {
                if (newChapter == thisBook.Chapters[i].ChapterName &&
                    chaptersNumbers[ChaptersListBox.SelectedIndex] ==
                    thisBook.Chapters[i].ChapterID)
                {
                    thisBook.CurrentChapter = i;
                    chapterChanged = true;
                }
            }
            ChaptersFlyout.Hide();
            if (chapterChanged == true)
            {
                thisBook.CurrentPage = 0;
                LayoutRoot.Children.Clear();
                await CreateFirstPage();
            }
        }

        /// <summary>
        /// Hides the ChapterFlyout without accepting any changes.
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
        /// Hides the ChaptersFlyout without accepting any changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChaptersFlyoutButton_Click(object sender, RoutedEventArgs e)
        {
            ChaptersFlyout.Hide();
            if ((string)BackgroundColorListBox.SelectedItem != rootPage.LibrarySource.BackgroundReadingColorName)
            {
                BackgroundColorListBox.SelectionChanged -= BackgroundColorListBox_SelectionChanged;
                BackgroundColorListBox.SelectedItem = rootPage.LibrarySource.BackgroundReadingColorName;
                BackgroundColorListBox.SelectionChanged += BackgroundColorListBox_SelectionChanged;
            }

            if ((string)FontColorListBox.SelectedItem != rootPage.LibrarySource.ReadingFontColorName)
            {
                FontColorListBox.SelectionChanged -= FontColorListBox_SelectionChanged;
                FontColorListBox.SelectedItem = rootPage.LibrarySource.ReadingFontColorName;
                FontColorListBox.SelectionChanged += FontColorListBox_SelectionChanged;
            }
        }

        /// <summary>
        /// Sets the FontColorListBox selected item to the correct color when opened
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FontColorListBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (FontColorListBox.Items.Contains(rootPage.LibrarySource.ReadingFontColorName))
            {
                FontColorListBox.SelectionChanged -= FontColorListBox_SelectionChanged;
                FontColorListBox.SelectedItem = rootPage.LibrarySource.ReadingFontColorName;
                FontColorListBox.SelectionChanged += FontColorListBox_SelectionChanged;
                FontColorListBox.ScrollIntoView(FontColorListBox.Items.First());
                FontColorListBox.UpdateLayout();
            }
            FontColorListBox.ScrollIntoView(FontColorListBox.SelectedItem);

        }

        /// <summary>
        /// Changes the ColorTextBlock text colors to give a demonstration to the user what their
        /// color choice will look like.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FontColorListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((sender as ListBox).SelectedItem != null)
            {
                string newForegroundColor = (sender as ListBox).SelectedItem as string;
                if (AllColorBrushes.ContainsKey(newForegroundColor))
                {
                    ColorTextBlock.Foreground = AllColorBrushes[newForegroundColor];
                    ColorTextBlockGrid.UpdateLayout();
                }
            }
        }

        /// <summary>
        /// Sets the BackgroundColorListBox selected item to the correct color when opened.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BackgroundColorListBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (BackgroundColorListBox.Items.Contains(rootPage.LibrarySource.BackgroundReadingColorName))
            {
                BackgroundColorListBox.SelectionChanged -= BackgroundColorListBox_SelectionChanged;
                BackgroundColorListBox.SelectedItem = rootPage.LibrarySource.BackgroundReadingColorName;
                BackgroundColorListBox.SelectionChanged += BackgroundColorListBox_SelectionChanged;
                BackgroundColorListBox.ScrollIntoView(BackgroundColorListBox.Items.First());
                BackgroundColorListBox.UpdateLayout();
                BackgroundColorListBox.ScrollIntoView(BackgroundColorListBox.SelectedItem);
            }
        }

        /// <summary>
        /// Changes the ColorTextBlock background to give a demonstration to the user what their
        /// color choice will look like.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BackgroundColorListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((sender as ListBox).SelectedItem != null)
            {
                string newBackgroundColor = (sender as ListBox).SelectedItem as string;
                if (AllColorBrushes.ContainsKey(newBackgroundColor))
                {
                    ColorTextBlockGrid.Background = AllColorBrushes[newBackgroundColor];
                    ColorTextBlockGrid.UpdateLayout();
                }
            }
        }

        /// <summary>
        /// Hides the ColorFlyout and accepts the changes, updating the layoutroot and its objects
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void AcceptColorButton_Click(object sender, RoutedEventArgs e)
        {
            rootPage.LibrarySource.ReadingFontColorName = (string)FontColorListBox.SelectedItem;
            rootPage.LibrarySource.ReadingFonts.ReadingFontColorName = (string)FontColorListBox.SelectedItem;
            rootPage.LibrarySource.ReadingFontColor = AllColorBrushes[(string)FontColorListBox.SelectedItem];
            rootPage.LibrarySource.BackgroundReadingColorName = (string)BackgroundColorListBox.SelectedItem;
            rootPage.LibrarySource.ReadingFonts.BackgroundReadingColorName = (string)BackgroundColorListBox.SelectedItem;
            rootPage.LibrarySource.BackgroundReadingColor = AllColorBrushes[(string)BackgroundColorListBox.SelectedItem];
            BottomAppBar.Visibility = Visibility.Collapsed;
            LayoutRoot.Children.Clear();
            await CreateFirstPage();
            ColorsFlyout.Hide();
        }

        /// <summary>
        /// Hides the ColorFlyout without accepting any changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CancelColorButton_Click(object sender, RoutedEventArgs e)
        {
            BottomAppBar.Visibility = Visibility.Collapsed;
            ColorsFlyout.Hide();
        }

        /// <summary>
        /// Sets the default color scheme back to the original for the user
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ColorsDefaultButton_Click(object sender, RoutedEventArgs e)
        {
            rootPage.LibrarySource.BackgroundReadingColorName = "White";
            rootPage.LibrarySource.BackgroundReadingColor = new SolidColorBrush(Colors.White);
            rootPage.LibrarySource.ReadingFontColorName = "Black";
            rootPage.LibrarySource.ReadingFontColor = new SolidColorBrush(Colors.Black);
            BottomAppBar.Visibility = Visibility.Collapsed;
            LayoutRoot.Children.Clear();
            await CreateFirstPage();
            ColorsFlyout.Hide();
        }

        /// <summary>
        /// Sets the ColorTextBlock foreground to the correct color when opened.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ColorTextBlock_Loaded(object sender, RoutedEventArgs e)
        {
            if (ColorTextBlock.Foreground != rootPage.LibrarySource.ReadingFontColor)
            {
                ColorTextBlock.Foreground = rootPage.LibrarySource.ReadingFontColor;
            }
        }

        /// <summary>
        /// Sets the ColorTextBlockGrid background to the correct color when opened.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ColorTextBlockGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (ColorTextBlockGrid.Background != rootPage.LibrarySource.BackgroundReadingColor)
            {
                ColorTextBlockGrid.Background = rootPage.LibrarySource.BackgroundReadingColor;
            }
        }

        /// <summary>
        /// Hides the ColorsFlyout if the appbarbutton is clicked while it's already opened
        /// and accepts no changes
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

        private void FontButton_Click(object sender, RoutedEventArgs e)
        {
            if (FontCheckerBlock.Visibility == Visibility.Visible)
            {
                FontFlyoutGrid.UpdateLayout();
                FontFlyout.Hide();
            }
        }
    }
}