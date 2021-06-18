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
using SharpCL;
using System.Diagnostics;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;
using System.Net.Http;
using System.Text;
//using System.Net.Http;

// Il modello di elemento Pagina vuota è documentato all'indirizzo https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x410

namespace ImageDemo
{
    /// <summary>
    /// Pagina vuota che può essere usata autonomamente oppure per l'esplorazione all'interno di un frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private Context context;
        private CommandQueue commandQueue;
        private Dictionary<string, Kernel> kernels;

        private const string kernelsCode = @"
            void md5(uint length_bytes) {
                int2 a = 0;
                int2 b = a;
                int2 c = a * b;
                uint i;
                uint bytes_left;
                char key[64];
                i = length_bytes;
            }
    
             __kernel void blur(read_only image2d_t source, write_only image2d_t destination) {
                // Get pixel coordinate
                int2 coord = (int2)(get_global_id(0), get_global_id(1));

                // Create a sampler that use edge color for coordinates outside the image
                const sampler_t sampler = CLK_NORMALIZED_COORDS_FALSE | CLK_ADDRESS_CLAMP_TO_EDGE | CLK_FILTER_NEAREST;

                // Blur using colors in a 7x7 square
                uint4 color = (uint4)(0, 0, 0, 0);
                for(int u=-3; u<=3; u++) {
                    for(int v=-3; v<=3; v++) {
                        color += read_imageui(source, sampler, coord + (int2)(u, v));
                    }
                }
                color /= 49;

                // Nic test
                uint k = 9;
                md5(k);
                // Write blurred pixel in destination image
                write_imageui(destination, coord, color);
             }

            __kernel void invert(read_only image2d_t source, write_only image2d_t destination) {
                // Get pixel coordinate
                int2 coord = (int2)(get_global_id(0), get_global_id(1));

                // Read color ad invert it (except for alpha value)
                uint4 color = read_imageui(source, coord);
                color.xyz = (uint3)(255,255,255) - color.xyz;

                // Write inverted pixel in destination image
                write_imageui(destination, coord, color);
             }

            __kernel void copy(read_only image2d_t input, write_only image2d_t output, float foo) {
                int2 coord = (int2)(get_global_id(0), get_global_id(1));
                write_imagef(output, coord, read_imagef(input, coord) + foo);
             }

        ";

        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // Get a context for the first GPU platform found
            context = Context.AutomaticContext(DeviceType.GPU);
            if (context == null)
            {
                ContentDialog dialog = new ContentDialog()
                {
                    Title = "No OpenCL compatible GPU found!",
                    Content = "Please install or update you GPU driver and retry.",
                    CloseButtonText = "Quit"
                };
                await dialog.ShowAsync();
                Application.Current.Exit();
                return;
            }
            else
            {
                String plats = "";
                foreach (Platform p in Platform.GetPlatforms())
                {
                    plats += "Name:\t" + p.Name + "\nVendor:\t" + p.Vendor + "\nVersion:\t" + p.Version + "\n";
                }
                ContentDialog dialog = new ContentDialog()
                {
                    Title = "GPU found!",
                    Content = plats,
                    CloseButtonText = "Close"
                };
                await dialog.ShowAsync();
            }
            
            // Get a command queue for the first available device in the context
            commandQueue = context.CreateCommandQueue();
            if(commandQueue == null)
            {
                ContentDialog dialog = new ContentDialog()
                {
                    Title = "Error!",
                    Content = "Can't create a command queue for the current context.",
                    CloseButtonText = "Quit"
                };
                await dialog.ShowAsync();
                Application.Current.Exit();
                return;
            }

            // Build all kernels from source code
            kernels = context.BuildAllKernels(kernelsCode);
            if(context.Error)
            {
                ContentDialog dialog = new ContentDialog()
                {
                    Title = "Error!",
                    Content = "Can't compile kernels, please check source code.",
                    CloseButtonText = "Quit"
                };
                await dialog.ShowAsync();
                Application.Current.Exit();
                return;
            }
        }

        private async void Open_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker picker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.PicturesLibrary
            };
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");

            Windows.Storage.StorageFile file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                using (IRandomAccessStream fileStream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read))
                {
                    BitmapDecoder decoder = await BitmapDecoder.CreateAsync(fileStream);
                    WriteableBitmap writeableBitmap = new WriteableBitmap((int)decoder.PixelWidth, (int)decoder.PixelHeight);
                    writeableBitmap.SetSource(fileStream);
                    SourceImage.Source = writeableBitmap;

                    BlurButton.IsEnabled = true;
                    InvertButton.IsEnabled = true;
                }
            }
            else
            {
                
                var imageUrl = "https://upload.wikimedia.org/wikipedia/commons/8/8e/Xbox_Velocity_Architecture_branding.jpg";
                var client = new HttpClient();
                Stream stream = await client.GetStreamAsync(imageUrl);
                var memStream = new MemoryStream();
                await stream.CopyToAsync(memStream);
                memStream.Position = 0;
                // since I'm hardcoding a url for an image, I might as well hardcode the dimensions of the image
                WriteableBitmap writeableBitmap = new WriteableBitmap(1920, 1080);
                writeableBitmap.SetSource(memStream.AsRandomAccessStream());
                SourceImage.Source = writeableBitmap;

                BlurButton.IsEnabled = true;
                InvertButton.IsEnabled = true;                
            }
        }

        private void Blur_Click(object sender, RoutedEventArgs e)
        {
            ExecuteKernel("blur");
        }

        private void Invert_Click(object sender, RoutedEventArgs e)
        {
            ExecuteKernel("invert");
        }

        private async void ExecuteKernel(string kernelName)
        {
            // Get source pixel data
            WriteableBitmap sourceBitmap = SourceImage.Source as WriteableBitmap;
            byte[] sourceData = sourceBitmap.PixelBuffer.ToArray();

            
            // Create OpenCL images
            SharpCL.Image test = context.CreateImage1D(1080, MemoryFlags.ReadOnly, ImageChannelOrder.RGB, ImageChannelType.SignedInt32);
            // checking if bug somehow in 2D image only
            // future nic here: the bug in dynamic data types for images (probably all and not just images)
            if (test == null)
            {
                ContentDialog dialog = new ContentDialog()
                {
                    Title = "CreateImage1D",
                    Content = "null",
                    CloseButtonText = "Quit"
                };
                await dialog.ShowAsync();
                Application.Current.Exit();
                return;
            }

            /*
             https://github.com/microsoft/OpenCLOn12/blob/master/test/openclon12test.cpp#L120
             cl::Image2D input(context, CL_MEM_READ_ONLY | CL_MEM_COPY_HOST_PTR,
                cl::ImageFormat(CL_RGBA, CL_FLOAT), width, height,
                sizeof(float) * width * 4, InputData);
             */
            SharpCL.Image sourceImage = context.CreateImage2D(sourceData, (ulong)sourceBitmap.PixelWidth, (ulong)sourceBitmap.PixelHeight,
                MemoryFlags.ReadOnly | MemoryFlags.CopyHostPointer, ImageChannelOrder.RGBA, ImageChannelType.UnsignedInt8);
            SharpCL.Image destinationImage = context.CreateImage2D((ulong)sourceBitmap.PixelWidth, (ulong)sourceBitmap.PixelHeight, MemoryFlags.WriteOnly, ImageChannelOrder.RGBA, ImageChannelType.UnsignedInt8);

            if (sourceImage == null)
            {
                ContentDialog dialog = new ContentDialog()
                {
                    Title = "Source Image",
                    Content = "null",
                    CloseButtonText = "Quit"
                };
                await dialog.ShowAsync();
                Application.Current.Exit();
                return;
            }

            if (destinationImage == null)
            {
                ContentDialog dialog = new ContentDialog()
                {
                    Title = "Destination Image",
                    Content = "null",
                    CloseButtonText = "Quit"
                };
                await dialog.ShowAsync();
                Application.Current.Exit();
                return;
            }

            // Run blur kernel
            kernels[kernelName].SetArgument(0, sourceImage);
            kernels[kernelName].SetArgument(1, destinationImage);
            Event kernelEvent = commandQueue.EnqueueKernel(kernels[kernelName], new ulong[] { (ulong)sourceBitmap.PixelWidth, (ulong)sourceBitmap.PixelHeight });
            if (commandQueue.Error)
            {
                ContentDialog dialog = new ContentDialog()
                {
                    Title = "Error!",
                    Content = "Can't enqueue kernel on the command queue.",
                    CloseButtonText = "Quit"
                };
                await dialog.ShowAsync();
                return;
            }

            // Read destination image data
            byte[] destinationData = new byte[sourceBitmap.PixelWidth * sourceBitmap.PixelHeight * 4];
            commandQueue.EnqueueReadImage(destinationImage, destinationData, default, default, true, new List<Event> { kernelEvent });
            if (commandQueue.Error)
            {
                ContentDialog dialog = new ContentDialog()
                {
                    Title = "Error!",
                    Content = "Can't enqueue read image command on the command queue.",
                    CloseButtonText = "Quit"
                };
                await dialog.ShowAsync();
                return;
            }

            // Use data to create a bitmap source for DestinationImage
            WriteableBitmap writeableBitmap = new WriteableBitmap(sourceBitmap.PixelWidth, sourceBitmap.PixelHeight);
            using (Stream stream = writeableBitmap.PixelBuffer.AsStream())
            {
                await stream.WriteAsync(destinationData, 0, sourceBitmap.PixelWidth * sourceBitmap.PixelHeight * 4);
            }
            DestinationImage.Source = writeableBitmap;

        }

    }
}
