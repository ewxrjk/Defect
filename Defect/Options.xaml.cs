// This program is © 2013 Richard Kettlewell.
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY// without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Defect
{
  /// <summary>
  /// Interaction logic for Options.xaml
  /// </summary>
  public partial class Options : Window
  {
    public Options()
    {
      InitializeComponent();
      Outcome = Outcomes.Cancelled;
      foreach (string name in Enum.GetNames(typeof(CellNeighbourhood))) {
        object icon = this.Resources[string.Format("CellNeighbourhood.{0}", name)];
        StackPanel stackPanel = new StackPanel() { Orientation = Orientation.Horizontal };
        stackPanel.Children.Add(icon as UIElement);
        stackPanel.Children.Add(new Label() { Content = name });
        ComboBoxItem comboBoxItem = new ComboBoxItem() { Content = stackPanel, Name = name };
        EnterNeighbourhood.Items.Add(comboBoxItem);
      }
    }

    #region Interface

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

    #endregion

    #region Internal State

    private MainWindow _ParentMainWindow;

    private uint invalidcontrols = 0;

    #endregion

    #region Values

    private void Configure()
    {
      EnterWidth.Text = ParentMainWindow.ArenaWidth.ToString();
      EnterHeight.Text = ParentMainWindow.ArenaHeight.ToString();
      EnterStates.Text = ParentMainWindow.ArenaLevels.ToString();
      EnterNeighbourhood.SelectedItem = EnterNeighbourhood.Items.Cast<ComboBoxItem>().First(item => item.Name == ParentMainWindow.Neighbourhood.ToString());
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

    private void Neighbourhood_Changed(object sender, SelectionChangedEventArgs e)
    {
      ParentMainWindow.Neighbourhood = (CellNeighbourhood)Enum.Parse(typeof(CellNeighbourhood), ((ComboBoxItem)EnterNeighbourhood.SelectedItem).Name);
    }

    #endregion

    #region Outcomes

    private void OK(object sender, RoutedEventArgs e)
    {
      Outcome = Outcomes.Reset;
      this.Close();
    }

    private void Cancel(object sender, RoutedEventArgs e)
    {
      this.Close();
    }
    
    #endregion

  }
}
