using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using UnityEngine;

public class ControllerInputManager : MonoBehaviour
{
    public static ControllerInputManager Instance { get; private set; }

    [Header("Serial")]
    public string portName = "/dev/cu.usbmodem101";
    public int baudRate = 115200;

    [Header("Controller IDs")]
    public string player1Id = "Controller 1";
    public string player2Id = "Controller 2";

    [Serializable]
    public struct ControllerState
    {
        public int rotation;
        public bool pushed;
        public bool connected;
    }

    public ControllerState player1;
    public ControllerState player2;

    [DllImport("MacSerial")] static extern bool Serial_Open(string port, int baud);
    [DllImport("MacSerial")] static extern int Serial_Read(byte[] buf, int max);
    [DllImport("MacSerial")] static extern void Serial_Close();

    const byte Header1 = 0xAA;
    const byte Header2 = 0x55;

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct ControllerPacket
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] controllerName;
        public int rotationValue;
        public byte pushed;
    }

    readonly object stateLock = new object();
    readonly byte[] readBuffer = new byte[256];
    readonly List<byte> streamBuffer = new List<byte>();

    Thread serialThread;
    bool running;

    int PacketSize => Marshal.SizeOf(typeof(ControllerPacket));

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        if (!Serial_Open(portName, baudRate))
        {
            Debug.LogError($"Could not open serial port: {portName}");
            enabled = false;
            return;
        }

        running = true;
        serialThread = new Thread(ReadSerialLoop);
        serialThread.IsBackground = true;
        serialThread.Start();
    }

    void ReadSerialLoop()
    {
        while (running)
        {
            int count = Serial_Read(readBuffer, readBuffer.Length);
            if (count <= 0) continue;

            lock (stateLock)
            {
                for (int i = 0; i < count; i++)
                    streamBuffer.Add(readBuffer[i]);

                ParsePackets();
            }
        }
    }

    void ParsePackets()
    {
        while (true)
        {
            int headerIndex = FindHeader();
            if (headerIndex < 0)
            {
                if (streamBuffer.Count > 1)
                    streamBuffer.RemoveRange(0, streamBuffer.Count - 1);
                return;
            }

            if (headerIndex > 0)
                streamBuffer.RemoveRange(0, headerIndex);

            int fullPacketSize = 2 + PacketSize;
            if (streamBuffer.Count < fullPacketSize)
                return;

            byte[] packetBytes = streamBuffer.GetRange(2, PacketSize).ToArray();
            streamBuffer.RemoveRange(0, fullPacketSize);

            var packet = BytesToStruct<ControllerPacket>(packetBytes);
            string id = FixedStringToString(packet.controllerName);

            if (id == player1Id)
            {
                player1.rotation = packet.rotationValue;
                player1.pushed = packet.pushed != 0;
                player1.connected = true;
            }
            else if (id == player2Id)
            {
                player2.rotation = packet.rotationValue;
                player2.pushed = packet.pushed != 0;
                player2.connected = true;
            }
        }
    }

    int FindHeader()
    {
        for (int i = 0; i < streamBuffer.Count - 1; i++)
        {
            if (streamBuffer[i] == Header1 && streamBuffer[i + 1] == Header2)
                return i;
        }
        return -1;
    }

    public ControllerState GetPlayer1()
    {
        lock (stateLock) return player1;
    }

    public ControllerState GetPlayer2()
    {
        lock (stateLock) return player2;
    }

    public void ClearPushes()
    {
        lock (stateLock)
        {
            player1.pushed = false;
            player2.pushed = false;
        }
    }

    static string FixedStringToString(byte[] bytes)
    {
        int len = Array.IndexOf(bytes, (byte)0);
        if (len < 0) len = bytes.Length;
        return Encoding.ASCII.GetString(bytes, 0, len);
    }

    static T BytesToStruct<T>(byte[] bytes) where T : struct
    {
        var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        try
        {
            return Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
        }
        finally
        {
            handle.Free();
        }
    }

    void OnDisable()
    {
        Shutdown();
    }

    void OnApplicationQuit()
    {
        Shutdown();
    }

    void Shutdown()
    {
        running = false;
        serialThread?.Join(500);
        Serial_Close();
    }
}