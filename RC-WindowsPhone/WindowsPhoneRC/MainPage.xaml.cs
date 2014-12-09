// Copyright(c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the BSD 2 - Clause License.
// See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Windows.Networking.Sockets;
using Windows.Networking.Proximity;
using System.Diagnostics;
using Windows.Storage.Streams;
using System.Threading.Tasks;
using BluetoothConnectionManager;
using System.Windows.Media;
using Microsoft.Devices.Sensors;
using Microsoft.Xna.Framework;
using System.Windows.Media.Imaging;

namespace WindowsPhoneRC
{
    public partial class MainPage : PhoneApplicationPage
    {
        private ConnectionManager connectionManager;

        private Accelerometer accelerometer;
        // Constructor
        public MainPage()
        {
            InitializeComponent();
            connectionManager = new ConnectionManager();
            connectionManager.MessageReceived += connectionManager_MessageReceived;

            if (!Accelerometer.IsSupported)
            {
                // The device on which the application is running does not support
                // the accelerometer sensor. Alert the user and disable the
                // Start and Stop buttons.
                StatusPane.Text = "This device doesn't have an accellerometer. Sorry.";
            }

        }

        async void connectionManager_MessageReceived(string message)
        {
            Debug.WriteLine("Message received:" + message);
            //dont care
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            connectionManager.Initialize();

            //allow new connection
            ConnectAppToDeviceButton.Content = "Connect";
            ConnectAppToDeviceButton.IsEnabled = true;

            if (accelerometer == null)
            {
                // Instantiate the Accelerometer.
                accelerometer = new Accelerometer();
                accelerometer.TimeBetweenUpdates = TimeSpan.FromMilliseconds(100);
                accelerometer.CurrentValueChanged +=
                    new EventHandler<SensorReadingEventArgs<AccelerometerReading>>(accelerometer_CurrentValueChanged);
                try
                {
                    StatusPane.Text = "Controls Ready";
                    accelerometer.Start();
                }
                catch (InvalidOperationException ex)
                {
                    StatusPane.Text = "An Error Occured.";
                }
            }
        }

        void accelerometer_CurrentValueChanged(object sender, SensorReadingEventArgs<AccelerometerReading> e)
        {
            // Call UpdateUI on the UI thread and pass the AccelerometerReading.
            Dispatcher.BeginInvoke(() => SendCommand(e.SensorReading));
        }

        String doubleToUShortString(double num)
        {
            return Convert.ToUInt16(Math.Min((Math.Abs(num)) * (400), 255)).ToString("000");            
        }

        private async Task SendCommand(AccelerometerReading accelerometerReading)
        {
            Vector3 acceleration = accelerometerReading.Acceleration;
            //defined thresholds for motion on accel
            double LR_THRESHOLD = 0.30;
            double FB_THRESHOLD = 0.20;

            //defined commands, don't change without changing Galileo-RC Command
            string RIGHT = "0";
            string LEFT = "1";
            string FWD = "1";
            string BACK = "0";
            string NOP = "2";

            //used to build and display the command
            string command = "";
            StatusPane.Text = "";
            
            if (acceleration.Y < (-1 * LR_THRESHOLD))
            {
                //right
                RIGHT_IMAGE.Source = (BitmapSource)Resources["RIGHT_ACTIVE"];
                LEFT_IMAGE.Source = (BitmapSource)Resources["LEFT_INACTIVE"];
                StatusPane.Text += " Right";
                command += RIGHT;

            }
            else if (acceleration.Y > LR_THRESHOLD)
            {
                //left
                RIGHT_IMAGE.Source = (BitmapSource)Resources["RIGHT_INACTIVE"];
                LEFT_IMAGE.Source = (BitmapSource)Resources["LEFT_ACTIVE"];
                StatusPane.Text += " Left";
                command += LEFT;
            }
            else
            {
                //stop left/r
                RIGHT_IMAGE.Source = (BitmapSource)Resources["RIGHT_INACTIVE"];
                LEFT_IMAGE.Source = (BitmapSource)Resources["LEFT_INACTIVE"];
                command += "2";
            }

            //get the unsigned short value for speed control            
            command += doubleToUShortString(acceleration.Y);

            if (acceleration.X > FB_THRESHOLD)
            {
                //send forward
                DOWN_IMAGE.Source = (BitmapSource)Resources["DOWN_INACTIVE"];
                UP_IMAGE.Source = (BitmapSource)Resources["UP_ACTIVE"];
                StatusPane.Text += " Forward";
                command += FWD;
            }
            else if (acceleration.X < (-1 * FB_THRESHOLD))
            {
                //send backward
                StatusPane.Text += " Backwards";
                UP_IMAGE.Source = (BitmapSource)Resources["UP_INACTIVE"];
                DOWN_IMAGE.Source = (BitmapSource)Resources["DOWN_ACTIVE"];
                command += BACK;
            }
            else
            {
                //send stop forward
                UP_IMAGE.Source = (BitmapSource)Resources["UP_INACTIVE"];
                DOWN_IMAGE.Source = (BitmapSource)Resources["DOWN_INACTIVE"];
                command += NOP;
            }

            //get the unsigned short value for speed control            
            command += doubleToUShortString(acceleration.X);

            //final command is two chars
            //first char is 0, 1, 2 to indicate left/right/stop or 
            //forward/right/stop for each set of motors
            await connectionManager.SendCommand(command);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            connectionManager.Terminate();
        }

        private void ConnectAppToDeviceButton_Click_1(object sender, RoutedEventArgs e)
        {
            AppToDevice();
        }

        private async void AppToDevice()
        {
            ConnectAppToDeviceButton.Content = "Connecting...";
            PeerFinder.AlternateIdentities["Bluetooth:Paired"] = "";
            var pairedDevices = await PeerFinder.FindAllPeersAsync();

            if (pairedDevices.Count == 0)
            {
                Debug.WriteLine("No paired devices were found.");
            }
            else
            { 
                foreach (var pairedDevice in pairedDevices)
                {
                    if (pairedDevice.DisplayName == DeviceName.Text)
                    {
                        connectionManager.Connect(pairedDevice.HostName);
                        ConnectAppToDeviceButton.Content = "Connected";
                        DeviceName.IsReadOnly = true;
                        ConnectAppToDeviceButton.IsEnabled = false;
                        continue;
                    }
                }
            }
        }

        private void ShutdownDeviceButton_Click_1(object sender, RoutedEventArgs e)
        {
            //send shutdown command
            connectionManager.SendCommand("44444444");
        }

        protected override void OnOrientationChanged(OrientationChangedEventArgs e)
        {
            if (e.Orientation == PageOrientation.LandscapeLeft)
            {
                base.OnOrientationChanged(e);
            }
        }

    }
}