using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Devices.Enumeration;
using Windows.Devices.Bluetooth.Rfcomm;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls.Primitives;


namespace NXBluetoothRemote
{
    public sealed partial class MainPage : Page
    {
        // SETTINGS
        public DeviceInformation LastConnectedDevice;
        public DeviceInformation NowConnectedDevice;
        public DeviceInformation AutoConnectDevice;
        public bool AutoConnectLast;

        private Windows.Devices.Bluetooth.Rfcomm.RfcommDeviceService _service;
        private StreamSocket _socket;
        private DataWriter dataWriterObject;
        private DataReader dataReaderObject;
        ObservableCollection<PairedDeviceInfo> _pairedDevices;
        private CancellationTokenSource ReadCancellationTokenSource;
        public MainPage()
        {
            this.InitializeComponent();
            this.Loaded += new RoutedEventHandler(GetPairedDevices);

        }
        async void GetPairedDevices(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Getting list of paired devices");
            try
            {
                DeviceInformationCollection DeviceInfoCollection = await DeviceInformation.FindAllAsync(RfcommDeviceService.GetDeviceSelector(RfcommServiceId.SerialPort));

                var numDevices = DeviceInfoCollection.Count();

                _pairedDevices = new ObservableCollection<PairedDeviceInfo>();
                _pairedDevices.Clear();

                FlyoutBase mn = ConnectButton.Flyout;
                MenuFlyout m = (MenuFlyout)mn;
                m.Items.Clear();

                if (numDevices == 0)
                {
                    System.Diagnostics.Debug.WriteLine("No paired devices found.");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("{0} paired devices found.", numDevices);

                    foreach (var deviceInfo in DeviceInfoCollection)
                    {
                        _pairedDevices.Add(new PairedDeviceInfo(deviceInfo));

                        ToggleMenuFlyoutItem fi = new ToggleMenuFlyoutItem();
                        fi.Text = deviceInfo.Name;

                        if (NowConnectedDevice != null && NowConnectedDevice.Id == deviceInfo.Id )
                        {
                            fi.IsChecked = true;
                        }
                        else
                        {
                            fi.IsChecked = false;
                        }
                        fi.Click += new RoutedEventHandler((ss, ev) => MenuFlyoutItemDevice_Click(ss, ev, deviceInfo, fi));
                        m.Items.Add(fi);
                    }
                }
                MenuFlyoutSeparator fs = new MenuFlyoutSeparator();
                m.Items.Add(fs);
                MenuFlyoutItem f = new MenuFlyoutItem();
                f.Text = "Refresh";
                f.Click += new RoutedEventHandler(GetPairedDevices);
                m.Items.Add(f);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("GetPairedDevices: " + ex.Message);
            }
        }
        private void MenuFlyoutItemDevice_Click(object sender, RoutedEventArgs e, DeviceInformation deviceInfo, ToggleMenuFlyoutItem button)
        {
            System.Diagnostics.Debug.WriteLine(" MenuFlyoutItemDevice_Click {0}", deviceInfo.Name);

            Connect(deviceInfo);
        }

        private void MenuFlyoutItemRefresh_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine(" MenuFlyoutItemRefresh_Click");

            GetPairedDevices(sender, e);
        }

        async private void Connect(DeviceInformation DeviceInfo)
        {
            System.Diagnostics.Debug.WriteLine("Connect");

            bool success = true;
            try
            {
                _service = await RfcommDeviceService.FromIdAsync(DeviceInfo.Id);

                if (_socket != null)
                {
                    // Disposing the socket with close it and release all resources associated with the socket
                    _socket.Dispose();
                }

                _socket = new StreamSocket();
                try
                {
                    // Note: If either parameter is null or empty, the call will throw an exception
                    await _socket.ConnectAsync(_service.ConnectionHostName, _service.ConnectionServiceName);
                }
                catch (Exception ex)
                {
                    success = false;
                    System.Diagnostics.Debug.WriteLine("Connect:" + ex.Message);
                }
                // If the connection was successful, the RemoteAddress field will be populated
                if (success)
                {
                    this.DoButton.IsEnabled = true;
                    string msg = String.Format("Connected to {0}!", _socket.Information.RemoteAddress.DisplayName);
                    System.Diagnostics.Debug.WriteLine(msg);
                    this.NowConnectedDevice = DeviceInfo;
                    //Listen(); If listening works at all, output is not handle yet.
                }
                else
                {
                    this.DoButton.IsEnabled = false;
                    this.NowConnectedDevice = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Overall Connect: " + ex.Message);
                _socket.Dispose();
                _socket = null;
                this.NowConnectedDevice = null;
            }
        }

        private async void button_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            if (button != null)
            {
                switch ((string)button.Content)
                {
                    case "Disconnect":
                        CancelReadTask();
                        await this._socket.CancelIOAsync();
                        _socket.Dispose();
                        _socket = null;
                        this.DoButton.IsEnabled = false;
                        break;
                    case "Send":
                        System.Diagnostics.Debug.WriteLine("Send button hit");
                        Send(this.startDelaySlider.Value.ToString()+","+ this.countSlider.Value.ToString() + "," + this.delaySlider.Value.ToString() + "," + this.holdSlider.Value.ToString() + ";");
                        //Listen();
                        break;
                    case "Refresh":
                        GetPairedDevices(sender, e);
                        break;
                }
            }
        }

        public async void Send(string msg)
        {
            try
            {
                if (_socket.OutputStream != null)
                {
                    // Create the DataWriter object and attach to OutputStream
                    dataWriterObject = new DataWriter(_socket.OutputStream);

                    //Launch the WriteAsync task to perform the write
                    await WriteAsync(msg);
                    System.Diagnostics.Debug.WriteLine("Send");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Send() failed, not connected");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Send(): " + ex.Message);
            }
            finally
            {
                // Cleanup once complete
                if (dataWriterObject != null)
                {
                    dataWriterObject.DetachStream();
                    dataWriterObject = null;
                }
            }
        }

        /// <summary>
        /// WriteAsync: Task that asynchronously writes data from the input text box 'sendText' to the OutputStream 
        /// </summary>
        /// <returns></returns>
        private async Task WriteAsync(string msg)
        {
            System.Diagnostics.Debug.WriteLine("WriteAsync");
            Task<UInt32> storeAsyncTask;

            if (msg.Length != 0)
            {
                // Load the text to the dataWriter object
                dataWriterObject.WriteString(msg);

                // Launch an async task to complete the write operation
                storeAsyncTask = dataWriterObject.StoreAsync().AsTask();

                UInt32 bytesWritten = await storeAsyncTask;
                if (bytesWritten > 0)
                {
                    string status_Text = msg + ", ";
                    status_Text += bytesWritten.ToString();
                    status_Text += " bytes written successfully!";
                    System.Diagnostics.Debug.WriteLine(status_Text);
                }
            }
            else
            {
                string status_Text2 = "Enter the text you want to write and then click on 'WRITE'";
                System.Diagnostics.Debug.WriteLine(status_Text2);
            }
        }

        /// <summary>
        /// - Create a DataReader object
        /// - Create an async task to read from the SerialDevice InputStream
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Listen()
        {
            System.Diagnostics.Debug.WriteLine("Listening started");
            try
            {
                ReadCancellationTokenSource = new CancellationTokenSource();
                if (_socket.InputStream != null)
                {
                    dataReaderObject = new DataReader(_socket.InputStream);
                    // keep reading the serial input
                    while (true)
                    {
                        await ReadAsync(ReadCancellationTokenSource.Token);
                    }
                }
            }
            catch (Exception ex)
            {
                //this.DoButton.IsEnabled = false;
                /*this.buttonDisconnect.IsEnabled = false;*/
                /*this.textBlockBTName.Text = "";*/
                /*this.TxtBlock_SelectedID.Text = "";*/
                if (ex.GetType().Name == "TaskCanceledException")
                {
                    System.Diagnostics.Debug.WriteLine("Listen: Reading task was cancelled, closing device and cleaning up");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Listen: " + ex.Message);
                }
            }
            finally
            {
                // Cleanup once complete
                if (dataReaderObject != null)
                {
                    dataReaderObject.DetachStream();
                    dataReaderObject = null;
                }
            }
        }

        /// <summary>
        /// ReadAsync: Task that waits on data and reads asynchronously from the serial device InputStream
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task ReadAsync(CancellationToken cancellationToken)
        {
            Task<UInt32> loadAsyncTask;

            uint ReadBufferLength = 1024;

            // If task cancellation was requested, comply
            cancellationToken.ThrowIfCancellationRequested();

            // Set InputStreamOptions to complete the asynchronous read operation when one or more bytes is available
            dataReaderObject.InputStreamOptions = InputStreamOptions.Partial;

            // Create a task object to wait for data on the serialPort.InputStream
            loadAsyncTask = dataReaderObject.LoadAsync(ReadBufferLength).AsTask(cancellationToken);

            // Launch the task and wait
            UInt32 bytesRead = await loadAsyncTask;
            if (bytesRead > 0)
            {
                try
                {
                    string recvdtxt = dataReaderObject.ReadString(bytesRead);
                    System.Diagnostics.Debug.WriteLine(recvdtxt);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("ReadAsync: " + ex.Message);
                }

            }
        }

        /// <summary>
        /// CancelReadTask:
        /// - Uses the ReadCancellationTokenSource to cancel read operations
        /// </summary>
        private void CancelReadTask()
        {
            if (ReadCancellationTokenSource != null)
            {
                if (!ReadCancellationTokenSource.IsCancellationRequested)
                {
                    ReadCancellationTokenSource.Cancel();
                }
            }
        }

        /// <summary>
        ///  Class to hold all paired device information
        /// </summary>
        public class PairedDeviceInfo
        {
            internal PairedDeviceInfo(DeviceInformation deviceInfo)
            {
                this.DeviceInfo = deviceInfo;
                this.ID = this.DeviceInfo.Id;
                this.Name = this.DeviceInfo.Name;
            }

            public string Name { get; private set; }
            public string ID { get; private set; }
            public DeviceInformation DeviceInfo { get; private set; }
        }
    }
}
