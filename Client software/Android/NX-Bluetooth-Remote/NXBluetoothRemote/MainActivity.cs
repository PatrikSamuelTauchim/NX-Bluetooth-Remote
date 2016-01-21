using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Os;
using Android.Widget;
using Dot42;
using Dot42.Manifest;
using Dot42.Collections;
using System.IO;
using Android.Bluetooth;
using Android.Content;
using Android.Util;
using Java.Io;
using Java.Util;
using System.Text.RegularExpressions;
using System.Text;
using Java.Text;

[assembly: Application("NX Bluetooth Remote")]

[assembly: UsesPermission(Android.Manifest.Permission.BLUETOOTH)]
[assembly: UsesPermission(Android.Manifest.Permission.BLUETOOTH_ADMIN)]

namespace NXBluetoothRemote
{
	[Activity(Icon = "Icon", Label = "NX Bluetooth Remote")]
	public class MainActivity : Activity
	{

		private TextView StatusT;
		private TextView Battery;
		
		BluetoothAdapter BT_Adapter;
		BluetoothDevice BT_Remote_Device;
		BluetoothSocket BT_Socket;
		OutputStream SendStream;
		InputStream ReceiveStream;
		private const int REQUEST_ENABLE_BT = 3;
		int FailCount = 0;
		int ConnStage = 0;
		private int BatteryCh;
		public bool Connected;
		
		string RemoteName = "NX Remote";
		string DebugTag = "NXBR";
		
		int startDelay=0;
		int count=1;
		int delay=0;
		int hold=0;

		protected override void OnCreate(Bundle savedInstance)
		{
			base.OnCreate(savedInstance);
			SetContentView(R.Layouts.MainLayout);
			StatusT = (TextView)FindViewById<TextView>(R.Ids.Status);
			Battery = (TextView)FindViewById<TextView>(R.Ids.Battery);
			
			var ConnectB = (Button)FindViewById(R.Ids.Connect);
			var DoItB = (Button)FindViewById(R.Ids.Shot);
			ConnectB.Click += (s, x) => Connect();
			DoItB.Click += (s, x) => DoIt();
			
			var startDelayBar = FindViewById<SeekBar>(R.Ids.startDelayBar);
			var countBar = FindViewById<SeekBar>(R.Ids.countBar);
			var delayBar = FindViewById<SeekBar>(R.Ids.delayBar);
			var holdBar = FindViewById<SeekBar>(R.Ids.holdBar);
			var startDelayLabel = FindViewById<TextView>(R.Ids.startDelayLabel);
			var countLabel = FindViewById<TextView>(R.Ids.countLabel);
			var delayLabel = FindViewById<TextView>(R.Ids.delayLabel);
			var holdLabel = FindViewById<TextView>(R.Ids.holdLabel);
			
			ShowStatus("Hello");
			startDelayLabel.Text = "After "+ startDelay.ToString() +" seconds";
			countLabel.Text = "take "+ count.ToString() +" photo(s)";
			delayLabel.Text = "with delay of " + delay.ToString()+" seconds";
			holdLabel.Text = "and hold shutter for " + hold.ToString()+"ms";
			
			startDelayBar.ProgressChanged += (s, x) => startDelay = x.Progress; //start delay in Seconds
			countBar.ProgressChanged += (s, x) => count = x.Progress+1;
			delayBar.ProgressChanged += (s, x) => delay = x.Progress*1000;
			holdBar.ProgressChanged += (s, x) => hold = x.Progress*100;
			startDelayBar.ProgressChanged += (s, x) => startDelayLabel.Text = "After "+ startDelay.ToString() +" seconds";
			countBar.ProgressChanged += (s, x) => countLabel.Text = "take "+ count.ToString() +" photos";
			delayBar.ProgressChanged += (s, x) => delayLabel.Text = "with delay of " + (delay/1000).ToString()+" seconds";
			holdBar.ProgressChanged += (s, x) => holdLabel.Text = "and hold shutter for " + hold.ToString()+"ms";
		}
		
		//When you press connect button
		public void Connect()
		{
			if (!Connected)
			{
				if (isBlueToothPresent())
				{
					if (isBlueToothOn())
					{
						if (isRemotePaired())
						{
							FailCount = 0;
							ConnStage = 0;
							BT_Adapter.CancelDiscovery();
							Thread.Sleep(FailCount*100);
							ShowStatus("Connecting to: " + BT_Remote_Device.Address);
							Disconnect();
							Thread.Sleep(FailCount*100);
							ConnectBT();
						}
						else
						{
							ShowStatus("Cannot find paired remote with name "+ RemoteName);
						}
					}
					else
					{
						ShowStatus("BlueTooth is turned off.");
					}
				}
				else
				{
					ShowStatus("Cannot find BlueTooth.");
				}
			}
			else
			{
				Disconnect();
			}
		}
		
		//When you press disconnect button
		public void Disconnect()
		{
			var ConnectB = (Button)FindViewById(R.Ids.Connect);
			try
			{
				//string Msg = "Disconnected\n";
				//SendStream.Write(Encoding.ASCII.GetBytes(Msg+""));
				ShowStatus("Disconnected");
				Connected=false;
				ConnectB.Text = "Connect";
			}
			catch (Exception e)
			{
				ShowStatus("Disconnect error 1: "+e);
			}
			try
			{
				BT_Socket.Close();
				BT_Socket = null;
				ShowStatus("Disconnected");
				Connected=false;
				ConnectB.Text = "Connect";
			}
			catch (Exception e)
			{
				ShowStatus("Disconnect error 2: "+e);
			}
		}
		
		public bool isBlueToothPresent()
		{
			BT_Adapter = BluetoothAdapter.GetDefaultAdapter();
			
			if (BT_Adapter != null)
				return true;
			else
				return false;
		}
		
		public bool isBlueToothOn()
		{
			if (BT_Adapter.IsEnabled())
			{
				return true;
			}
			else
			{
				RequestBlueToothEnable();
				return false;
			}
		}
		
		public void RequestBlueToothEnable()
		{
			Intent enableIntent = new Intent(BluetoothAdapter.ACTION_REQUEST_ENABLE);
			StartActivityForResult(enableIntent, REQUEST_ENABLE_BT);
		}
		
		protected override void OnActivityResult(int requestCode, int resultCode, Intent data)
		{
			if (resultCode == Activity.RESULT_OK)
			{
				ShowStatus("Bluetooth successfully enabled");
			}
			else
			{
				ShowStatus("Failed to enable bluetooth");
			}
		}
		
		public bool isRemotePaired()
		{
			var BT_My_Addr = BT_Adapter.Address; //Get the devices MAC
			var BT_Bonded = BT_Adapter.GetBondedDevices().ToList(); //Get a list of bonded devices- I bonded to a BT2TTL Board earlier.
			
			foreach (var BTDevice in BT_Bonded)
			{
				string tmp = BTDevice.Name;
				if (BTDevice.Name.Contains(RemoteName))
				{
					BT_Remote_Device = BTDevice;
					ShowStatus("Found remote with address: " + BT_Remote_Device.Address);
					break;
				}
			}
			if (BT_Remote_Device!=null)
				return true;
			else
				return false;
		}
		
		// Connection is not always successfull at firtst attempt
		public void OnConnectFailed(string e)
		{
			Log.E(DebugTag, e);
			FailCount++;
			if (FailCount<=20)
			{
				ShowStatus("Connection failed " + FailCount + " times, delay set to " + FailCount*1000 + "ms");
				Thread.Sleep(FailCount*1000);
				ConnectBT();
			}
			else
			{
				ShowStatus("Connection failed for 20 times");
				Connected = false;
			}
		}
		
		public void ConnectBT()
		{
			var ConnectB = (Button)FindViewById(R.Ids.Connect);
			if (ConnStage == 0)
			{
				try
				{
					UUID uuid = BT_Remote_Device.GetUuids()[0].GetUuid();
					BT_Socket = BT_Remote_Device.CreateRfcommSocketToServiceRecord(uuid);
					Log.I(DebugTag,"BT_Socket "+BT_Socket.ToString()+uuid.ToString());
					Thread.Sleep(FailCount*100);
					ConnStage++;
				}
				catch (Exception e)
				{
					ShowStatus("Connection stage 0: "+e);
					string toLog = "Fail: " + FailCount + " Error 0 "+e.ToString();
					OnConnectFailed(toLog);
					return;
				}
			}
			if (ConnStage == 1)
			{
				try
				{
					Thread.Sleep(FailCount*100);
					BT_Adapter.CancelDiscovery();
					Thread.Sleep(FailCount*100);
					BT_Socket.Connect();
					Log.I(DebugTag,"BT_Socket connected: "+BT_Socket.IsConnected());
					Thread.Sleep(FailCount*100);
					ShowStatus("Connected to Remote after " + FailCount + " fail(s)");
					Connected=true;
					ConnectB.Text = "Disconnect";
					ConnStage++;
				}
				catch (Exception e)
				{
					Log.I(DebugTag,"BT_Socket connected: "+BT_Socket.IsConnected());
					ShowStatus("Connection stage 1: "+e);
					string toLog = "Fail: " + FailCount + " Error 1 "+e.ToString();
					Connected=false;
					ConnectB.Text = "Connect";
					OnConnectFailed(toLog);
					return;
				}
			}
			if (ConnStage == 2)
			{
				try
				{
					SendStream = BT_Socket.GetOutputStream();
					ReceiveStream = BT_Socket.GetInputStream();
					Thread.Sleep(FailCount*100);
					Connected=true;
					ConnectB.Text = "Disconnect";
					ConnStage++;
				}
				catch (Exception e)
				{
					ShowStatus("Connection stage 2: "+e);
					string toLog = "Fail: " + FailCount + " Error 2 "+e.ToString();
					Connected=false;
					ConnectB.Text = "Connect";
					OnConnectFailed(toLog);
					return;
				}
			}
			if (ConnStage == 3)
			{
				try
				{
					//string ConnectMsg = "Connected";
					Connected=true;
					ConnectB.Text = "Disconnect";
					//SendStream.Write(Encoding.ASCII.GetBytes(ConnectMsg));
				}
				catch (Exception e)
				{
					ShowStatus("Connection stage  3: "+e);
					Connected=false;
					ConnectB.Text = "Connect";
					string toLog = "Fail: " + FailCount + " Error 3 "+e.ToString();
					OnConnectFailed(toLog);
					return;
				}
			}
		}

		public void DoIt()
		{
			if (Connected)
			{
			SendStream.Write(Encoding.ASCII.GetBytes(startDelay*1000 + "," + count + "," + delay + "," + hold + ";"));
			Log.I(DebugTag, "Sending: " + startDelay*1000 + "," + + count + "," + delay + "," + hold + ";");
			}
			else
			{
				ShowStatus("Connect first!");
			}
		}

		protected override void OnDestroy()
		{
			Disconnect();
			base.OnDestroy();
		}
		
		public void ShowStatus(string status)
		{
			StatusT.Text = status;
		}
	}
}