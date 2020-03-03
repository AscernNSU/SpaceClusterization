using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
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

namespace SpaceClusterization
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Initialize geo coordinates variables
        int numOfCoords = 0;
        double[,] xy = null;
        double[,] clusters = null;
        double r_threshold = 75;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void LoadImageClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Select a geographical image";
            openFileDialog.Filter = "All supported graphics|*.jpg;*.jpeg;*.png|" +
              "JPEG (*.jpg;*.jpeg)|*.jpg;*.jpeg|" +
              "Portable Network Graphic (*.png)|*.png";
            if (openFileDialog.ShowDialog() == true)
                GeoImage.Source = new BitmapImage(new Uri(openFileDialog.FileName));
        }

        private bool LoadCoords()
        {
            TextReader reader;
            String line;
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Select a geographical image";
            openFileDialog.Filter = "Text files (*.txt)|*.txt";
            bool parsing_flawless = true;

            if (openFileDialog.ShowDialog() == false)
            {
                MessageBox.Show("Error opening the dialog box!", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            reader = File.OpenText(openFileDialog.FileName);
            numOfCoords = Int32.Parse(reader.ReadLine());
            if (numOfCoords <= 0 && numOfCoords > 1000000)
                return false;
            xy = new double[numOfCoords, 2];
            for (int i = 0; (line = reader.ReadLine()) != null; i++)
            {
                string[] coords = line.Split(' ');
                if (!double.TryParse(coords[0], out xy[i, 0]) ||
                    !double.TryParse(coords[1], out xy[i, 1]))
                {
                    Console.WriteLine("Bad value at {0} coordinate!", i);
                    xy[i, 0] = xy[i, 1] = 0;
                    parsing_flawless = false;
                }
            }
            if (parsing_flawless)
                MessageBox.Show("Geographic coordinates loaded succesfully!",
                    "Loading complete", MessageBoxButton.OK, MessageBoxImage.Information);
            else
                MessageBox.Show("Geographic coordinates loaded but there are bad coordinate values, setting it to zero.",
                    "Warning!", MessageBoxButton.OK, MessageBoxImage.Warning);
            return true;
        }

        private void LoadCoordsClick(object sender, RoutedEventArgs e)
        {
            int num_of_clusters = Convert.ToInt32(RangeSlider.Value);

            if(LoadCoords() && (bool)ShowInitCoords.IsChecked)
                DisplayCoords();
        }

        private void DisplayCoords()
        {
            ImageGrid.Children.Clear();
            int xy_size = numOfCoords;
            Ellipse[] myEllipse = new Ellipse[xy_size];
            for (int i = 0; i < xy_size; i++)
            {
                myEllipse[i] = new Ellipse();
                myEllipse[i].Stroke = System.Windows.Media.Brushes.Black;
                myEllipse[i].Fill = System.Windows.Media.Brushes.DarkBlue;
                myEllipse[i].Opacity = 0.8;
                myEllipse[i].HorizontalAlignment = HorizontalAlignment.Left;
                myEllipse[i].VerticalAlignment = VerticalAlignment.Top;
                myEllipse[i].Width = myEllipse[i].Height = 10;
                myEllipse[i].Margin = new Thickness(xy[i, 0] - 5, xy[i, 1] - 5, 0, 0);
                ImageGrid.Children.Add(myEllipse[i]);
                //MessageBox.Show("Ellipse is drawn successfully!");
            }
        }

        private void ClusterizeClick(object sender, RoutedEventArgs e)
        {
            if (xy != null)
            {
                int numOfClusters = Convert.ToInt32(RangeSlider.Value);
                if (numOfClusters > numOfCoords)
                {
                    MessageBox.Show("The number of clusters cannot be more than the number of coordinates!",
                        "Warning!", MessageBoxButton.OK, MessageBoxImage.Warning);
                    numOfClusters = numOfCoords;
                    RangeSlider.Value = numOfClusters;
                }
                Clusterize(Convert.ToInt32(RangeSlider.Value), numOfCoords, xy);
                DisplayClusters();
            }
            else
                MessageBox.Show("Error: no coordinate info, please load coordinates first!",
                     "Warning!", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void Clusterize(int numOfClusters, int numOfCoords, double[,] coords)
        {
            clusters = new double[numOfClusters, 2];
            alglib.clusterizerstate s;
            alglib.kmeansreport rep;
            // Perform the clusterization algorithm via ALGLIB library
            alglib.clusterizercreate(out s);
            alglib.clusterizersetpoints(s, coords, 2);
            alglib.clusterizersetkmeanslimits(s, 5, 0);
            alglib.clusterizerrunkmeans(s, numOfClusters, out rep);
            clusters = rep.c;

            System.Console.WriteLine("Clusterization process result code: {0}", rep.terminationtype); // EXPECTED: 1
        }


        private void DisplayClusters()
        {
            ImageGrid.Children.Clear();
            if ((bool)ShowInitCoords.IsChecked)
                DisplayCoords();
            int xy_size = (clusters == null) ? 0 : clusters.GetLength(0);
            Ellipse[] clstrEllipse = new Ellipse[xy_size];
            for (int i = 0; i < xy_size; i++)
            {
                clstrEllipse[i] = new Ellipse();
                clstrEllipse[i].Stroke = System.Windows.Media.Brushes.Black;
                clstrEllipse[i].Fill = System.Windows.Media.Brushes.OrangeRed;
                clstrEllipse[i].Opacity = 0.5;
                clstrEllipse[i].HorizontalAlignment = HorizontalAlignment.Left;
                clstrEllipse[i].VerticalAlignment = VerticalAlignment.Top;
                clstrEllipse[i].Width = clstrEllipse[i].Height = 75;
                clstrEllipse[i].Margin = new Thickness(clusters[i, 0] - 37, clusters[i, 1] - 37, 0, 0);
                ImageGrid.Children.Add(clstrEllipse[i]);

                //MessageBox.Show("Ellipse is drawn successfully!");
            }

            if ((bool)ShowNumberOfCoords.IsChecked)
            {
                TextBlock[] nOfCoordsNearCluster = new TextBlock[xy_size];
                int NumOfCoordsNearClstr = 0;
                for (int i = 0; i < xy_size; i++)
                {
                    nOfCoordsNearCluster[i] = new TextBlock();
                    NumOfCoordsNearClstr = CalculateNumOfCoordsNearClstr(i);
                    if (NumOfCoordsNearClstr < 10)
                    {
                        nOfCoordsNearCluster[i].Text = NumOfCoordsNearClstr.ToString();
                        nOfCoordsNearCluster[i].Margin = new Thickness(clusters[i, 0] - 12, clusters[i, 1] - 33, 0, 0);
                        nOfCoordsNearCluster[i].FontSize = 48;
                    }
                    else
                    {
                        nOfCoordsNearCluster[i].Text = "9+";
                        nOfCoordsNearCluster[i].Margin = new Thickness(clusters[i, 0] - 18, clusters[i, 1] - 22, 0, 0);
                        nOfCoordsNearCluster[i].FontSize = 32;
                    }
                    ImageGrid.Children.Add(nOfCoordsNearCluster[i]);
                }
            }
        }

        double dist2(int nth_coord, int nth_clstr)
        {
            return (xy[nth_coord, 0] - clusters[nth_clstr, 0]) * (xy[nth_coord, 0] - clusters[nth_clstr, 0]) +
                        (xy[nth_coord, 1] - clusters[nth_clstr, 1]) * (xy[nth_coord, 1] - clusters[nth_clstr, 1]);
        }

        private int CalculateNumOfCoordsNearClstr(int n_of_current_cluster)
        {
            int NumOfCoords = 0;
            for (int i = 0; i < xy.GetLength(0); i++)
                if (Math.Sqrt(dist2(i, n_of_current_cluster)) < r_threshold)
                    NumOfCoords++;
            return NumOfCoords;
        }

        private void RangeSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            RangeSlider.Value = Math.Round(RangeSlider.Value); // Round the slider value
        }

        private void ShowInitCoordsClick(object sender, RoutedEventArgs e)
        {
            if ((bool)ShowInitCoords.IsChecked)
                DisplayCoords();
            // After checking the condition display clusters anyway
                DisplayClusters();
        }

        private void ShowNumberOfCoordsClick(object sender, RoutedEventArgs e)
        {
            DisplayClusters();
            RadiusSlider.IsEnabled = (bool)ShowNumberOfCoords.IsChecked;
        }

        private void RadiusSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            r_threshold = RadiusSlider.Value;
        }
    }
}
