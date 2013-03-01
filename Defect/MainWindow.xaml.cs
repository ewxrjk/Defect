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
    public MainWindow()
    {
      InitializeComponent();
      ArenaLevels = 16;
      ArenaWidth = 512;
      ArenaHeight = 512;
      Scale = 1;
      // Set the initial speed
      StepInterval = 500;
      SpeedSlider.Value = 1000 - StepInterval;
      Reset();  // draw an initial random image
      EnableDisable();
    }

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
    /// Display scale
    /// </summary>
    public int Scale { get; set; }

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

    private void New(object sender, RoutedEventArgs e)
    {
      Settings settings = new Settings()
      {
        Owner = this,
        ParentMainWindow = this,
      };
      settings.ShowDialog();
      switch(settings.Outcome) {
        case Defect.Settings.Outcomes.Cancelled:
          break;
        case Defect.Settings.Outcomes.Reset:
          Reset();
          break;
        case Defect.Settings.Outcomes.ResetAndGo:
          Reset();
          Go(null, null);
          break;
      }
    }

    private void Reset() {
      // If the worker is going, cancel it
      Stop(null, null);
      Arena = new DefectGrid(ArenaWidth, ArenaHeight, ArenaLevels);
      Output.Width = ArenaWidth * Scale;
      Output.Height = ArenaHeight * Scale;
      InitializeBitmap();
      InitializeColorData();
      InitializePalette();
      UpdateColorData();
      UpdateBitmap();
      Output.Source = Bitmap;
      Status.Text = "Use Edit > Go to begin";
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

    private void SaveAs(object sender, RoutedEventArgs e)
    {
      // TODO
    }

    private void Exit(object sender, RoutedEventArgs e)
    {
      this.Close();
    }

    private void Copy(object sender, RoutedEventArgs e)
    {
      // TODO
    }

    private void About(object sender, RoutedEventArgs e)
    {
      About about = new About()
      {
        Owner = this
      };
      about.ShowDialog();
    }

    private void Go(object sender, RoutedEventArgs e)
    {
      if (!Going) {
        Going = true;
        BackgroundThread = new Thread(new ThreadStart(this.Worker))
        {
          Name = "MainWindow.Worker"
        };
        BackgroundThread.Start();
        EnableDisable();
      }
    }

    private void Stop(object sender, RoutedEventArgs e)
    {
      if (Going) {
        lock (Lock) {
          Going = false;
        }
        BackgroundThread.Join();
        BackgroundThread = null;
        EnableDisable();
      }
    }

    private void EnableDisable()
    {
      GoButton.IsEnabled = GoMenuItem.IsEnabled = !Going;
      StopButton.IsEnabled = StopMenuItem.IsEnabled = Going;
    }

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
                Stop(null, null);
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

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
      Stop(null, null);
    }

    private void Restart(object sender, RoutedEventArgs e)
    {
      Reset();
    }

    private void NewSpeed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
      lock (Lock) {
        StepInterval = 1000 - e.NewValue;
      }
    }

    private void Settings(object sender, RoutedEventArgs e)
    {
      Settings settings = new Settings()
      {
        Owner = this,
        ParentMainWindow = this,
      };
      settings.CancelButton.Visibility = Visibility.Collapsed;
      settings.ShowDialog();
    }

  }
}