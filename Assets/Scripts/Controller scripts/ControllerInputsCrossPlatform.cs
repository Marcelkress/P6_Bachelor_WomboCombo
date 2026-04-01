using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Reads two rotary-encoder controllers over a serial/USB connection.
///
/// Linux  — Uses libc termios via P/Invoke. No native plugin required.
///          The LinuxSerial.dll / System.IO.Ports fallback paths are gone;
///          this code talks to the OS directly through libc.so.6.
///
/// macOS  — Tries the MacSerial native plugin first; falls back to libc
///          termios if the plugin is absent.
///
/// Windows— Uses System.IO.Ports (fully supported on Windows Mono/CoreCLR).
///
/// ── LINUX QUICK-START ─────────────────────────────────────────────────────
///  1. Identify your port: ls /dev/ttyACM0  (or /dev/ttyUSB0, etc.)
///  2. Grant access (re-login after):  sudo usermod -aG uucp $USER
///  3. log out and back in (or reboot) to apply group change.
///  3. Set portName in the Inspector to match (e.g. /dev/ttyACM0).
/// ─────────────────────────────────────────────────────────────────────────
/// </summary>
public class ControllerInputCrossPlatform : MonoBehaviour
{
    public static ControllerInputCrossPlatform Instance { get; private set; }

    [Header("Serial")]
    public string portName = DefaultPortName;
    public int    baudRate = 115200;

    [Header("Controller IDs")]
    public string player1Id = "Controller 1";
    public string player2Id = "Controller 2";
    
    public ControllerState player1;
    public ControllerState player2;

    // ─────────────────────────────────────────────────────────────────────
    //  Default port per platform
    // ─────────────────────────────────────────────────────────────────────
#if   UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
    const string DefaultPortName = "/dev/ttyACM0";
#elif UNITY_STANDALONE_OSX   || UNITY_EDITOR_OSX
    const string DefaultPortName = "/dev/cu.usbmodem101";
#elif UNITY_STANDALONE_WIN   || UNITY_EDITOR_WIN
    const string DefaultPortName = "COM3";
#else
    const string DefaultPortName = "";
#endif

    // ─────────────────────────────────────────────────────────────────────
    //  Serial port abstraction
    // ─────────────────────────────────────────────────────────────────────
    interface ISerialPort
    {
        bool Open(string port, int baud);
        int  Read(byte[] buf, int max);
        void Close();
    }

    // ═════════════════════════════════════════════════════════════════════
    //  POSIX (Linux + macOS) — libc termios via P/Invoke
    // ═════════════════════════════════════════════════════════════════════
#if UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX

    // ── libc syscalls (shared between Linux and macOS) ───────────────────
    [DllImport("libc", SetLastError = true, EntryPoint = "open")]
    static extern int  libc_open([MarshalAs(UnmanagedType.LPStr)] string path, int flags);

    [DllImport("libc", SetLastError = true, EntryPoint = "read")]
    static extern int  libc_read(int fd, byte[] buf, int count);

    [DllImport("libc", EntryPoint = "close")]
    static extern int  libc_close(int fd);

    [DllImport("libc", SetLastError = true, EntryPoint = "tcgetattr")]
    static extern int  libc_tcgetattr(int fd, ref TermiosNative t);

    [DllImport("libc", SetLastError = true, EntryPoint = "tcsetattr")]
    static extern int  libc_tcsetattr(int fd, uint action, ref TermiosNative t);

    // cfmakeraw sets raw mode (no echo, no canonical, no signals, etc.)
    [DllImport("libc", EntryPoint = "cfmakeraw")]
    static extern void libc_cfmakeraw(ref TermiosNative t);

    // ── Platform-specific termios layout, constants, and baud helpers ────
    //
    //   Linux : tcflag_t = uint (4 bytes), speed_t = uint (4 bytes), NCCS = 19
    //   macOS : tcflag_t = ulong (8 bytes), speed_t = ulong (8 bytes), NCCS = 20
    //           Linux uses encoded Bxxx constants; macOS uses raw integer baud values.
    // ────────────────────────────────────────────────────────────────────

#if UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX

    // open(2) flags (Linux x86-64 / ARM64)
    const int  O_RDWR          = 2;
    const int  O_NOCTTY        = 256;    // 0x100
    const int  O_NONBLOCK      = 2048;   // 0x800
    const uint TCSANOW         = 0u;
    // c_cflag bits
    const uint CSIZE_MASK      = 0x030u;
    const uint CS8             = 0x030u;
    const uint CREAD           = 0x080u;
    const uint CLOCAL          = 0x800u;
    // c_iflag bits
    const uint IGNPAR          = 0x004u;
    // c_cc indices
    const int  CC_SIZE         = 19;
    const int  VTIME_IDX       = 5;
    const int  VMIN_IDX        = 6;
    // EAGAIN errno on Linux
    const int  EAGAIN          = 11;

    // Linux encodes baud as a symbolic constant, NOT the raw integer.
    [DllImport("libc", SetLastError = true, EntryPoint = "cfsetispeed")]
    static extern int libc_cfsetispeed(ref TermiosNative t, uint speed);
    [DllImport("libc", SetLastError = true, EntryPoint = "cfsetospeed")]
    static extern int libc_cfsetospeed(ref TermiosNative t, uint speed);

    static uint BaudConst(int baud) => baud switch
    {
        9600   => 15u,
        19200  => 16u,
        38400  => 17u,
        57600  => 0x1001u,
        115200 => 0x1002u,
        230400 => 0x1003u,
        460800 => 0x1004u,
        _      => 0x1002u   // default 115200
    };

    // Linux termios struct — 44 bytes on x86-64 and ARM64
    // Layout: 4x uint (iflag/oflag/cflag/lflag) + 1 byte (c_line) +
    //         19 bytes (c_cc) + 2x uint (ispeed/ospeed)
    [StructLayout(LayoutKind.Sequential)]
    struct TermiosNative
    {
        public uint c_iflag;
        public uint c_oflag;
        public uint c_cflag;
        public uint c_lflag;
        public byte c_line;          // Linux-specific discipline line byte
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 19)]
        public byte[] c_cc;
        public uint c_ispeed;
        public uint c_ospeed;
    }

#else // ── macOS ────────────────────────────────────────────────────────

    const int   O_RDWR          = 2;
    const int   O_NOCTTY        = 0x20000;  // macOS value differs from Linux
    const int   O_NONBLOCK      = 4;        // 0x0004
    const uint  TCSANOW         = 0u;
    // c_cflag bits (BSD/macOS values, different from Linux)
    const ulong CSIZE_MASK      = 0x0300UL;
    const ulong CS8             = 0x0300UL;
    const ulong CREAD           = 0x0800UL;
    const ulong CLOCAL          = 0x8000UL;
    // c_iflag bits
    const ulong IGNPAR          = 0x0004UL;
    // c_cc indices (macOS VMIN/VTIME are at different positions than Linux)
    const int   CC_SIZE         = 20;
    const int   VTIME_IDX       = 17;
    const int   VMIN_IDX        = 16;
    // EAGAIN errno on macOS/BSD
    const int   EAGAIN          = 35;

    // macOS speed_t = unsigned long (8 bytes on 64-bit), and cfsetispeed
    // takes the raw integer baud rate (not a Linux-style encoded constant).
    [DllImport("libc", SetLastError = true, EntryPoint = "cfsetispeed")]
    static extern int libc_cfsetispeed(ref TermiosNative t, ulong speed);
    [DllImport("libc", SetLastError = true, EntryPoint = "cfsetospeed")]
    static extern int libc_cfsetospeed(ref TermiosNative t, ulong speed);

    static ulong BaudConst(int baud) => (ulong)baud;  // macOS uses raw value

    // macOS termios struct — 72 bytes on 64-bit macOS
    // tcflag_t = unsigned long (8 bytes), NCCS = 20 cc_t bytes,
    // then 4 bytes padding to align speed_t (unsigned long, 8 bytes).
    [StructLayout(LayoutKind.Sequential)]
    struct TermiosNative
    {
        public ulong c_iflag;
        public ulong c_oflag;
        public ulong c_cflag;
        public ulong c_lflag;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public byte[] c_cc;
        public uint   _pad;          // 4-byte padding: aligns c_ispeed to 8 bytes
        public ulong  c_ispeed;
        public ulong  c_ospeed;
    }

#endif // Linux vs macOS constants

    // ── PosixSerialPort: shared implementation ───────────────────────────
    sealed class PosixSerialPort : ISerialPort
    {
        int _fd = -1;

        public bool Open(string port, int baud)
        {
            // O_NONBLOCK during open avoids blocking on modem-control lines;
            // we keep it for non-blocking reads in the poll loop.
            _fd = libc_open(port, O_RDWR | O_NOCTTY | O_NONBLOCK);
            if (_fd < 0)
            {
                int errno = Marshal.GetLastWin32Error();
                string hint = errno switch
                {
                    2  => " — device not found. Check the port name (ls /dev/ttyACM* /dev/ttyUSB*).",
                    13 => " — permission denied. Run: sudo usermod -aG dialout $USER  (then re-login).",
                    16 => " — device busy. Another process may have the port open.",
                    _  => string.Empty
                };
                Debug.LogError($"[Serial] open({port}) failed. errno={errno}{hint}");
                return false;
            }

            // Read current terminal attributes
            var t = new TermiosNative();
            if (libc_tcgetattr(_fd, ref t) < 0)
            {
                Debug.LogError($"[Serial] tcgetattr failed. errno={Marshal.GetLastWin32Error()}");
                libc_close(_fd); _fd = -1;
                return false;
            }

            // Raw mode: disables canonical processing, echo, signals, etc.
            libc_cfmakeraw(ref t);

            // Ensure 8-N-1, receiver enabled, ignore modem control lines.
            t.c_cflag = (t.c_cflag & ~CSIZE_MASK) | CS8 | CREAD | CLOCAL;

            // Ignore framing/parity errors from the device.
            t.c_iflag |= IGNPAR;

            // VMIN=0 / VTIME=0 → read() returns immediately with whatever is
            // in the buffer (or 0 bytes). Combined with O_NONBLOCK this is a
            // clean non-blocking poll; the read loop sleeps 1 ms between polls.
            if (t.c_cc == null) t.c_cc = new byte[CC_SIZE];
            t.c_cc[VMIN_IDX]  = 0;
            t.c_cc[VTIME_IDX] = 0;

            // Apply baud rate via the cfset functions (correct for each platform)
            if (libc_cfsetispeed(ref t, BaudConst(baud)) < 0 ||
                libc_cfsetospeed(ref t, BaudConst(baud)) < 0)
            {
                Debug.LogError($"[Serial] cfsetispeed/cfsetospeed failed. errno={Marshal.GetLastWin32Error()}");
                libc_close(_fd); _fd = -1;
                return false;
            }

            // Commit settings immediately (TCSANOW)
            if (libc_tcsetattr(_fd, TCSANOW, ref t) < 0)
            {
                Debug.LogError($"[Serial] tcsetattr failed. errno={Marshal.GetLastWin32Error()}");
                libc_close(_fd); _fd = -1;
                return false;
            }

            Debug.Log($"[Serial] Opened {port} @ {baud} baud (fd={_fd}, libc termios)");
            return true;
        }

        public int Read(byte[] buf, int max)
        {
            if (_fd < 0) return 0;
            int n = libc_read(_fd, buf, max);
            if (n < 0)
            {
                int err = Marshal.GetLastWin32Error();
                if (err == EAGAIN) return 0;   // normal: no data yet with O_NONBLOCK
                Debug.LogWarning($"[Serial] read error. errno={err}");
                return 0;
            }
            return n;
        }

        public void Close()
        {
            if (_fd >= 0) { libc_close(_fd); _fd = -1; }
        }
    }

#endif // LINUX || OSX

    // ═════════════════════════════════════════════════════════════════════
    //  macOS — native plugin (preferred; PosixSerialPort is the fallback)
    // ═════════════════════════════════════════════════════════════════════
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
    sealed class MacPluginSerialPort : ISerialPort
    {
        [DllImport("MacSerial")] static extern bool Serial_Open(string port, int baud);
        [DllImport("MacSerial")] static extern int  Serial_Read(byte[] buf, int max);
        [DllImport("MacSerial")] static extern void Serial_Close();

        bool _open;

        public bool Open(string port, int baud)
        {
            try
            {
                _open = Serial_Open(port, baud);
                if (_open) Debug.Log($"[Serial] Opened {port} @ {baud} baud (MacSerial plugin)");
                return _open;
            }
            catch (DllNotFoundException)
            {
                return false;   // caller will fall back to PosixSerialPort
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Serial] MacSerial plugin error: {ex.Message}");
                return false;
            }
        }

        public int  Read(byte[] buf, int max) => _open ? Serial_Read(buf, max) : 0;
        public void Close()
        {
            if (_open) { Serial_Close(); _open = false; }
        }
    }
#endif

    // ═════════════════════════════════════════════════════════════════════
    //  Windows — System.IO.Ports
    // ═════════════════════════════════════════════════════════════════════
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    sealed class WindowsSerialPort : ISerialPort
    {
        System.IO.Ports.SerialPort _port;

        public bool Open(string port, int baud)
        {
            try
            {
                _port = new System.IO.Ports.SerialPort(port, baud) { ReadTimeout = 10 };
                _port.Open();
                Debug.Log($"[Serial] Opened {port} @ {baud} baud (System.IO.Ports)");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Serial] Windows open failed: {ex.Message}");
                return false;
            }
        }

        public int Read(byte[] buf, int max)
        {
            try
            {
                int avail = _port.BytesToRead;
                return avail > 0 ? _port.Read(buf, 0, Math.Min(max, avail)) : 0;
            }
            catch (TimeoutException) { return 0; }
            catch (Exception ex)
            {
                Debug.LogError($"[Serial] Windows read error: {ex.Message}");
                return 0;
            }
        }

        public void Close()
        {
            try { if (_port?.IsOpen == true) _port.Close(); }
            catch (Exception ex) { Debug.LogWarning($"[Serial] Windows close error: {ex.Message}"); }
        }
    }
#endif

    // ─────────────────────────────────────────────────────────────────────
    //  Packet protocol  (0xAA 0x55 | ControllerPacket)
    // ─────────────────────────────────────────────────────────────────────
    const byte Header1 = 0xAA;
    const byte Header2 = 0x55;

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct ControllerPacket
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] controllerName;
        public int    rotationValue;
        public byte   pushed;
    }

    int PacketSize => Marshal.SizeOf(typeof(ControllerPacket));

    // ─────────────────────────────────────────────────────────────────────
    //  Runtime state
    // ─────────────────────────────────────────────────────────────────────
    readonly object     _lock      = new object();
    readonly byte[]     _readBuf   = new byte[256];
    readonly List<byte> _streamBuf = new List<byte>();

    ISerialPort   _serial;
    Thread        _thread;
    volatile bool _running;

    // ─────────────────────────────────────────────────────────────────────
    //  Unity lifecycle
    // ─────────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        
        inputManager = GetComponent<PlayerInputManager>();
    }

    void Start()
    {
        if (string.IsNullOrEmpty(portName))
        {
            Debug.LogError("[Serial] No port name configured.");
            enabled = false;
            return;
        }

        _serial = CreateAndOpen();
        if (_serial == null)
        {
            Debug.LogError($"[Serial] Could not open {portName}. See earlier messages for details.");
            enabled = false;
            return;
        }

        _running = true;
        _thread  = new Thread(ReadLoop)
        {
            IsBackground = true,
            Name         = "ControllerSerial"
        };
        _thread.Start();
    }

    ISerialPort CreateAndOpen()
    {
#if UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
        var p = new PosixSerialPort();
        return p.Open(portName, baudRate) ? p : null;

#elif UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
        // Try native MacSerial plugin first; fall back to libc if absent
        var mac = new MacPluginSerialPort();
        if (mac.Open(portName, baudRate)) return mac;

        Debug.Log("[Serial] MacSerial plugin unavailable — using libc termios fallback.");
        var posix = new PosixSerialPort();
        return posix.Open(portName, baudRate) ? posix : null;

#elif UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        var win = new WindowsSerialPort();
        return win.Open(portName, baudRate) ? win : null;

#else
        Debug.LogError("[Serial] No serial implementation for this platform.");
        return null;
#endif
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Background read loop
    // ─────────────────────────────────────────────────────────────────────
    void ReadLoop()
    {
        while (_running)
        {
            int n = _serial.Read(_readBuf, _readBuf.Length);
            if (n <= 0) { Thread.Sleep(1); continue; }  // 1 ms poll interval

            lock (_lock)
            {
                for (int i = 0; i < n; i++) _streamBuf.Add(_readBuf[i]);
                ParsePackets();
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Packet parsing
    // ─────────────────────────────────────────────────────────────────────
    void ParsePackets()
    {
        while (true)
        {
            int hi = FindHeader();
            if (hi < 0)
            {
                // Keep the last byte in case it is the first byte of the next header
                if (_streamBuf.Count > 1) _streamBuf.RemoveRange(0, _streamBuf.Count - 1);
                return;
            }
            if (hi > 0) _streamBuf.RemoveRange(0, hi);  // discard garbage before header

            int fullSize = 2 + PacketSize;
            if (_streamBuf.Count < fullSize) return;    // wait for more bytes

            byte[] raw = _streamBuf.GetRange(2, PacketSize).ToArray();
            _streamBuf.RemoveRange(0, fullSize);

            var pkt = BytesToStruct<ControllerPacket>(raw);
            string id = NullTerminatedAscii(pkt.controllerName);

            if (id == player1Id)
            {
                player1.rotation  = pkt.rotationValue;
                player1.pushed    = pkt.pushed != 0;
                player1.connected = true;
            }
            else if (id == player2Id)
            {
                player2.rotation  = pkt.rotationValue;
                player2.pushed    = pkt.pushed != 0;
                player2.connected = true;
            }
        }
    }

    int FindHeader()
    {
        for (int i = 0; i < _streamBuf.Count - 1; i++)
            if (_streamBuf[i] == Header1 && _streamBuf[i + 1] == Header2) return i;
        return -1;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────
    public ControllerState GetPlayer1() { lock (_lock) return player1; }
    public ControllerState GetPlayer2() { lock (_lock) return player2; }

    public void ClearPushes()
    {
        lock (_lock) { player1.pushed = false; player2.pushed = false; }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────────────
    static string NullTerminatedAscii(byte[] b)
    {
        int len = Array.IndexOf(b, (byte)0);
        return Encoding.ASCII.GetString(b, 0, len < 0 ? b.Length : len);
    }

    static T BytesToStruct<T>(byte[] bytes) where T : struct
    {
        var h = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        try   { return Marshal.PtrToStructure<T>(h.AddrOfPinnedObject()); }
        finally { h.Free(); }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Shutdown
    // ─────────────────────────────────────────────────────────────────────
    void OnDisable()         => Shutdown();
    void OnApplicationQuit() => Shutdown();

    public ControllerState GetInput(int ID)
    {
        return ID == 1 ? player1 : player2;
    }

    private PlayerInputManager inputManager;
    
    void Update()
    {
        if (inputManager.playerCount == 0)
        {
            if (player1.connected == true)
            {
                inputManager.JoinPlayer();
            }
        }
        else if(inputManager.playerCount == 1)
        {
            if (player2.connected == true)
            {
                inputManager.JoinPlayer();
            }
        }
    }
    
    void Shutdown()
    {
        _running = false;
        _thread?.Join(500);
        _serial?.Close();
        _serial = null;
    }
}