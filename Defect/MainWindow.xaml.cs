using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Defect
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    #region Constructors

    public MainWindow()
      : this(null)
    {
    }

    public MainWindow(MainWindow creator)
    {
      InitializeComponent();
      if (creator == null) {
        ArenaLevels = 16;
        ArenaWidth = 256;
        ArenaHeight = 256;
        Scale = 1;
        SpeedSlider.Value = Math.Floor((SpeedSlider.Minimum + SpeedSlider.Maximum) / 4);
        Neighbourhood = CellNeighbourhood.VonNeumann;
      }
      else {
        ArenaLevels = creator.ArenaLevels;
        ArenaWidth = creator.ArenaWidth;
        ArenaHeight = creator.ArenaHeight;
        Scale = creator.Scale;
        SpeedSlider.Value = creator.SpeedSlider.Value;
        Neighbourhood = creator.Neighbourhood;
      }
      Reset();  // draw an initial random image
    }

    #endregion
    
    #region Settings

    /// <summary>
    /// Grid width
    /// </summary>
    public int ArenaWidth { get; set; }

    /// <summary>
    /// Grid Height
    /// </summary>
    public int ArenaHeight { get; set; }

    /// <summary>
    /// Level count
    /// </summary>
    public int ArenaLevels { get; set; }

    /// <summary>
    /// Time between steps in seconds
    /// </summary>
    public double StepInterval { get; set; }

    /// <summary>
    /// Cell neighbourhood type
    /// </summary>
    public CellNeighbourhood Neighbourhood { get; set; }

    /// <summary>
    /// Display scale
    /// </summary>
    public int Scale { get; set; }

    #endregion

    #region State

    /// <summary>
    /// The current defect array
    /// </summary>
    private DefectGrid Arena;

    /// <summary>
    /// Bitmap to render into
    /// </summary>
    private WriteableBitmap Bitmap;

    /// <summary>
    /// The current palette
    /// </summary>
    private uint[] Palette;

    /// <summary>
    /// Palette object for GIF images
    /// </summary>
    private GIF.ColorTable GIFPalette;

    /// <summary>
    /// Array for rendering
    /// </summary>
    private uint[] ColorData;

    /// <summary>
    /// Lock for thread comms
    /// </summary>
    private object Lock = new object();

    /// <summary>
    /// True when active
    /// </summary>
    private bool Going = false;

    /// <summary>
    /// ms between worker thread wakes
    /// </summary>
    private int ResponseInterval = 10;

    /// <summary>
    /// Background thread
    /// </summary>
    private Thread BackgroundThread = null;

    /// <summary>
    /// The base filename to record GIFs to
    /// </summary>
    private string RecordFilename = null;

    /// <summary>
    /// The current recording stream
    /// </summary>
    private FileStream RecordStream = null;

    /// <summary>
    /// Encoder for current recording stream
    /// </summary>
    private GIF RecordContext = null;

    /// <summary>
    /// Sequence number for disambiguating recording filenames
    /// </summary>
    private int RecordSequence = 1;

    /// <summary>
    /// Pixel data for recording
    /// </summary>
    private byte[] RecordPixels = null;

    #endregion

    #region Window Furniture

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
      StopExecuted(null, null);
      if (RecordStream != null) {
        ShutdownRecording();
      }
    }

    #endregion

    #region About Menu

    private void About(object sender, RoutedEventArgs e)
    {
      About about = new About()
      {
        Owner = this
      };
      about.ShowDialog();
    }

    #endregion

    #region Toolbar

    private void NewSpeed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
      lock (Lock) {
        StepInterval = 1000 - (e.NewValue - SpeedSlider.Minimum) / (SpeedSlider.Maximum - SpeedSlider.Minimum) * 1000;
      }
    }

    #endregion

    #region Commands specific to this window

    static MainWindow()
    {
      StopGoCommand.InputGestures.Add(new KeyGesture(Key.Space));
      OptionsCommand.InputGestures.Add(new KeyGesture(Key.O, ModifierKeys.Control));
      ApplicationCommands.Close.InputGestures.Add(new KeyGesture(Key.W, ModifierKeys.Control));
      ExitCommand.InputGestures.Add(new KeyGesture(Key.Q, ModifierKeys.Control));
      RecordToCommand.InputGestures.Add(new KeyGesture(Key.R, ModifierKeys.Control));
    }

    public static RoutedCommand GoCommand = new RoutedCommand();

    public static RoutedCommand StopGoCommand = new RoutedCommand();

    public static RoutedCommand ResetCommand = new RoutedCommand();

    public static RoutedCommand OptionsCommand = new RoutedCommand();

    public static RoutedCommand ExitCommand = new RoutedCommand();

    public static RoutedCommand RecordToCommand = new RoutedCommand();

    #endregion

    #region Command implementations

    private void GoExecuted(object sender, ExecutedRoutedEventArgs e)
    {
      if (!Going) {
        Going = true;
        BackgroundThread = new Thread(new ThreadStart(this.Worker))
        {
          Name = "MainWindow.Worker"
        };
        BackgroundThread.Start();
      }
    }

    private void GoCanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
      e.CanExecute = !Going;
    }

    private void StopExecuted(object sender, ExecutedRoutedEventArgs e)
    {
      if (Going) {
        lock (Lock) {
          Going = false;
        }
        BackgroundThread.Join();
        BackgroundThread = null;
      }
    }

    private void StopCanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
      e.CanExecute = Going;
    }

    private void StopGoExecuted(object sender, ExecutedRoutedEventArgs e)
    {
      if (Going) {
        StopExecuted(sender, e);
      }
      else {
        GoExecuted(sender, e);
      }
    }

    private void ResetExecuted(object sender, ExecutedRoutedEventArgs e)
    {
      Reset();
    }

    private void NewExecuted(object sender, ExecutedRoutedEventArgs e)
    {
      MainWindow newMainWindow = new MainWindow(this);
      newMainWindow.Show();
    }

    private void CloseExecuted(object sender, ExecutedRoutedEventArgs e)
    {
      this.Close();
    }

    private void OptionsExecuted(object sender, ExecutedRoutedEventArgs e)
    {
      Options options = new Options()
      {
        Owner = this,
        ParentMainWindow = this,
      };
      options.CancelButton.Visibility = Visibility.Collapsed;
      options.ShowDialog();
    }

    private void SaveAsExecuted(object sender, ExecutedRoutedEventArgs e)
    {
      BitmapFrame frame;
      lock (Lock) {
        UpdateColorData();
        UpdateBitmap();
        frame = BitmapFrame.Create(Bitmap.Clone());
      }
      SaveFileDialog saveFileDialog = new SaveFileDialog()
      {
        DereferenceLinks = true,
        Title = "Save image",
        Filter = "Image files|" + Tools.BitmapExtensions,
        DefaultExt = ".png",
      };
      bool? result = saveFileDialog.ShowDialog(this);
      if (result == true) {
        try {
          BitmapEncoder encoder = Tools.FindBitmapEncoder(saveFileDialog.FileName);
          if (encoder is JpegBitmapEncoder) {
            ((JpegBitmapEncoder)encoder).QualityLevel = 90;
          }
          encoder.Frames = new List<BitmapFrame>() { frame };
          using (FileStream output = new FileStream(saveFileDialog.FileName, FileMode.Create)) {
            encoder.Save(output);
            output.Flush();
          }
        }
        catch (Exception ex) {
          MessageBox.Show(String.Format("Saving {0}: {1}",
                                        saveFileDialog.FileName, ex.Message),
                          "Error saving image",
                          MessageBoxButton.OK,
                          MessageBoxImage.Error);
        }
      }
    }

    private void RecordToExecuted(object sender, ExecutedRoutedEventArgs e)
    {
      SaveFileDialog saveFileDialog = new SaveFileDialog()
      {
        DereferenceLinks = true,
        Title = "Record image image",
        Filter = "GIF files|*.gif",
        DefaultExt = ".gif",
        OverwritePrompt = false,
      };
      bool? result = saveFileDialog.ShowDialog(this);
      if (result == true) {
        RecordFilename = saveFileDialog.FileName;
        if (RecordContext == null) {
          InitializeRecording();
        }
      }
    }

    private void ExitExecuted(object sender, ExecutedRoutedEventArgs e)
    {
      Environment.Exit(0);
    }

    #endregion

    #region Setup

    private void Reset() {
      // If the worker is going, cancel it
      StopExecuted(null, null);
      if (RecordStream != null) {
        ShutdownRecording();
      }
      Arena = new DefectGrid(ArenaWidth, ArenaHeight, ArenaLevels, Neighbourhood);
      Output.Width = ArenaWidth * Scale;
      Output.Height = ArenaHeight * Scale;
      InitializeBitmap();
      InitializeColorData();
      InitializePalette();
      UpdateColorData();
      UpdateBitmap();
      Output.Source = Bitmap;
      Status.Text = "Ready";
      if (RecordFilename != null) {
        InitializeRecording();
      }
    }

    /// <summary>
    /// Initialize <code>Bitmap</code>
    /// </summary>
    private void InitializeBitmap()
    {
      // TODO - maybe there are better ways to choose a size?
      PresentationSource source = PresentationSource.FromVisual(this);
      double dpiX, dpiY;
      if (source != null) {
        dpiX = 96.0 * source.CompositionTarget.TransformToDevice.M11;
        dpiY = 96.0 * source.CompositionTarget.TransformToDevice.M22;
      }
      else {
        dpiX = dpiY = 96.0;
      }
      Bitmap = new WriteableBitmap(Arena.Width * Scale, Arena.Height * Scale, dpiX, dpiY, PixelFormats.Bgr32, null);
    }

    /// <summary>
    /// Initialize <code>ColorData</code>
    /// </summary>
    private void InitializeColorData()
    {
      ColorData = new uint[Arena.Width * Arena.Height * Scale * Scale];
    }

    /// <summary>
    /// Initialize <code>Palette</code>
    /// </summary>
    private void InitializePalette()
    {
      Palette = new uint[ArenaLevels];
      GIFPalette = new GIF.ColorTable()
      {
        Table = new System.Drawing.Color[ArenaLevels],
        BackgroundColorIndex = ArenaLevels,
      };
      for (int level = 0; level < ArenaLevels; ++level) {
        double h = 360.0 * level / ArenaLevels, s = 1.0, v = 1.0;
        double r, g, b;
        Tools.HsvToRgb(h, s, v, out r, out g, out b);
        int ri = (int)(255 * r);
        int gi = (int)(255 * g);
        int bi = (int)(255 * b);
        Palette[level] = (uint)(ri + 256 * gi + 65536 * bi) + 16777216u * 255u;
        GIFPalette.Table[level] = System.Drawing.Color.FromArgb(ri, gi, bi);
      }
    }

    private void InitializeRecording ()
    {
      string path = FindUniqueFilename();
      RecordStream = new FileStream(path, FileMode.Create); // TODO errors
      RecordContext = new GIF()
      {
        Output = RecordStream,
        ScreenWidth = Arena.Width * Scale,
        ScreenHeight = Arena.Height * Scale,
        GlobalColorTable = GIFPalette,
      };
      RecordPixels = new byte[Arena.Width * Arena.Height * Scale * Scale];
      RecordContext.Begin();
    }

    private void ShutdownRecording()
    {
      RecordContext.End();
      RecordStream.Flush(); // TODO errors
      RecordStream.Dispose();
      RecordStream = null;
      RecordContext = null;
    }

    private string FindUniqueFilename()
    {
      for (; ; ) {
        string path = string.Format("{0}{1}{2}",
                             System.IO.Path.GetFileNameWithoutExtension(RecordFilename),
                             RecordSequence,
                             System.IO.Path.GetExtension(RecordFilename));
        if (!File.Exists(path)) {
          return path;
        }
        ++RecordSequence;
      }
    }

    #endregion

    #region Updating

    /// <summary>
    /// Update ColorData from the current state of Arena
    /// </summary>
    private void UpdateColorData()
    {
      Arena.Render(ColorData, Palette, Scale);
    }

    /// <summary>
    /// Update Bitmap from the current state of ColorData
    /// </summary>
    /// <remarks><para>Must be called in the UI thread.</para></remarks>
    private void UpdateBitmap()
    {
      Bitmap.WritePixels(new Int32Rect(0, 0, Arena.Width * Scale, Arena.Height * Scale), ColorData, 4 * Arena.Width * Scale, 0);
    }

    #endregion

    #region Background thread

    private void Worker()
    {
      try {
        DateTime last = DateTime.UtcNow;
        TimeSpan perf = new TimeSpan();
        for (; ; ) {
          bool render = false;
          int changed = 0;
          double waitTime = 0;
          lock (Lock) {
            if (!Going) {
              break;
            }
            DateTime now = DateTime.UtcNow;
            waitTime = StepInterval - now.Subtract(last).TotalMilliseconds;
            if (waitTime <= 0) {
              changed = Arena.Step();
              UpdateColorData();
              perf = DateTime.UtcNow.Subtract(now);
              render = true;
              last = now;
              waitTime = StepInterval;
              if (RecordContext != null) {
                // TODO we could set all the pixels that haven't changed to transparent
                // and save a lot of space in early frames
                Arena.Render(RecordPixels, Scale); // TODO Scale is dubiously appropriate here
                RecordContext.WriteImage(new GIF.Image()
                {
                  ImageData = RecordPixels,
                  DelayCentiSeconds = 20,
                }); // TODO errors
              }
            }
          }
          if (render) {
            Dispatcher.InvokeAsync(() =>
            {
              Status.Text = string.Format("{0} cells changed; {1}ms", changed, perf.TotalMilliseconds);
              UpdateBitmap();
              if (changed == 0) {
                // Stuck!
                System.Media.SystemSounds.Exclamation.Play();
                StopExecuted(null, null);
                if (RecordContext != null) {
                  ShutdownRecording(); // TODO errors
                }
              }
            });
          }
          Thread.Sleep((int)Math.Min(waitTime, ResponseInterval));
        }
      }
      catch (TaskCanceledException) {
        // We get this if the process is terminated abruptly.
      }
    }

    #endregion

  }
}
