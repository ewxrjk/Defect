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
using System.Threading;

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
        ArenaWidth = 512;
        ArenaHeight = 512;
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

    #endregion

    #region Window Furniture

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
      StopExecuted(null, null);
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
    }

    public static RoutedCommand GoCommand = new RoutedCommand();

    public static RoutedCommand StopGoCommand = new RoutedCommand();

    public static RoutedCommand ResetCommand = new RoutedCommand();

    public static RoutedCommand OptionsCommand = new RoutedCommand();

    public static RoutedCommand ExitCommand = new RoutedCommand();

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

    private void ExitExecuted(object sender, ExecutedRoutedEventArgs e)
    {
      Environment.Exit(0);
    }

    #endregion

    #region Setup

    private void Reset() {
      // If the worker is going, cancel it
      StopExecuted(null, null);
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
      for (int level = 0; level < ArenaLevels; ++level) {
        double h = 360.0 * level / ArenaLevels, s = 1.0, v = 1.0;
        double r, g, b;
        Tools.HsvToRgb(h, s, v, out r, out g, out b);
        uint ri = (uint)(255 * r);
        uint gi = (uint)(255 * g);
        uint bi = (uint)(255 * b);
        Palette[level] = ri + 256u * gi + 65536u * bi + 16777216u * 255u;
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