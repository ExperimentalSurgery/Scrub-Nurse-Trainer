using System;
using System.Net;
using System.Net.Sockets;

public class BBOSCReceiver {} // bogus declaration to meet with unity's strict name = filename scheme

#if !UNITY_FLASH
namespace OSC.NET
{
	/// <summary>
	/// OSCReceiver
	/// </summary>
	public class OSCReceiver
	{
		protected UdpClient udpClient;
		protected int localPort;

		public OSCReceiver(int localPort)
		{
			this.localPort = localPort;
			Connect();
		}

		public void Connect()
		{
			if(this.udpClient != null) Close();

			// try to open a new socket automatically - will fail if a Zombie process left the port open
			this.udpClient = new UdpClient(this.localPort);

			// open the socket manually instead, reusing port if it wasn't closed properly
			// NOTE: while this works for remote TUIO control (via Android etc.), it does NOT
			// work for native TUIO touch, or Touch2Tuio->TUIO builds.
// 			IPEndPoint localpt = new IPEndPoint(IPAddress.Parse((Dns.GetHostEntry(Dns.GetHostName())).AddressList[0].ToString()),this.localPort);
//			this.udpClient = new UdpClient();
//			this.udpClient.Client.SetSocketOption(SocketOptionLevel.Socket,SocketOptionName.ReuseAddress, true);
//			this.udpClient.Client.Bind(localpt);
		}

		public void Close()
		{
			if (this.udpClient!=null) this.udpClient.Close();
			this.udpClient = null;
		}

		public OSCPacket Receive()
		{
            try
            {
                IPEndPoint ip = null;
                byte[] bytes = this.udpClient.Receive(ref ip);
                if (bytes != null && bytes.Length > 0)
                    return OSCPacket.Unpack(bytes);

            } catch (Exception e) { 
                Console.WriteLine(e.Message);
                return null;
            }

			return null;
		}
	}
}
#else
// empty dummy
namespace OSC.NET
{
	public class OSCReceiver
	{
		protected int localPort;
		public OSCReceiver(int localPort){}
		public void Connect(){}
		public void Close(){}
		public OSCPacket Receive(){return null;}
	}
}

#endif
