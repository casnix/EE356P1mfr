using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.Drawing;
using System.Drawing.Design;
using System.Drawing.Imaging;
using System.IO;

// This is a WPF project, but I have to include this to be able to
//   enumerate the fixed-width fonts because .NET has no function for listing
//   fixed-width font families
using System.Windows.Forms; // I use the TextRenderer class from this
using System.Drawing.Drawing2D;

// 13 tall
// 13 wide
namespace EE356P1mfr
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool ComponentsInitialized;
        public ICollection<System.Windows.Media.FontFamily> FixedWidthFonts { get; set; }
        public Dictionary<int, System.Windows.Media.FontFamily> FontIndexer { get; set; }
        private Dictionary<int, System.Drawing.FontFamily> FormsFontIndexer { get; set; }
//        private List<System.Windows.Media.FontFamily> FixedWidthFontsEnumerated { get; set; }
        private Dictionary<int[], char> ASCIIChars { get; set; }
        private List<int[]> AvailableShades { get; set; }
        private List<int> AvailableShadeInts { get; set; }
        private Dictionary<int, int[]> ShadeMap { get; set; }
        private string AvailableASCIIString;
        private Dictionary<int[], Bitmap> ASCIIShades { get; set; }
        private bool ColorOutput;
        private float SelectedFontSize;
        private float[] AvailableFontSizes = new float[] { 12, 14, 18, 24, 28, 36 };
        private string OpenedFile;
        private string Ready = "Ready.";
        private bool UnsavedWork = false;
        private BitmapImage OpenBitmap = null;
        // It would be nice to write a memory map for this one so that I don't have duplicate images in memory
        private Bitmap OprOpenBitmap = null;
        private BitmapImage ConversionBitmap = null;
        private Bitmap OprConversionBitmap = null;
        private int[] BitmapXY;
        private int X = 0;
        private int Y = 1;
        private int R = 0;
        private int G = 1;
        private int B = 2;

        public MainWindow()
        {
            this.ComponentsInitialized = false;
            // Fill out font list with monospaced fonts available from
            // System.Drawing.  I believe this will be all installed TrueType fixed-width fonts
            this.SelectedFontSize = AvailableFontSizes[1];
            this.FixedWidthFonts = EnumerateFixedWidthFonts();

            // Fill out ASCII enumerators
            this.AvailableASCIIString = "`1234567890-=~!@#$%^&*()_+qwertyuiop[]\\QWERTYUIOP{}|asdfghjkl;'ASDFGHJKL:\"zxcvbnm,./ZXCVBNM<>?█";
            InitializeComponent();
            this.ComponentsInitialized = true;
            // Initialize color buttons/status menu items
            this.ColorOutput = false;
            this.mnuOptionsToggle_Click(null, null);

            this.ASCIIShades = this.CalculateFontShades();

            SetWindowStatus(Ready);
        }

        private void SetWindowStatus(string status)
        {
            lblFooterStatus.Content = "Status: " + status;
        }

        private void OpenNewImage()
        {
            // Check for unsaved work
            if (UnsavedWork)
            {
                MessageBoxResult haveISavedYet = System.Windows.MessageBox.Show("You have unsaved work.  Are you sure you want to open a different image?", "Are you sure?", MessageBoxButton.YesNo);
                if(haveISavedYet == MessageBoxResult.No) { return; }
            }

            // Show file dialog
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".bmp";
            dlg.Filter = "BMP Files (*.bmp)|*.bmp|WBMP Files (*.wbmp)|*.wbmp";
            Nullable<bool> r = dlg.ShowDialog();

            if (r == false) { return; } // User pressed cancel
            this.OpenedFile = dlg.FileName;
            
            // Check if exists
            if (!File.Exists(OpenedFile))
            {
                System.Windows.MessageBox.Show("No such file as " + OpenedFile, "Error!");
                this.OpenedFile = null;
                return;
            }
            this.OpenBitmap = new BitmapImage(new Uri(OpenedFile, UriKind.Relative));
            this.OprOpenBitmap = new Bitmap(OpenedFile);
            // Find which side is longer
            if (OpenBitmap.Width > OpenBitmap.Height)
            {
                lblYourPic.Visibility = Visibility.Hidden;
                rectInput.Visibility = Visibility.Hidden;
                imgInput.Visibility = Visibility.Visible;
                
                imgInput.Source = OpenBitmap;
            }else if(OpenBitmap.Width < OpenBitmap.Height)
            {
                lblYourPic.Visibility = Visibility.Hidden;
                rectInput.Visibility = Visibility.Hidden;
                imgInput.Visibility = Visibility.Visible;
                imgInput.Source = OpenBitmap;
            }
            else if(OpenBitmap.Width == OpenBitmap.Height)
            {
                lblYourPic.Visibility = Visibility.Hidden;
                rectInput.Visibility = Visibility.Hidden;
                imgInput.Visibility = Visibility.Visible;
                imgInput.Source = OpenBitmap;
            }

            UnsavedWork = false;
        }

        private int[] PullAverageRGBFromBmp(Bitmap bm)
        {
            BitmapData srcData = bm.LockBits(
            new System.Drawing.Rectangle(0, 0, bm.Width, bm.Height),
            ImageLockMode.ReadOnly,
            System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            int stride = srcData.Stride;

            IntPtr Scan0 = srcData.Scan0;

            int[] totals = new int[] { 0, 0, 0 };

            int width = bm.Width;
            int height = bm.Height;

            unsafe
            {
                byte* p = (byte*)(void*)Scan0;

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        for (int color = 0; color < 3; color++)
                        {
                            int idx = (y * stride) + x * 3 + color;

                            totals[color] += p[idx];
                        }
                    }
                }
            }

            return new int[] { totals[2] / (width * height), totals[1] / (width * height), totals[0] / (width * height) };
        }

        private Bitmap ConvertToGrayscale()
        {
            Bitmap newBitmap = new Bitmap(OprOpenBitmap.Width, OprOpenBitmap.Height);
            Graphics g = Graphics.FromImage(newBitmap);
            ColorMatrix colorMatrix = new ColorMatrix(
            new float[][]
                {
                    new float[] {.3f, .3f, .3f, 0, 0},
                    new float[] {.59f, .59f, .59f, 0, 0},
                    new float[] {.11f, .11f, .11f, 0, 0},
                    new float[] {0, 0, 0, 1, 0},
                    new float[] {0, 0, 0, 0, 1}
                });
            ImageAttributes attributes = new ImageAttributes();
            attributes.SetColorMatrix(colorMatrix);
            g.DrawImage(OprOpenBitmap, new System.Drawing.Rectangle(0, 0, OprOpenBitmap.Width, OprOpenBitmap.Height),
               0, 0, OprOpenBitmap.Width, OprOpenBitmap.Height, GraphicsUnit.Pixel, attributes);

            g.Dispose();
            return newBitmap;
        }

        private void GenerateColorArt()
        {
            SetWindowStatus("Converting to grayscale...");
            Bitmap grayscaleBmp = ConvertToGrayscale();

            // This will turn a picture with a length and height that isn't cleanly divisible by font height and width
            // into a picture that is.
            int postWidth = grayscaleBmp.Width / BitmapXY[X];
            int postHeight = grayscaleBmp.Height / BitmapXY[Y];
            int xMargin = (grayscaleBmp.Width - postWidth * BitmapXY[X]) / 2;
            int yMargin = (grayscaleBmp.Height - postHeight * BitmapXY[Y]) / 2;
            this.OprConversionBitmap = new Bitmap(postWidth * BitmapXY[X], postHeight * BitmapXY[Y]);
            Graphics c = Graphics.FromImage(OprConversionBitmap);
            c.Clear(System.Drawing.Color.White);

            SetWindowStatus("Matching shaders...");
            for(int x = (postWidth)-1; x > -1; x--)
            {
                for (int y = (postHeight)-1; y > -1; y--)
                {
                    // First, find the appropriate shade
                    int[] pureShade = CalculateShader(x, y);
                    int[] actualShade = FindNearestShader(pureShade);
                    
                    // The following is unique to the color portion
                    Bitmap workingSquare = GetSquare(x, y);
                    int[] avgRGB = PullAverageRGBFromBmp(workingSquare);
                    FillSquare(actualShade, x, y, avgRGB);
                }
            }

            imgInput.Visibility = Visibility.Hidden;
            rectGeneration.Visibility = Visibility.Visible;
            this.ConversionBitmap = Bitmap2BitmapImage(OprConversionBitmap);
            imgASCII.Source = ConversionBitmap;
            SetWindowStatus("Done.");
            return;
        }

        private void GenerateBlackArt()
        {
            SetWindowStatus("Converting to grayscale...");
            Bitmap grayscaleBmp = ConvertToGrayscale();

            // This will turn a picture with a length and height that isn't cleanly divisible by font height and width
            // into a picture that is.
            int postWidth = grayscaleBmp.Width / BitmapXY[X];
            int postHeight = grayscaleBmp.Height / BitmapXY[Y];
            int xMargin = (grayscaleBmp.Width - postWidth * BitmapXY[X]) / 2;
            int yMargin = (grayscaleBmp.Height - postHeight * BitmapXY[Y]) / 2;
            this.OprConversionBitmap = new Bitmap(postWidth * BitmapXY[X], postHeight * BitmapXY[Y]);
            Graphics c = Graphics.FromImage(OprConversionBitmap);
            c.Clear(System.Drawing.Color.White);

            SetWindowStatus("Matching shaders...");
            for (int x = (postWidth) - 1; x > -1; x--)
            {
                for (int y = (postHeight) - 1; y > -1; y--)
                {
                    // First, find the appropriate shade
                    int[] pureShade = CalculateShader(x, y);
                    int[] actualShade = FindNearestShader(pureShade);

                    FillSquare(actualShade, x, y, new int[] { 0, 0, 0 });
                }
            }

            imgInput.Visibility = Visibility.Hidden;
            rectGeneration.Visibility = Visibility.Visible;
            this.ConversionBitmap = Bitmap2BitmapImage(OprConversionBitmap);
            imgASCII.Source = ConversionBitmap;
        }

        private Bitmap GetSquare(int postX, int postY)
        {
            Bitmap ret = new Bitmap(BitmapXY[X], BitmapXY[Y]);
            for(int x = 0; x < BitmapXY[X]; x++)
            {
                for(int y = 0; y < BitmapXY[Y]; y++)
                {
                    ret.SetPixel(x, y, OprOpenBitmap.GetPixel(postX + x, postY + y));
                }
            }

            return ret;
        }

        private int ShadeToInt(int[] shade)
        {
            string StrShade = "" + shade[0] + "" + shade[1] + "" + shade[2];
            int intShade = Int32.Parse(StrShade);

            return intShade;
        }
         
        private int[] FindNearestShader(int[] pure)
        {
            int nuPure = ShadeToInt(pure);
            int nearest = AvailableShadeInts.OrderBy(x => (Math.Abs(x - nuPure))).First();
            return ShadeMap[nearest];
        }

        private int[] CalculateShader(int postX, int postY)
        {
            Bitmap tmp = new Bitmap(BitmapXY[X], BitmapXY[Y]);
            for(int x = 0; x < BitmapXY[X]; x++)
            {
                for(int y = 0; y < BitmapXY[Y]; y++)
                {
                    tmp.SetPixel(x, y, OprConversionBitmap.GetPixel(postX + x, postY + y));
                }
            }

            return PullAverageRGBFromBmp(tmp);
        }

        private void FillSquare(int[] shade, int postX, int postY, int[] vs)
        {
            if (ColorOutput == false)
            {
                for(int x = 0; x < BitmapXY[X]; x++)
                {
                    for(int y = 0; y < BitmapXY[Y]; y++)
                    {
                        OprConversionBitmap.SetPixel(postX + x, postY + y, ASCIIShades[shade].GetPixel(x, y));
                    }
                }

                return;
            }

            Bitmap gennedBmp = GenerateShader(shade, vs);
            for(int x = 0; x < BitmapXY[X]; x++)
            {
                for(int y = 0; y < BitmapXY[Y]; y++)
                {
                    OprConversionBitmap.SetPixel(postX + x, postY + y, gennedBmp.GetPixel(x, y));
                }
            }

            return;
        }

        private Bitmap GenerateShader(int[] shade, int[] rgb)
        {
            int fontIndex = cmboFonts.SelectedIndex;
            int mX = 0;
            int mY = 0;
            Bitmap bmp = new Bitmap(100, 100);
            char ch = ASCIIChars[shade];
            for (int i = 0; i < 1; i++)
            {
                // Find pixel size, and create bitmap of character
                Graphics g = Graphics.FromImage(bmp);

                Font myFont = new Font(FormsFontIndexer[fontIndex], SelectedFontSize, GraphicsUnit.Pixel);
                SizeF size = g.MeasureString(ch.ToString(), myFont);
                PointF rect = new PointF(size.Width, size.Height);
                mX = (int)rect.X;
                mY = (int)rect.Y;
                //System.Windows.MessageBox.Show(""+(myFont.SizeInPoints / 72 * g.DpiX));

                //System.Windows.MessageBox.Show("X:"+ (int)rect.X+"\nY:"+(int)rect.Y+"\nfX"+rect.X+"\nfY:"+rect.Y);
                Bitmap outBmp = new Bitmap((int)rect.X, (int)rect.Y, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                //todo set pixelformat
                Graphics o = Graphics.FromImage(outBmp);
                o.Clear(System.Drawing.Color.White);
                o.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                o.InterpolationMode = InterpolationMode.HighQualityBicubic;
                o.PixelOffsetMode = PixelOffsetMode.None;
                //System.Windows.MessageBox.Show("" + ch);
                o.DrawString(ch.ToString(), myFont, new SolidBrush(System.Drawing.Color.FromArgb(rgb[R], rgb[G], rgb[B])), 0, 0);
                outBmp.Save("./outbmp.gen.bmp");
               

                g.Dispose();
                o.Dispose();
                outBmp.Dispose();
            }

            return bmp;
        }

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        private BitmapImage Bitmap2BitmapImage(Bitmap bitmap)
        {
            IntPtr hBitmap = bitmap.GetHbitmap();
            BitmapSource retBmpSrc;

            try
            {
                retBmpSrc = (BitmapSource)System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                             hBitmap,
                             IntPtr.Zero,
                             Int32Rect.Empty,
                             BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                DeleteObject(hBitmap);
            }

            BitmapImage retBmpImg = retBmpSrc as BitmapImage;
            return retBmpImg;
        }

        private void btnImgLoad_Click(object sender, RoutedEventArgs e)
        {
            this.OpenNewImage();
        }

        private void btnGenerate_Click(object sender, RoutedEventArgs e)
        {
            if (ColorOutput)
            {
                GenerateColorArt();
            }
            else
            {
                GenerateBlackArt();
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        { }

        private void btnConvert_Click(object sender, RoutedEventArgs e)
        { }

        private void mnuFileOpen_Click(object sender, RoutedEventArgs e)
        {
            OpenNewImage();
        }

        private void mnuFileSave_Click(object sender, RoutedEventArgs e)
        { }

        private void mnuFileClose_Click(object sender, RoutedEventArgs e)
        {
            // If UNSAVED_IMAGE is set, ask if we want to save it
            // ...todo

            MessageBoxResult closeBox = System.Windows.MessageBox.Show("Are you sure you want to quit? You will lose any unsaved work.", "Confirm", MessageBoxButton.YesNo);
            if(closeBox == MessageBoxResult.No)
            {
                return;
            }

            System.Windows.Application.Current.Shutdown(0x00);
        }

        private void mnuOptionsToggle_Click(object sender, RoutedEventArgs e)
        {
            this.ColorOutput = !this.ColorOutput;
            if (this.ColorOutput)
            {
                mnuItmColorOutputMode.Header = "Color output is ON";
                mnuColorstatus.Header = "Color";
            }
            else
            {
                mnuItmColorOutputMode.Header = "Color output is OFF";
                mnuColorstatus.Header = "Black and white";
            }
        }

     //   private void mnuHelpDisplay_Click(object sender, RoutedEventArgs e)
     //   { }

        private void mnuHelpAbout_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("EE356P1mfr -- Project 1 in C# (WPF, .NET) for EE-356 Fall 2018\nAuthor: Matt Rienzo\nCopyright: Matt Rienzo, 2018\nGithub: https://github.com/casnix/EE356P1mfr");
        }

        private void cmboFontSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedFontSize = AvailableFontSizes[cmboFontSize.SelectedIndex];
        }

        private void cmboFonts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!ComponentsInitialized)
            {
                return;
            }

            ASCIIShades = CalculateFontShades();
            SetWindowStatus(Ready);
        }
        
        // EnumerateFixedWidthFonts() -- Function's function should be pretty self explanatory
        // Takes no arguments, but returns an ICollection of font families
        //      Works by measuring the length of `WiOo' against `....'.  If the length
        //      is the same, then it's probably a fixed-width font.  The only false positive would
        //      be a font specially crafted to make this function produce a false positive
        //      (which should be a UI issue and not a security issue unless TextRenderer.MeasureText
        //      has some flaw susceptible to buffer overflow or something...)
        //      I know this is hacky, but I couldn't find a better way to do it.
        private ICollection<System.Windows.Media.FontFamily> EnumerateFixedWidthFonts()
        {
            ICollection<System.Windows.Media.FontFamily> MonoSpaceFonts = new List<System.Windows.Media.FontFamily>();
            int index = 0;
            // Wipe shades
            //            this.FixedWidthFontsEnumerated = null;
            this.FontIndexer = new Dictionary<int, System.Windows.Media.FontFamily>();
            this.FormsFontIndexer = new Dictionary<int, System.Drawing.FontFamily>();
            string exceptionsFonts = null;
            string exceptionText = null;
            foreach (System.Drawing.FontFamily ff in System.Drawing.FontFamily.Families)
            {
                if (ff.IsStyleAvailable(System.Drawing.FontStyle.Regular))
                {
                    float diff;
                    using (Font font = new Font(ff, SelectedFontSize))
                    {
                        diff = TextRenderer.MeasureText("WiOo", font).Width - TextRenderer.MeasureText("....", font).Width;
                    }
                    if (Math.Abs(diff) < float.Epsilon * 2)
                    {
                        try
                        {
                            System.Windows.Media.FontFamily fo = new System.Windows.Media.FontFamily(ff.Name);
                            MonoSpaceFonts.Add(new System.Windows.Media.FontFamily(ff.Name));
//                            this.FixedWidthFontsEnumerated.Add(new System.Windows.Media.FontFamily(ff.Name));
                            this.FontIndexer.Add(index, fo);
                            this.FormsFontIndexer.Add(index, ff);
                        }
                        catch(Exception e)
                        {
                            exceptionsFonts += ff.Name + "\n";
                            exceptionText += e.ToString() + "\nException message:" + e.Message + "\n\n";
                        }
                        index++;
                    }
                }

            }

            if(exceptionsFonts != null)
            {
                MessageBoxResult r = System.Windows.MessageBox.Show("Encountered exception(s) while enumerating fonts!\nThe following font families generated an exception:\n" + exceptionsFonts + "\nSee the exception(s) text(s)?",
                    "Font exception", MessageBoxButton.YesNo);

                if(r == MessageBoxResult.Yes)
                {
                    System.Windows.MessageBox.Show(exceptionText);
                }
            }

            if(MonoSpaceFonts == null)
            {
                // Err 0xEF for "Empty Fonts"
                System.Windows.MessageBox.Show("Application Error: There are no fixed width fonts! (Error 0xEF)");
                System.Windows.Application.Current.Shutdown(0xee);
            }
            return MonoSpaceFonts;
        }
        
        private Dictionary<int[], Bitmap> CalculateFontShades()
        {
            SetWindowStatus("Calculating font shaders...");

            Dictionary<int[], Bitmap> retDict = new Dictionary<int[], Bitmap>();
            int fontIndex = cmboFonts.SelectedIndex;
            this.ASCIIChars = new Dictionary<int[], char>();
            this.AvailableShades = new List<int[]>();
            this.AvailableShadeInts = new List<int>();
            this.ShadeMap = new Dictionary<int, int[]>();
            int mX = 0;
            int mY = 0;
            System.Drawing.Image bmp = new Bitmap(100, 100);
            for (int i = 0; i < AvailableASCIIString.Length; i++)
            {
                // Find pixel size, and create bitmap of character
                Graphics g = Graphics.FromImage(bmp);

                Font myFont = new Font(FormsFontIndexer[fontIndex], SelectedFontSize, GraphicsUnit.Pixel);
                SizeF size = g.MeasureString(AvailableASCIIString[i].ToString(), myFont);
                PointF rect = new PointF(size.Width, size.Height);
                mX = (int)rect.X;
                mY = (int)rect.Y;
                //System.Windows.MessageBox.Show(""+(myFont.SizeInPoints / 72 * g.DpiX));
                
                //System.Windows.MessageBox.Show("X:"+ (int)rect.X+"\nY:"+(int)rect.Y+"\nfX"+rect.X+"\nfY:"+rect.Y);
                Bitmap outBmp = new Bitmap((int)rect.X, (int)rect.Y, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                //todo set pixelformat
                Graphics o = Graphics.FromImage(outBmp);
                o.Clear(System.Drawing.Color.White);
                o.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                o.InterpolationMode = InterpolationMode.HighQualityBicubic;
                o.PixelOffsetMode = PixelOffsetMode.None;
                //System.Windows.MessageBox.Show("" + AvailableASCIIString[i]);
                o.DrawString(AvailableASCIIString[i].ToString(), myFont, System.Drawing.Brushes.Black, 0, 0);
                outBmp.Save("./outbmp.bmp");
                // Determine shade
                int[] shade = PullAverageRGBFromBmp(outBmp);


                // Check if any letter has the same shade (if so, skip)
                string StrShade = "" + shade[0] + "" + shade[1] + "" + shade[2];
                int intShade = Int32.Parse(StrShade);
                //if (!ASCIIChars.ContainsKey(shade) && !ShadeMap.ContainsKey()
                if (!ShadeMap.ContainsKey(intShade))
                {
                    ASCIIChars.Add(shade, AvailableASCIIString[i]);
                    retDict.Add(shade, outBmp);
                    AvailableShades.Add(shade);
                    AvailableShadeInts.Add(intShade);
                    ShadeMap.Add(intShade, shade);
                }

                
                g.Dispose();
                o.Dispose();
                outBmp.Dispose();
            }

            bmp.Dispose();
            this.BitmapXY = new int[] { mX, mY };
            return retDict;
        }
    }
}
