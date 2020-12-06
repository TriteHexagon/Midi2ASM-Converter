using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

namespace MIDI2ASMGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public string filePath = "";
        public MainWindow()
        {
            InitializeComponent();
        }

        private void LinkOnRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(e.Uri.ToString());
        }

        public void Button_SelectFile(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = Directory.GetCurrentDirectory();
            openFileDialog.Filter = "MIDI Files (*.midi; *.mid)|*.midi;*.mid";
            if (openFileDialog.ShowDialog() == true)
            {
                filePath = openFileDialog.FileName;
            }
        }

        public void Button_Convert(object sender, RoutedEventArgs e)
        {
            // sets up all options
            int BaseNotetypeLocation = 0;
            string[] Envelopes = new string[3];
            int[] Dutycycles = new int[2];
            bool OptionsOKFlag = true;
            string NotationStyle = "PCLegacy";
            //filename = "hello";

            //various settings test
            EnvelopeSetting(Envelopes, ref OptionsOKFlag);
            CheckDutycycles(Dutycycles, ref OptionsOKFlag);
            if (!Int32.TryParse(tbToggleNoise.Text, out int Togglenoise) || Togglenoise < 1)
            {
                MessageBox.Show("Drumkit is invalid!");
                OptionsOKFlag = false;
            }
            if (filePath == "")
            {
                MessageBox.Show("No MIDI file selected!");
                OptionsOKFlag = false;
            }

            if (!OptionsOKFlag)
            {
                goto Error;
            }          

            bool[] GUIOptions = new bool[7];
            GUIOptions[0] = (cbNoiseTemplate.IsChecked == true);
            GUIOptions[1] = (cbTempoTrack.IsChecked == true);
            GUIOptions[2] = (cbWarnings.IsChecked == true);
            GUIOptions[3] = (cbAutoSync.IsChecked == true);
            GUIOptions[4] = (cbIgnoreRests.IsChecked == true);
            GUIOptions[5] = (cbCapitalizeHexadecimal.IsChecked == true);
            GUIOptions[6] = (cbASMName.IsChecked == true);
            if (rbPCNew.IsChecked == true)
            {
                NotationStyle = "PCNew";
            }
            else if (rbPRPY.IsChecked == true)
            {
                NotationStyle = "PRPY";
            }

            //auxiliar stuff
            int[] allNotetypes = { 12, 8, 6, 4, 3 };
            List<int> allowedNotetypesTemp = new List<int>();
            bool?[] allowedNotetypesBool = { cbNotetype12.IsChecked, cbNotetype8.IsChecked, cbNotetype6.IsChecked, cbNotetype4.IsChecked, cbNotetype3.IsChecked };

            //makes sure one and only one notetype is checked
            int allowedNotetypesTrueCount = 0;
            foreach (bool? ATrue in allowedNotetypesBool)
            {
                if (ATrue == true)
                {
                    allowedNotetypesTrueCount++;
                }          
            }
            Trace.WriteLine(allowedNotetypesTrueCount);
            if (allowedNotetypesTrueCount==0)
            {
                MessageBox.Show("One notetype needs to be checked!");
                goto Error;
            }
            if (allowedNotetypesTrueCount > 1)
            {
                MessageBox.Show("Only one notetype can be checked (base). Use third state for other allowed notetypes.");
                goto Error;
            }

            int j = 0;
            for (int i=0; i<allNotetypes.Length; i++)
            {
                if (allowedNotetypesBool[i] == true)
                {
                    BaseNotetypeLocation = j;
                }
                if (allowedNotetypesBool[i] == null || allowedNotetypesBool[i] == true)
                {
                    j++;
                    allowedNotetypesTemp.Add(allNotetypes[i]);
                }
            }
            int[] allowedNotetypes = allowedNotetypesTemp.ToArray();

            new Program(BaseNotetypeLocation, allowedNotetypes, GUIOptions, Envelopes, Togglenoise, Dutycycles, filePath, NotationStyle);

            Error:;
        }

        public void EnvelopeSetting(string[] Envelopes, ref bool OptionsOKFlag)
        {
            if (!uint.TryParse(tbP1BaseEnvelope.Text, System.Globalization.NumberStyles.HexNumber, null, out uint Pulse1BaseEnvelope))
            {
                MessageBox.Show("Pulse 1 envelope is not an hexadecimal number.");
                OptionsOKFlag = false;
            }
            if (!uint.TryParse(tbP2BaseEnvelope.Text, System.Globalization.NumberStyles.HexNumber, null, out uint Pulse2BaseEnvelope))
            {
                MessageBox.Show("Pulse 2 envelope is not an hexadecimal number.");
                OptionsOKFlag = false;
            }
            if (!uint.TryParse(tbWaveBaseEnvelope.Text, System.Globalization.NumberStyles.HexNumber, null, out uint WaveBaseEnvelope))
            {
                MessageBox.Show("Wave envelope is not an hexadecimal number.");
                OptionsOKFlag = false;
            }

            if(Pulse1BaseEnvelope > 0xf)
            {
                MessageBox.Show("Pulse 1 envelope is greater than f.");
                OptionsOKFlag = false;
            }
            if (Pulse2BaseEnvelope > 0xf)
            {
                MessageBox.Show("Pulse 2 envelope is greater than f.");
                OptionsOKFlag = false;
            }
            if (WaveBaseEnvelope > 0xf)
            {
                MessageBox.Show("Waveform is greater than f.");
                OptionsOKFlag = false;
            }

            if(OptionsOKFlag)
            {
                if (cbCapitalizeHexadecimal.IsChecked==true)
                {
                    Envelopes[0] = Pulse1BaseEnvelope.ToString("X");
                    Envelopes[1] = Pulse2BaseEnvelope.ToString("X");
                    Envelopes[2] = WaveBaseEnvelope.ToString("X");
                }
                else
                {
                    Envelopes[0] = Pulse1BaseEnvelope.ToString("x");
                    Envelopes[1] = Pulse2BaseEnvelope.ToString("x");
                    Envelopes[2] = WaveBaseEnvelope.ToString("x");
                }
            }
        }

        public void CheckDutycycles(int[] Dutycycles, ref bool OptionsOKFlag)
        {
            if (!Int32.TryParse(tbP1Dutycycle.Text, out Dutycycles[0]) || Dutycycles[0] < 0 || Dutycycles[0] > 3)
            {
                MessageBox.Show("Pulse 1's Dutycycle is invalid (has to be a number between 0 and 3).");
                OptionsOKFlag = false;
            }
            if (!Int32.TryParse(tbP2Dutycycle.Text, out Dutycycles[1]) || Dutycycles[1] < 0 || Dutycycles[1] > 3)
            {
                MessageBox.Show("Pulse 2's Dutycycle is invalid (has to be a number between 0 and 3).");
                OptionsOKFlag = false;
            }
        }
    }
}
