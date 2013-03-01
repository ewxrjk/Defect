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
using System.Windows.Shapes;

namespace Defect
{
  /// <summary>
  /// Interaction logic for Settings.xaml
  /// </summary>
  public partial class Settings : Window
  {
    public Settings()
    {
      InitializeComponent();
      Outcome = Outcomes.Cancelled;
    }

    /// <summary>
    /// Parent window
    /// </summary>
    /// <remarks>This window's configuration will be modified.</remarks>
    public MainWindow ParentMainWindow
    {
      get
      {
        return _ParentMainWindow;
      }
      set
      {
        _ParentMainWindow = value;
        Configure();
      }
    }

    /// <summary>
    /// Possible outcomes
    /// </summary>
    public enum Outcomes
    {
      /// <summary>
      /// Operation was cancelled
      /// </summary>
      Cancelled,

      /// <summary>
      /// Reset automaton
      /// </summary>
      Reset,

      /// <summary>
      /// Reset automaton and start it
      /// </summary>
      ResetAndGo
    };

    /// <summary>
    /// Outcome of the settings change
    /// </summary>
    public Outcomes Outcome { get; private set; }

    private MainWindow _ParentMainWindow;

    private uint invalidcontrols = 0;

    private void Configure()
    {
      EnterWidth.Text = ParentMainWindow.ArenaWidth.ToString();
      EnterHeight.Text = ParentMainWindow.ArenaHeight.ToString();
      EnterStates.Text = ParentMainWindow.ArenaLevels.ToString();
    }

    private void OK(object sender, RoutedEventArgs e)
    {
      Outcome = Outcomes.Reset;
      this.Close();
    }

    private void Changed(TextBox inputTextBlock, int min, int max, Label errorLabel, Action<int> setter, uint controlbit)
    {
      string fault = null;
      int value;
      if (int.TryParse(inputTextBlock.Text, out value)) {
        if (value >= min && value <= max) {
          errorLabel.Visibility = Visibility.Hidden;
          setter(value);
          invalidcontrols &= ~controlbit;
          OKButton.IsEnabled = (invalidcontrols == 0);
          return;
        }
        if (value < min) {
          fault = string.Format("minimum {0}", min);
        }
        else {
          fault = string.Format("maximum {0}", max);
        }
      }
      else {
        fault = "integer required";
      }
      errorLabel.Content = fault;
      errorLabel.Visibility = Visibility.Visible;
      invalidcontrols |= controlbit;
      OKButton.IsEnabled = false;
    }

    private void Width_Changed(object sender, TextChangedEventArgs e)
    {
      Changed(EnterWidth, 16, 32768, EnterWidthError, (int value) => { ParentMainWindow.ArenaWidth = value; }, 1);
    }

    private void Height_Changed(object sender, TextChangedEventArgs e)
    {
      Changed(EnterHeight, 16, 32768, EnterHeightError, (int value) => { ParentMainWindow.ArenaHeight = value; }, 2);
    }

    private void States_Changed(object sender, TextChangedEventArgs e)
    {
      Changed(EnterStates, 2, 256, EnterStatesError, (int value) => { ParentMainWindow.ArenaLevels = value; }, 4);
    }

    private void Cancel(object sender, RoutedEventArgs e)
    {
      this.Close();
    }
  }
}
