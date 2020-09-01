using Solo_Public_Lobby.DataAccess;
using Solo_Public_Lobby.Helpers;
using Solo_Public_Lobby.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Newtonsoft.Json;

namespace Solo_Public_Lobby
{
    public class Game : INotifyPropertyChanged
    {
        Game()
        {
            active  = false;
            created = false;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        // Interface
        public string GameName { get; set; }
        public string udpPorts { get; set; }
        public string tcpPorts { get; set; }
        public bool active { get { return _active; } set { _active = value; OnPropertyChanged("Enabled"); } }
        public bool created { get; set; }

        public string Enabled
        {
            get
            {
                return active ? "✔" : "❌";
            }
        }

        public string GetTCPRuleName(string szDirection)
        {
            return this.GameName + " - Private Public Lobby TCP::" + szDirection;
        }

        public string GetUDPRuleName(string szDirection)
        {
            return this.GameName + " - Private Public Lobby UDP::" + szDirection;
        }

        // Events
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        //  private functions
        private bool _active { get; set; }

    }
    public partial class MainWindow : Window
    {
        private static string config = @"games.json";

        private IPTool iPTool = new IPTool();
        private DaWhitelist whiteList = new DaWhitelist();
        private List<IPAddress> addresses = new List<IPAddress>();
        private MWhitelist mWhitelist = new MWhitelist();
        private bool bInitComplete = false;

        public ObservableCollection<Game> games
        {
            get { return _games; }
            set
            {
                if (_games != value)
                {
                    _games = value;
                }
            }
        }

        private ObservableCollection<Game> _games { get; set; }

        public MainWindow()
        {
            InitializeComponent();      
            Loaded += MainWindow_Loaded;

            games = new ObservableCollection<Game>();

            // Read games info.
            if( ! File.Exists(config) )
            {
                // create it.
                createConfig(config);
            }

            readConfig(config);

            DataContext = this;
        }

        private void createConfig(string config)
        {
            File.WriteAllText(config, "[{\n   'GameName': 'Destiny 2',\n   'udpPorts': '1119-1120,3097-3196,3724,4000,6112-6114,27015-27200',\n   'tcpPorts': '3074,3724,4000,6112-6114'\n},\n{\n    'GameName': 'GTA V Online',\n    'udpPorts': '6672,61455-61458',\n    'tcpPorts': ''\n}]");
        }

        private void readConfig(string config)
        {
            var jsonGamesString = File.ReadAllText(config);

            JsonTextReader reader = new JsonTextReader(new StringReader(jsonGamesString));
            reader.SupportMultipleContent = true;

            while (true)
            {
                if (!reader.Read())
                {
                    break;
                }

                JsonSerializer serializer = new JsonSerializer();
                games = serializer.Deserialize<ObservableCollection<Game>>(reader);
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            FirewallRule.lblAdmin = lblAdmin;
            Init();
        }

        void Init()
        {
            lblYourIPAddress.Content += " " + iPTool.IpAddress + ".";
            addresses = DaWhitelist.ReadIPsFromJSON();
            lsbAddresses.ItemsSource = addresses;
            foreach (IPAddress ip in addresses)
            {
                mWhitelist.Ips.Add(ip.ToString());
            }
            SetIpCount();

            bInitComplete = true;
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            if(IPTool.ValidateIP(txbIpToAdd.Text))
            {
                if(!addresses.Contains(IPAddress.Parse(txbIpToAdd.Text)))
                {
                    addresses.Add(IPAddress.Parse(txbIpToAdd.Text));
                    lsbAddresses.Items.Refresh();
                    mWhitelist.Ips.Add(txbIpToAdd.Text);
                    DaWhitelist.SaveToJson(mWhitelist);
                    getSelectedGame().created = false; getSelectedGame().active = false;
                    FirewallRule.DeleteRules( getSelectedGame() );
                    SetIpCount();
                    UpdateActive();
                }
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if(lsbAddresses.SelectedIndex != -1)
            {
                mWhitelist.Ips.Remove(lsbAddresses.SelectedItem.ToString());
                addresses.Remove(IPAddress.Parse(lsbAddresses.SelectedItem.ToString()));
                lsbAddresses.Items.Refresh();
                DaWhitelist.SaveToJson(mWhitelist);
                getSelectedGame().created = false; getSelectedGame().active = false;
                FirewallRule.DeleteRules( getSelectedGame() );
                SetIpCount();
                UpdateActive();
            }
        }

        private Game getSelectedGame()
        {
            return gameGrid.HasItems ? games[gameGrid.SelectedIndex] : null;
        }

        private void SetIpCount()
        {
            lblAmountIPs.Content = addresses.Count() + " IPs whitelisted!";
        }

        private void btnEnableDisable_Click(object sender, RoutedEventArgs e)
        {
            SetRules();
        }

        void SetRules()
        {
            string remoteAddresses = RangeCalculator.GetRemoteAddresses(addresses);

            // If the firewall rules aren't set yet.
            if (!getSelectedGame().created)
            {
                if (FirewallRule.CreateInbound(remoteAddresses, this.getSelectedGame(), true, false) &&
                    FirewallRule.CreateOutbound(remoteAddresses, this.getSelectedGame(), true, false))
                {
                    getSelectedGame().active = true;
                    getSelectedGame().created = true;
                    UpdateActive();
                }
                return;
            }

            // If they are set but not enabled.
            if (getSelectedGame().created && !getSelectedGame().active)
            {
                if (FirewallRule.CreateInbound(remoteAddresses, this.getSelectedGame(), true, true) &&
                    FirewallRule.CreateOutbound(remoteAddresses, this.getSelectedGame(), true, true))
                {
                    getSelectedGame().active = true;
                    UpdateActive();
                }
                return;
            }

            // If they are active and set.
            if(getSelectedGame().created && getSelectedGame().active)
            {
                if (FirewallRule.CreateInbound(remoteAddresses, this.getSelectedGame(), false, true) &&
                    FirewallRule.CreateOutbound(remoteAddresses, this.getSelectedGame(), false, true))
                {
                    getSelectedGame().active = false;
                    UpdateActive();
                }
                return;
            }
        }

        void UpdateActive()
        {
            if (!bInitComplete) return;

            if (getSelectedGame().active)
            {
                btnEnableDisable.Background = ColorBrush.Green;
                image4.Source = new BitmapImage(new Uri("/CodeSwine-Solo_Public_Lobby;component/ImageResources/locked.png", UriKind.Relative));
                lblLock.Content = "Rules active." + Environment.NewLine + "Click to deactivate!";
            }
            else
            {
                btnEnableDisable.Background = ColorBrush.Red;
                image4.Source = new BitmapImage(new Uri("/CodeSwine-Solo_Public_Lobby;component/ImageResources/unlocked.png", UriKind.Relative));
                lblLock.Content = "Rules not active." + Environment.NewLine + "Click to activate!";
            }
        }

        [DllImport("User32.dll")]
            private static extern bool RegisterHotKey(
        [In] IntPtr hWnd,
        [In] int id,
        [In] uint fsModifiers,
        [In] uint vk);

        [DllImport("User32.dll")]
        private static extern bool UnregisterHotKey(
            [In] IntPtr hWnd,
            [In] int id);

        private HwndSource _source;
        private const int HOTKEY_ID = 9000;

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var helper = new WindowInteropHelper(this);
            _source = HwndSource.FromHwnd(helper.Handle);
            _source.AddHook(HwndHook);
            RegisterHotKey();
        }

        protected override void OnClosed(EventArgs e)
        {
            _source.RemoveHook(HwndHook);
            _source = null;
            UnregisterHotKey();
            foreach (Game g in games)
            {
                FirewallRule.DeleteRules(g);
            }
            base.OnClosed(e);
        }

        private void RegisterHotKey()
        {
            var helper = new WindowInteropHelper(this);
            const uint VK_F10 = 0x79;
            const uint MOD_CTRL = 0x0002;
            if (!RegisterHotKey(helper.Handle, HOTKEY_ID, MOD_CTRL, VK_F10))
            {
                
            }
        }

        private void UnregisterHotKey()
        {
            var helper = new WindowInteropHelper(this);
            UnregisterHotKey(helper.Handle, HOTKEY_ID);
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            switch (msg)
            {
                case WM_HOTKEY:
                    switch (wParam.ToInt32())
                    {
                        case HOTKEY_ID:
                            OnHotKeyPressed();
                            handled = true;
                            break;
                    }
                    break;
            }
            return IntPtr.Zero;
        }

        private void OnHotKeyPressed()
        {
            SetRules();
            System.Media.SystemSounds.Hand.Play();
        }

        private void copyIPButtonClicked(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(this.iPTool.IpAddress.ToString().TrimEnd('\r', '\n'));
            return;
        }

        private void gameGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateActive();
        }
    }
}
