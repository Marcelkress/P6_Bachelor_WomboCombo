// EncoderInputManager.cs
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

public class EncoderInputManager : MonoBehaviour
{
    [System.Serializable]
    public class EncoderPort
    {
        public string                  portName   = "/dev/cu.usbmodem101";
        public PlayerEncoderController controller;
        [HideInInspector] public StringBuilder sb = new StringBuilder();
    }

    public EncoderPort[] ports = new EncoderPort[1];

    [DllImport("MacSerial")] static extern bool Serial_Open (string port, int baud);
    [DllImport("MacSerial")] static extern int  Serial_Read (byte[] buf, int max);
    [DllImport("MacSerial")] static extern void Serial_Close();

    readonly byte[] readBuffer = new byte[512];

    void OnEnable()
    {
        foreach (var p in ports)
        {
            bool ok = Serial_Open(p.portName, 115200);
            if (!ok) Debug.LogError($"Could not open {p.portName}");
            else     Debug.Log($"Opened {p.portName}");
        }
    }

    void Update()
    {
        foreach (var p in ports)
            ReadPort(p);
    }

    void ReadPort(EncoderPort p)
    {
        int n = Serial_Read(readBuffer, readBuffer.Length);
        if (n <= 0) return;

        p.sb.Append(Encoding.UTF8.GetString(readBuffer, 0, n));

        int idx;
        while ((idx = p.sb.ToString().IndexOf('\n')) >= 0)
        {
            string line = p.sb.ToString().Substring(0, idx).Trim();
            p.sb.Remove(0, idx + 1);
            ParseLine(line, p);
        }
    }

    void ParseLine(string line, EncoderPort p)
    {
        var parts = line.Split(',');
        if (parts.Length != 2) return;

        if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int position)) return;
        if (!int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int button))   return;

        p.controller?.OnEncoderData(position, button == 1);
    }

    void OnDisable()
    {
        foreach (var p in ports)
            Serial_Close();
    }
}