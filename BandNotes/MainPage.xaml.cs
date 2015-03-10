using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Microsoft.Band;
using Microsoft.Band.Tiles;
using Windows.UI.Xaml.Media.Imaging;
using Microsoft.Band.Notifications;
using Windows.UI.Popups;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=391641

namespace BandNotes
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // TODO: Prepare page for display here.

            // TODO: If your application contains multiple pages, ensure that you are
            // handling the hardware Back button by registering for the
            // Windows.Phone.UI.Input.HardwareButtons.BackPressed event.
            // If you are using the NavigationHelper provided by some templates,
            // this event is handled for you.
        }

        private async void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            sendButton.IsEnabled = false;

            IBandInfo[] pairedBands = await BandClientManager.Instance.GetBandsAsync();

            if (pairedBands.Count() <= 0)
            {
                await new MessageDialog("Cannot find a paired Band. Please ensure your Band is connected.").ShowAsync();
                sendButton.IsEnabled = true;
                return;
            }

            try
            {
                using(IBandClient bandClient = await BandClientManager.Instance.ConnectAsync(pairedBands[0]))
                {
                    Guid tileGuid;
                    IEnumerable<BandTile> tiles = await bandClient.TileManager.GetTilesAsync();
                    if (tiles.Count() <= 0)
                    {
                        int tileCapacity = await bandClient.TileManager.GetRemainingTileCapacityAsync();
                        if (tileCapacity <= 0)
                        {
                            //Print an error
                            sendButton.IsEnabled = true;
                            return;
                        }

                        BandIcon tileIcon = await LoadIcon("ms-appx:///Assets/Notebook-46.png");
                        BandIcon smallIcon = await LoadIcon("ms-appx:///Assets/Notebook-24.png");

                        tileGuid = Guid.NewGuid();
                        BandTile tile = new BandTile(tileGuid)
                        {
                            IsBadgingEnabled = true,
                            Name = "Band Notes",
                            SmallIcon = smallIcon,
                            TileIcon = tileIcon
                        };

                        if (await bandClient.TileManager.AddTileAsync(tile) != true)
                        {
                            //print an error
                            sendButton.IsEnabled = true;
                            return;
                        }
                    }
                    else
                    {
                        tileGuid = tiles.First().TileId;
                    }

                    await bandClient.NotificationManager.SendMessageAsync
                    (
                        tileGuid,
                        "Band Notes", 
                        txtBody.Text, 
                        DateTimeOffset.Now
                    );
                }
            }
            catch (BandException ex)
            {
                // handle a band connection exception
                sendButton.IsEnabled = true;
            }
            sendButton.IsEnabled = true;
        }

        private async Task<BandIcon> LoadIcon(string uri)
        {
            StorageFile imageFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri(uri));

            using (IRandomAccessStream fileStream = await imageFile.OpenAsync(FileAccessMode.Read))
            {
                WriteableBitmap bitmap = new WriteableBitmap(1, 1);
                await bitmap.SetSourceAsync(fileStream);
                return bitmap.ToBandIcon();
            }
        }

        private async Task<BandIcon> LoadIconChar(string txt, int size, string font="Segoe UI Symbol")
        {
            var canvas = new Canvas();

            var textBlock = new TextBlock { Text = txt };
            canvas.Children.Add(textBlock);
            Canvas.SetLeft(textBlock, 0);
            Canvas.SetTop(textBlock, 0);

            var bitmap = new WriteableBitmap(size, size);
            bitmap.Render(canvas, null);
            bitmap.Invalidate();
            return bitmap;
        }
    }
}
