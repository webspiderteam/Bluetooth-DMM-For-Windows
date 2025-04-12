using Microsoft.Win32;
using Octokit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml.Linq;
using WPFLocalizeExtension.Deprecated.Extensions;
using WPFLocalizeExtension.Engine;
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;

namespace BluetoothDMM
{
    /// <summary>
    /// Interaction logic for SettingsNew.xaml
    /// </summary>
    public partial class SettingsNew : Window
    {
        public Dictionary<string, DeviceProps> DeviceListC { get; set; }
        public IEnumerable<ColorInfo> color_query { get; set; }

        // The path to the key where Windows looks for startup applications
        readonly RegistryKey rkApp = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
        bool topmost;
        public SettingsNew(System.Collections.Generic.Dictionary<string, DeviceProps> deviceListC)
        {
            InitializeComponent();
            DeviceListC = deviceListC;
            lstDevices.DataContext = this;
            color_query = from PropertyInfo property in typeof(Colors).GetProperties()
                              orderby property.Name
                              //orderby ((Color)property.GetValue(null, null)).ToString()
                              select new ColorInfo(
                                      property.Name,
                                      (Color)property.GetValue(null, null));
            cmbColors.DataContext = this;// (new System.Linq.SystemCore_EnumerableDebugView<BluetoothDMM.ColorInfo>(color_query).Items[12]).HexValue
            //cmbColors.SelectedValue = color_query.FirstOrDefault( Properties.Settings.Default.ADisplayBarColor;
            //cmbColors.ItemsSource = color_query;
            //cmbColors.SelectedValue = Properties.Settings.Default.ADisplayBarColor;

            LocalizeDictionary.Instance.OutputMissingKeys = true;
            LocalizeDictionary.Instance.MissingKeyEvent += Instance_MissingKeyEvent;
            this.Closing += SettingsNew_Closing;
            this.Activated += SettingsNew_Activated;
            topmost = Topmost;
            txtPassword.Password = Properties.MQTT.Default.Password;
#if DEBUG
            vertest.Visibility = Visibility.Visible;
#endif
            if (rkApp.GetValue("BluetoothDMM") == null)
            {
                // The value doesn't exist, the application is not set to run at startup
                chkWStartup.IsChecked = false;
            }
            else
            {
                // The value exists, the application is set to run at startup
                chkWStartup.IsChecked = true;
            }
            if (Updates.Releases != null)
                CheckUpdate(Updates.Releases);
        }
        private void Instance_MissingKeyEvent(object sender, MissingKeyEventArgs e)
        {
            e.MissingKeyResult = "Hello World";
        }
        private void SettingsNew_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.Reload();
            Properties.MQTT.Default.Reload();
        }

        private void SettingsNew_Activated(object sender, EventArgs e)
        {
            this.Topmost = topmost;
        }

        private void Hl_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            topmost = this.Topmost;
            this.Topmost = false;
            browser.Source=e.Uri;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (DialogResult==null)
                DialogResult = false;
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            CheckUpdate();
        }
        private async void CheckUpdate(IReadOnlyList<Release> Releases=null)
        {
            var _prefix = "WPFRelease";
            var _suffix = "";
            pnlBusy.Visibility = Visibility.Visible;
            btnCheckUpdates.IsEnabled=false;
            boxChangeLog.Visibility = Visibility.Collapsed;
            boxLatest.Visibility=Visibility.Collapsed;
            try
            {
                IReadOnlyList<Release> releases;
                if (Releases != null)
                    releases = Releases;
                else
                {
                    GitHubClient client = new GitHubClient(new ProductHeaderValue("BluetoothDMMForWindows"));
                    releases = await client.Repository.Release.GetAll("webspiderteam", "Bluetooth-DMM-For-Windows");
                }
                //Setup the versions
                string tagname=null;
                int LatestReleaseIndex = 0;
                txtLinks.Inlines.Clear();
                txtCurrentVersion.Inlines.Clear();
                pnlLinks.Visibility = Visibility.Hidden;
                foreach (Release release in releases)
                {
                    tagname = Trim_Tag(release.TagName, _prefix, _suffix);
                    if(tagname != null)
                        break;
                    LatestReleaseIndex++;
                }
                Version latestGitHubVersion = new Version(tagname);
                Version localVersion = new Version(Get_Version()); //Local version. 
                if ((bool)test.IsChecked)
                    localVersion = new Version($"{v1.Text}.{v2.Text}.{v3.Text}"); //Only for testing

                //Compare the Versions
                int versionComparison = localVersion.CompareTo(latestGitHubVersion);
                boxLatest.Visibility=Visibility.Visible;
                if (versionComparison < 0)
                {
                    for (var i = 0; i < releases[LatestReleaseIndex].Assets.Count; i++)
                    {
                        Hyperlink hl = new Hyperlink();
                        hl.NavigateUri = new Uri(releases[LatestReleaseIndex].Assets[i].BrowserDownloadUrl);
                        hl.Inlines.Add(new Run(Trim_Tag(releases[LatestReleaseIndex].Assets[i].Name, "BluetoothDMMForWindows.", _release: false)));
                        hl.RequestNavigate += Hl_RequestNavigate;
                        txtLinks.Inlines.Add(hl);
                        txtLinks.Inlines.Add(new Run("  "));
                    }
                    pnlLinks.Visibility = Visibility.Visible;
                    txtLatest.Text = "";
                    Get_Detailed_ReleaseInfo(releases[LatestReleaseIndex], txtLatest);
                    txtChangeLog.Text = "";
                    int count = 0;
                    foreach (var release in releases)
                    {
                        var releaseversion = Trim_Tag(release.TagName, _prefix, _suffix);
                        if (releaseversion != null)
                        {
                            Version releaseVersion = new Version(releaseversion);
                            int releaseComparison = localVersion.CompareTo(releaseVersion);
                            if (releaseComparison < 0)
                            {
                                if (count > 0)
                                    txtChangeLog.Inlines.Add("\r\n\r\n");
                                Get_Detailed_ReleaseInfo(release, txtChangeLog);
                                count++;
                            }
                            else
                            {
                                var current = new Run();
                                new WPFLocalizeExtension.Extensions.LocExtension("CurrentVersion").SetBinding(current, Run.TextProperty);
                                txtCurrentVersion.Inlines.Add(new Run("("));
                                txtCurrentVersion.Inlines.Add(current);
                                txtCurrentVersion.Inlines.Add(new Run($" : {release.Name})"));
                                break;
                            }
                        }
                    }
                    boxChangeLog.Visibility = count > 1 ? Visibility.Visible : Visibility.Collapsed;
                }
                else
                    new WPFLocalizeExtension.Extensions.LocExtension("NoUpdatesAvail").SetBinding(txtLatest, TextBlock.TextProperty);
                Console.WriteLine(releases);
            }
            catch (Exception ex)
            {
                new WPFLocalizeExtension.Extensions.LocExtension("ErrorGettingData").SetBinding(txtLatest, TextBlock.TextProperty);
                Debug.WriteLine("Github Error" + ex);
            }
            boxLatest.Visibility = Visibility.Visible;
            pnlBusy.Visibility = Visibility.Hidden;
            btnCheckUpdates.IsEnabled=true;
        }
        private void Get_Detailed_ReleaseInfo(Release Rel, TextBlock textBlock)
        {
            textBlock.Inlines.Add(new Run($"[{Rel.TagName}] {Rel.Name} (Publish Date: {Rel.PublishedAt.Value.LocalDateTime}) {(Rel.Prerelease ? "(PreRelease)" : "")}") { FontWeight = FontWeights.Bold });
            string details = "";
            details += "\r\n";
            try
            {
                var Features = Get_Features(Rel.Body);
                if (Features == null)
                    details += Rel.Body;
                else
                    details += FeatureString(Features.Select(x => "• " + x), "\r\n");
                textBlock.Inlines.Add(new Run(details));
            }
            catch (Exception ex)
            { Debug.WriteLine("xxx " + ex); }
        }
        public List<string> Get_Features(string LongDesc)
        {
            string[] lines = LongDesc.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            List<string> feats = new List<string>();
            foreach (string line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("- ") || trimmed.StartsWith("* "))
                    feats.Add(trimmed.Substring(2).Trim());
            }
            if (feats.Count > 0)
                return feats;
            else
                return null;
        }
        private static string FeatureString<T>(IEnumerable<T> Vals, string Between)
        {
            string all = "";
            foreach (var val in Vals)
            {
                if (all != "")
                    all += Between;
                all += val;
            }
            return all;
        }
        private static string Get_Version()
        {
            Assembly assembly = Assembly.GetEntryAssembly();
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            string version_ = fileVersionInfo.ProductVersion;
            return version_;
        }
        private string Trim_Tag(string tag,string _prefix="", string _suffix="",bool _release=true)
        {
            try
            {
                var tagname = tag;
                if (_prefix != "" && tagname.StartsWith(_prefix))
                {
                    tagname = tagname.Replace(_prefix, "");
                    Regex rgx = new Regex(@"(?<![*])([0-9+]*\.[0-9+]*[\.]?[0-9]*)");
                    if (_release && rgx.IsMatch(tagname))
                        tagname = rgx.Match(tagname).ToString();
                }
                else
                    return null;
                if (_suffix != "" && tagname.EndsWith(_suffix))
                    tagname = tagname.Replace(_suffix, ""); //(?<![*])([0-9+]*\.[0-9+]*[\.]?[0-9]*)

                return tagname;
            }
            catch { return ""; }
        }
        private void btnRemove_Click(object sender, RoutedEventArgs e)
        {
            if (lstDevices.SelectedItems.Count > 0)
            {
                DeviceListC.Remove(((KeyValuePair<string, DeviceProps>)lstDevices.SelectedValue).Key);
                lstDevices.SelectedIndex = -1;
                lstDevices.Items.Refresh();
            }
        }

        private void btnRemoveAll_Click(object sender, RoutedEventArgs e)
        {
            DeviceListC.Clear();
            lstDevices.SelectedIndex = -1;
            lstDevices.Items.Refresh();
           
        }

        private void btnChange_Click(object sender, RoutedEventArgs e)
        {
            if (lstDevices.SelectedItems.Count > 0)
            {
                var a = ((KeyValuePair<string, DeviceProps>)lstDevices.SelectedValue).Value.Name;
                EditBox.Text = a;
                var b = ((KeyValuePair<string, DeviceProps>)lstDevices.SelectedValue).Value.Type;
                EditDevTypeCmb.SelectedIndex = b;
                Key.Text = ((KeyValuePair<string, DeviceProps>)lstDevices.SelectedValue).Key;
                Renamer.IsOpen = true;
            }
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            if (EditBox.Text.Length > 0)
            {
                DeviceListC[Key.Text].Name = EditBox.Text;
                DeviceListC[Key.Text].Type = EditDevTypeCmb.SelectedIndex;
                Key.Text = "";
                lstDevices.Items.Refresh();
                Renamer.IsOpen = false;
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)chkWStartup.IsChecked)
            {
                // Add the value in the registry so that the application runs at startup
                rkApp.SetValue("BluetoothDMM", System.Reflection.Assembly.GetExecutingAssembly().Location + " -m");
            }
            else
            {
                // Remove the value from the registry so that the application doesn't start
                rkApp.DeleteValue("BluetoothDMM", false);
            }
            if (Properties.Settings.Default.ConnectOn)
            {
                Properties.Settings.Default.DeviceID.Clear();
                foreach (KeyValuePair<string, DeviceProps> items in DeviceListC)
                {
                    Properties.Settings.Default.DeviceID.Add(items.Key + "\n" + items.Value.Name + "|" + items.Value.Type.ToString());
                }
            }
            Properties.MQTT.Default.Password = txtPassword.Password;
            Properties.Settings.Default.Lang = ((System.Globalization.CultureInfo)cmbLanguage.SelectedItem).Name;
            Properties.Settings.Default.Save();
            Properties.MQTT.Default.Save();
            DialogResult = true;
        }

        private void btnValues_Click(object sender, RoutedEventArgs e)
        {
            var datatypes = new MQTTDataFormat();
            datatypes.Topmost = this.Topmost;
            datatypes.ShowDialog();
        }

    }
    public class ColorInfo
    {
        public string ColorName { get; set; }
        public Color Color { get; set; }

        public SolidColorBrush SampleBrush
        {
            get { return new SolidColorBrush(Color); }
        }
        public string HexValue
        {
            get { return Color.ToString(); }
        }

        public ColorInfo(string color_name, Color color)
        {
            ColorName = color_name;
            Color = color;
        }
        public override string ToString()
        {
            return Color.ToString();
        }
    }
}
