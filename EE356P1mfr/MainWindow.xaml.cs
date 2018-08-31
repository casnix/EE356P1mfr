﻿using System;
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
        public ICollection<System.Windows.Media.FontFamily> FixedWidthFonts { get; set; }
        public Dictionary<int, System.Windows.Media.FontFamily> FontIndexer { get; set; }
        private Dictionary<int, System.Drawing.FontFamily> FormsFontIndexer { get; set; }
//        private List<System.Windows.Media.FontFamily> FixedWidthFontsEnumerated { get; set; }
        private string AvailableASCIIString;
        private Dictionary<float, Bitmap> ASCIIShades { get; set; }
        private bool ColorOutput;
        public MainWindow()
        {
            // Fill out font list with monospaced fonts available from
            // System.Drawing.  I believe this will be all installed TrueType fixed-width fonts
            this.FixedWidthFonts = EnumerateFixedWidthFonts();

            // Fill out ASCII enumerators
            this.AvailableASCIIString = "`1234567890-=~!@#$%^&*()_+qwertyuiop[]\\QWERTYUIOP{}|asdfghjkl;'ASDFGHJKL:\"zxcvbnm,./ZXCVBNM<>?█";
            InitializeComponent();

            // Initialize color buttons/status menu items
            this.ColorOutput = false;
            this.mnuOptionsToggle_Click(null, null);

            this.ASCIIShades = this.CalculateFontShades();
            // Really just adding this comment to test how the GitHub plugin for VisualStudio
            // handles dates/commit authors/etc.  !!! This was labeled as the `Sysfunc probe'
            // during commit/push on 8/23/2018.  -rienzo

        }


        private void btnImgLoad_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnGenerate_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        { }

        private void mnuFileOpen_Click(object sender, RoutedEventArgs e)
        { }

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

        private void mnuHelpDisplay_Click(object sender, RoutedEventArgs e)
        { }

        private void mnuHelpAbout_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("EE356P1mfr -- Project 1 in C# (WPF, .NET) for EE-356 Fall 2018\nAuthor: Matt Rienzo\nCopyright: Matt Rienzo, 2018\nGithub: https://github.com/casnix/EE356P1mfr");
        }

        private void cmboFonts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

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
                    using (Font font = new Font(ff, 16))
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
                    "Font exception action", MessageBoxButton.YesNo);

                if(r == MessageBoxResult.Yes)
                {
                    System.Windows.MessageBox.Show(exceptionText);
                }
            }

            if(MonoSpaceFonts == null)
            {
                System.Windows.MessageBox.Show("Application Error: There are no fixed width fonts! (Error 0xEE)");
                System.Windows.Application.Current.Shutdown(0xee);
            }
            return MonoSpaceFonts;
        }
        
        private Dictionary<float, Bitmap> CalculateFontShades()
        {
            int fontIndex = cmboFonts.SelectedIndex;
            
            System.Drawing.Image bmp = new Bitmap(100, 100);
            for (int i = 0; i < AvailableASCIIString.Length; i++)
            {
                Graphics g = Graphics.FromImage(bmp);

                Font myFont = new Font(FormsFontIndexer[fontIndex], 14, GraphicsUnit.Pixel);
                SizeF size = g.MeasureString(""+AvailableASCIIString[i], myFont);
                PointF rect = new PointF(size.Width, size.Height);

                System.Windows.MessageBox.Show("X:" + (int)Math.Ceiling(rect.X) + "\nY:" + (int)Math.Ceiling(rect.Y));
                System.Windows.Application.Current.Shutdown();
                System.Drawing.Image outBmp = new Bitmap((int)Math.Ceiling(rect.X), (int)Math.Ceiling(rect.X));
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.DrawString("" + AvailableASCIIString[i], myFont, new SolidBrush(System.Drawing.Color.Black), rect);
            }


            return null;
        }
    }
}
