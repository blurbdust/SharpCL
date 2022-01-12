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

namespace ImageDemo
{
    public sealed partial class MainPage : Page
    {
        private Context context;
        private CommandQueue commandQueue;
        private Dictionary<string, Kernel> kernels;

        private const string kernelsCode = @"
                __kernel void do_md5s(__global char* size, __global char* in, __global char* out) {
	            // the following code is from a very old version of john, I'm copying the license and credits below
		    // MD5 OpenCL kernel based on Solar Designer's MD5 algorithm implementation at:
                    // http://openwall.info/wiki/people/solar/software/public-domain-source-code/md5
		    //
                    // This software is Copyright (c) 2010, Dhiru Kholia <dhiru.kholia at gmail.com>,
                    // and it is hereby released to the general public under the following terms:
                    // Redistribution and use in source and binary forms, with or without modification,
                    // are permitted.
                    //
                    // Useful References:
                    // 1. CUDA MD5 Hashing Experiments, http://majuric.org/software/cudamd5/
                    // 2. oclcrack, http://sghctoma.extra.hu/index.php?p=entry&id=11
                    // 3. http://people.eku.edu/styere/Encrypt/JS-MD5.html
                    // 4. http://en.wikipedia.org/wiki/MD5#Algorithm */

                    // Macros for reading/writing chars from int32's (from rar_kernel.cl) 
                    #define GETCHAR(buf, index) (((uchar*)(buf))[(index)])
                    #define PUTCHAR(buf, index, val) (buf)[(index)>>2] = ((buf)[(index)>>2] & ~(0xffU << (((index) & 3) << 3))) + ((val) << (((index) & 3) << 3))

                    // The basic MD5 functions
                    #define F(x, y, z)			((z) ^ ((x) & ((y) ^ (z))))
                    #define G(x, y, z)			((y) ^ ((z) & ((x) ^ (y))))
                    #define H(x, y, z)			((x) ^ (y) ^ (z))
                    #define I(x, y, z)			((y) ^ ((x) | ~(z)))

                    // The MD5 transformation for all four rounds.
                    #define STEP(f, a, b, c, d, x, t, s) \
                        (a) += f((b), (c), (d)) + (x) + (t); \
                        (a) = (((a) << (s)) | (((a) & 0xffffffff) >> (32 - (s)))); \
                        (a) += (b);

                    #define GET(i) (key[(i)])

                    int id = get_global_id(0);
	                uint key[16] = { 0 };
	                uint i;
	                uint num_keys = 1;
	                uint KEY_LENGTH = size[0];

                    for (int n = 0; n < size[0]; n++){
                        key[n] = in[n];
                    }

	                int base = id * (KEY_LENGTH / 4);

	                // padding code (borrowed from MD5_eq.c)
	                char *p = (char *) key;
	                for (i = 0; i != 64 && p[i]; i++);

                    PUTCHAR(key, i, 0x80);
                    PUTCHAR(key, 56, i << 3);
                    PUTCHAR(key, 57, i >> 5);

	                uint a, b, c, d;
	                a = 0x67452301;
	                b = 0xefcdab89;
	                c = 0x98badcfe;
	                d = 0x10325476;

                    // Round 1
                    STEP(F, a, b, c, d, GET(0), 0xd76aa478, 7)
	                STEP(F, d, a, b, c, GET(1), 0xe8c7b756, 12)
	                STEP(F, c, d, a, b, GET(2), 0x242070db, 17)
	                STEP(F, b, c, d, a, GET(3), 0xc1bdceee, 22)
	                STEP(F, a, b, c, d, GET(4), 0xf57c0faf, 7)
	                STEP(F, d, a, b, c, GET(5), 0x4787c62a, 12)
	                STEP(F, c, d, a, b, GET(6), 0xa8304613, 17)
	                STEP(F, b, c, d, a, GET(7), 0xfd469501, 22)
	                STEP(F, a, b, c, d, GET(8), 0x698098d8, 7)
	                STEP(F, d, a, b, c, GET(9), 0x8b44f7af, 12)
	                STEP(F, c, d, a, b, GET(10), 0xffff5bb1, 17)
	                STEP(F, b, c, d, a, GET(11), 0x895cd7be, 22)
	                STEP(F, a, b, c, d, GET(12), 0x6b901122, 7)
	                STEP(F, d, a, b, c, GET(13), 0xfd987193, 12)
	                STEP(F, c, d, a, b, GET(14), 0xa679438e, 17)
	                STEP(F, b, c, d, a, GET(15), 0x49b40821, 22)

                    // Round 2
                    STEP(G, a, b, c, d, GET(1), 0xf61e2562, 5)
	                STEP(G, d, a, b, c, GET(6), 0xc040b340, 9)
	                STEP(G, c, d, a, b, GET(11), 0x265e5a51, 14)
	                STEP(G, b, c, d, a, GET(0), 0xe9b6c7aa, 20)
	                STEP(G, a, b, c, d, GET(5), 0xd62f105d, 5)
	                STEP(G, d, a, b, c, GET(10), 0x02441453, 9)
	                STEP(G, c, d, a, b, GET(15), 0xd8a1e681, 14)
	                STEP(G, b, c, d, a, GET(4), 0xe7d3fbc8, 20)
	                STEP(G, a, b, c, d, GET(9), 0x21e1cde6, 5)
	                STEP(G, d, a, b, c, GET(14), 0xc33707d6, 9)
	                STEP(G, c, d, a, b, GET(3), 0xf4d50d87, 14)
	                STEP(G, b, c, d, a, GET(8), 0x455a14ed, 20)
	                STEP(G, a, b, c, d, GET(13), 0xa9e3e905, 5)
	                STEP(G, d, a, b, c, GET(2), 0xfcefa3f8, 9)
	                STEP(G, c, d, a, b, GET(7), 0x676f02d9, 14)
	                STEP(G, b, c, d, a, GET(12), 0x8d2a4c8a, 20)

                    // Round 3
                    STEP(H, a, b, c, d, GET(5), 0xfffa3942, 4)
	                STEP(H, d, a, b, c, GET(8), 0x8771f681, 11)
	                STEP(H, c, d, a, b, GET(11), 0x6d9d6122, 16)
	                STEP(H, b, c, d, a, GET(14), 0xfde5380c, 23)
	                STEP(H, a, b, c, d, GET(1), 0xa4beea44, 4)
	                STEP(H, d, a, b, c, GET(4), 0x4bdecfa9, 11)
	                STEP(H, c, d, a, b, GET(7), 0xf6bb4b60, 16)
	                STEP(H, b, c, d, a, GET(10), 0xbebfbc70, 23)
	                STEP(H, a, b, c, d, GET(13), 0x289b7ec6, 4)
	                STEP(H, d, a, b, c, GET(0), 0xeaa127fa, 11)
	                STEP(H, c, d, a, b, GET(3), 0xd4ef3085, 16)
	                STEP(H, b, c, d, a, GET(6), 0x04881d05, 23)
	                STEP(H, a, b, c, d, GET(9), 0xd9d4d039, 4)
	                STEP(H, d, a, b, c, GET(12), 0xe6db99e5, 11)
	                STEP(H, c, d, a, b, GET(15), 0x1fa27cf8, 16)
	                STEP(H, b, c, d, a, GET(2), 0xc4ac5665, 23)

                    // Round 4
                    STEP(I, a, b, c, d, GET(0), 0xf4292244, 6)
	                STEP(I, d, a, b, c, GET(7), 0x432aff97, 10)
	                STEP(I, c, d, a, b, GET(14), 0xab9423a7, 15)
	                STEP(I, b, c, d, a, GET(5), 0xfc93a039, 21)
	                STEP(I, a, b, c, d, GET(12), 0x655b59c3, 6)
	                STEP(I, d, a, b, c, GET(3), 0x8f0ccc92, 10)
	                STEP(I, c, d, a, b, GET(10), 0xffeff47d, 15)
	                STEP(I, b, c, d, a, GET(1), 0x85845dd1, 21)
	                STEP(I, a, b, c, d, GET(8), 0x6fa87e4f, 6)
	                STEP(I, d, a, b, c, GET(15), 0xfe2ce6e0, 10)
	                STEP(I, c, d, a, b, GET(6), 0xa3014314, 15)
	                STEP(I, b, c, d, a, GET(13), 0x4e0811a1, 21)
	                STEP(I, a, b, c, d, GET(4), 0xf7537e82, 6)
	                STEP(I, d, a, b, c, GET(11), 0xbd3af235, 10)
	                STEP(I, c, d, a, b, GET(2), 0x2ad7d2bb, 15)
	                STEP(I, b, c, d, a, GET(9), 0xeb86d391, 21)
			
                    // ok let's be honest, this isn't the hackiest thing in this repo :)
                    out[3] = (a + 0x67452301) >> 24;
                    out[2] = ((a + 0x67452301) << 8) >> 24;
                    out[1] = ((a + 0x67452301) << 16) >> 24;
                    out[0] = ((a + 0x67452301) << 24) >> 24;
	                out[7] = (b + 0xefcdab89) >> 24;
	                out[6] = ((b + 0xefcdab89) << 8) >> 24;
	                out[5] = ((b + 0xefcdab89) << 16) >> 24;
	                out[4] = ((b + 0xefcdab89) << 24) >> 24;
	                out[11] = (c + 0x98badcfe)  >> 24;
	                out[10] = ((c + 0x98badcfe) << 8) >> 24;
	                out[9] = ((c + 0x98badcfe) << 16) >> 24;
	                out[8] = ((c + 0x98badcfe) << 24) >> 24;
	                out[15] = (d + 0x10325476) >> 24;
	                out[14] = ((d + 0x10325476) << 8) >> 24;
	                out[13] = ((d + 0x10325476) << 16) >> 24;
	                out[12] = ((d + 0x10325476) << 24) >> 24;
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
        private void MD5_Click(object sender, RoutedEventArgs e)
        {
            ExecuteKernel_Hash("do_md5s");
        }

        private async void ExecuteKernel_Hash(string kernelName)
        {
            byte[] tmp = Encoding.UTF8.GetBytes("hashcat");
            SharpCL.Buffer srcBuffer = context.CreateBuffer(tmp, MemoryFlags.ReadOnly | MemoryFlags.CopyHostPointer);
            //SharpCL.Image srcImg = context.CreateImage1DBuffer(srcBuffer, tmp, (ulong)tmp.Length, MemoryFlags.ReadOnly | MemoryFlags.CopyHostPointer, ImageChannelOrder.RGB, ImageChannelType.SignedInt16);

            if (srcBuffer == null)
            {
                ContentDialog dialog = new ContentDialog()
                {
                    Title = "srcBuffer",
                    Content = "null",
                    CloseButtonText = "Quit"
                };
                await dialog.ShowAsync();
                Application.Current.Exit();
                return;
            }
            byte[] size = { (byte)tmp.Length, 0 };
            SharpCL.Buffer sizeBuffer = context.CreateBuffer(size, MemoryFlags.ReadOnly | MemoryFlags.CopyHostPointer);
            if (sizeBuffer == null)
            {
                ContentDialog dialog = new ContentDialog()
                {
                    Title = "sizeBuffer",
                    Content = "null",
                    CloseButtonText = "Quit"
                };
                await dialog.ShowAsync();
                Application.Current.Exit();
                return;
            }

            byte[] tmp2 = Encoding.UTF8.GetBytes("AAAAAAAAAAAAAAAA");
            SharpCL.Buffer dstBuffer = context.CreateBuffer<char>((ulong)16, MemoryFlags.WriteOnly );
            //SharpCL.Image dstImg = context.CreateImage1DBuffer(dstBuffer, tmp, (ulong)tmp.Length, MemoryFlags.WriteOnly | MemoryFlags.CopyHostPointer, ImageChannelOrder.RGB, ImageChannelType.SignedInt16);
            
            if (dstBuffer == null)
            {
                ContentDialog dialog = new ContentDialog()
                {
                    Title = "dstBuffer",
                    Content = "null",
                    CloseButtonText = "Quit"
                };
                await dialog.ShowAsync();
                Application.Current.Exit();
                return;
            }


            kernels[kernelName].SetArgument(0, sizeBuffer);
            kernels[kernelName].SetArgument(1, srcBuffer);
            kernels[kernelName].SetArgument(2, dstBuffer);

            //Event kernelEvent = commandQueue.EnqueueKernel(kernels[kernelName], new ulong[] { (ulong)16 }, new ulong[] { (ulong)0 }, new ulong[] { (ulong)8 });
            Event kernelEvent = commandQueue.EnqueueKernel(kernels[kernelName], new ulong[] { (ulong)16 });
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

            // Read destination data; fails due to null buffer
            byte[] destinationData = new byte[16];
            commandQueue.EnqueueReadBuffer(dstBuffer, destinationData, true, default, 0, new List<Event> { kernelEvent });
            if (commandQueue.Error)
            {
                ContentDialog dialog = new ContentDialog()
                {
                    Title = "Error!",
                    Content = "Can't enqueue read buffer command on the command queue.",
                    CloseButtonText = "Quit"
                };
                await dialog.ShowAsync();
                return;
            }
            string hexString = BitConverter.ToString(destinationData);
            hexString = hexString.Replace("-", "");
            TextBox.Text = hexString;

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
