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
using HtmlAgilityPack;
using Windows.UI.Xaml.Documents;
using System.Diagnostics;
using Windows.Storage;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Popups;

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
        Run myRun;
        string chapterText;
        int pageNumber;
        Paragraph para;
        RichTextBlock myRTB;
        List<TextBlock> fontBlocks;
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
            ReadingBottomBar.Visibility = Visibility.Collapsed;
            //FontListBox.Visibility = Visibility.Collapsed;
            this.DataContext = thisBook;
            GetSystemFonts();
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
        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
            if (thisBook.IsCompleted == true)
            {
                thisBook.CurrentChapter = 0;
                thisBook.CurrentPage = 0;
                rootPage.LibrarySource.RecentReads.Remove(thisBook);
            }
            rootPage.CallUpdateBooks();
        }

        private void GetSystemFonts()
        {
            fontBlocks = new List<TextBlock>();
            string[] fonts = {"Arial", "Arial Black", "Arial Unicode MS", "Calibri", "Cambria",
                                 "Cambria Math", "Comic Sans MS", "Candara", "Consolas", "Constantia",
                                 "Corbel", "Courier New", "George", "Lucida Sans Unicode", "Segoe UI",
                                 "Symbol", "Tahoma", "Times New Roman", "Trebuchet MS", "Verdana"};

            for (int i = 0; i < fonts.Length; i++)
            {
                fontBlocks.Add(new TextBlock());
                fontBlocks[i].Text = fonts[i];
                fontBlocks[i].FontFamily = new FontFamily(fonts[i]);
                FontFamilyListBox.Items.Add(fontBlocks[i]);
            }
        }


        /// <summary>
        /// Takes the chapters full html file and loads it, then converts to text, and finally
        /// loads that into a RichTextBox and adds that to the grid
        /// </summary>
        private async Task CreateFirstPage()
        {
            HtmlDocument htmlDoc = new HtmlDocument();
            
            if (thisBook.CurrentChapter == 0)
            {
                //adds image to the first page of each book from assets or storage
                Image image = new Image();
                InlineUIContainer containers = new InlineUIContainer();
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
            StorageFolder appBaseFolder = ApplicationData.Current.LocalFolder;
            StorageFolder imageFolder = await IO.CreateOrGetFolders(appBaseFolder, folders);
            StorageFile imageFile = await imageFolder.GetFileAsync(folders[folders.Length - 1]);
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
            myRTB.FontSize = rootPage.LibrarySource.ReadingFontSize;
            myRTB.FontFamily = new FontFamily(rootPage.LibrarySource.ReadingFontFamily);
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
                listRTBO[pageNumber].Tapped += myRTB_Tapped;
                pageNumber++;
                LayoutRoot.Children.Add(listRTBO[pageNumber - 1]);
                myRTB.Visibility = Visibility.Collapsed;
                LayoutRoot.UpdateLayout();

                while (listRTBO[pageNumber - 1].HasOverflowContent)
                {
                    listRTBO.Add(new RichTextBlockOverflow());
                    listRTBO[pageNumber - 1].OverflowContentTarget = listRTBO[pageNumber];
                    listRTBO[pageNumber - 1].Visibility = Visibility.Collapsed;
                    listRTBO[pageNumber].Visibility = Visibility.Visible;
                    listRTBO[pageNumber].Tapped += myRTB_Tapped;
                    LayoutRoot.Children.Add(listRTBO[pageNumber]);
                    pageNumber++;
                    LayoutRoot.UpdateLayout();
                }
            }
            thisBook.Chapters[thisBook.CurrentChapter].PageCount = LayoutRoot.Children.Count();
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
            }
        }

        private void ChaptersBarButton_Click(object sender, RoutedEventArgs e)
        {

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
        /// Shows the different font families in the textblock below the list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FontFamilyListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TextBlock newFontFamily = (TextBlock)((sender as ListBox).SelectedItem);
            FontCheckerBlock.FontFamily = new FontFamily(newFontFamily.Text);
        }

        /// <summary>
        /// Takes the selected font size and font families and applies them to the reading page
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void AcceptButton_Click(object sender, RoutedEventArgs e)
        {
            string newFont = (string)FontSizeListBox.SelectedItem as string;
            rootPage.LibrarySource.ReadingFontSize = Convert.ToInt32(newFont);
            TextBlock newFontFamily = (TextBlock)FontFamilyListBox.SelectedItem as TextBlock;
            rootPage.LibrarySource.ReadingFontFamily = newFontFamily.Text;
            FontFlyout.Hide();
            BottomAppBar.Visibility = Visibility.Collapsed;
            LayoutRoot.Children.Clear();
            await CreateFirstPage();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            FontFlyout.Hide();
        }
    }
}
