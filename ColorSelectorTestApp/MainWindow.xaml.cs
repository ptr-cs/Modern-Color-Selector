using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace ColorSelectorTestApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            List<Color> presetColors = new()
            {
                (Color)ColorConverter.ConvertFromString("#FB0000"),
                (Color)ColorConverter.ConvertFromString("#FFA019"),
                (Color)ColorConverter.ConvertFromString("#FED440"),
                (Color)ColorConverter.ConvertFromString("#C5EC41"),
                (Color)ColorConverter.ConvertFromString("#00C0FF"),
                (Color)ColorConverter.ConvertFromString("#BC0100"),
                (Color)ColorConverter.ConvertFromString("#FC7C00"),
                (Color)ColorConverter.ConvertFromString("#FFC000"),
                (Color)ColorConverter.ConvertFromString("#6DC400"),
                (Color)ColorConverter.ConvertFromString("#0597F2"),
                (Color)ColorConverter.ConvertFromString("#0044A4"),
                (Color)ColorConverter.ConvertFromString("#B119E0"),
                (Color)ColorConverter.ConvertFromString("#F7F9F8"),
                (Color)ColorConverter.ConvertFromString("#A3AABA"),
                (Color)ColorConverter.ConvertFromString("#222222"),
                (Color)ColorConverter.ConvertFromString("#03297E"),
                (Color)ColorConverter.ConvertFromString("#7000BC"),
                (Color)ColorConverter.ConvertFromString("#CDD2D6"),
                (Color)ColorConverter.ConvertFromString("#888DA0"),
                (Color)ColorConverter.ConvertFromString("#131313")
            };
            presetColors.ForEach(x => colorSelector.PresetColors.Add(x));
        }

        private void ColorSelector_ColorSelected(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine($"Selected: {colorSelector.SelectedColor}");
        }

        private void ColorSelector_CurrentColorChanged(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine($"Changed: {colorSelector.CurrentColor}");
        }

        private void ColorSelector_CustomColorSaved(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine($"Saved: {colorSelector.CustomColors.First()}");
        }
    }
}
