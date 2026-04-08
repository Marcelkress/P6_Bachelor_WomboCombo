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
/// macOS  — Tries the MacSerial native plugin first; falls back to libc termios.
/// Windows— Uses System.IO.Ports.
///
/// Reads are BLOCKING — the OS wakes the read thread the moment bytes arrive,
/// so there is no polling latency. Parsing uses a fixed buffer and avoids
/// per-packet allocations to keep the GC quiet.
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
    interface ISerialPort
    {
        bool Open(string port, int baud);
        int  Read(byte[] buf, int max);   // BLOCKING — returns ≥1 or 0 on retryable error
        void Close();
    }

    // ═════════════════════════════════════════════════════════════════════
    //  POSIX (Linux + macOS) — libc termios via P/Invoke, BLOCKING reads
    // ═════════════════════════════════════════════════════════════════════
#if UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX

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

    [DllImport("libc", EntryPoint = "cfmakeraw")]
    static extern void libc_cfmakeraw(ref TermiosNative t);

#if UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX

    const int  O_RDWR          = 2;
    const int  O_NOCTTY        = 256;
    const uint TCSANOW         = 0u;
    const uint CSIZE_MASK      = 0x030u;
    const uint CS8             = 0x030u;
    const uint CREAD           = 0x080u;
    const uint CLOCAL          = 0x800u;
    const uint IGNPAR          = 0x004u;
    const int  CC_SIZE         = 19;
    const int  VTIME_IDX       = 5;
    const int  VMIN_IDX        = 6;
    const int  EINTR           = 4;

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
        _      => 0x1002u
    };

    [StructLayout(LayoutKind.Sequential)]
    struct TermiosNative
    {
        public uint c_iflag;
        public uint c_oflag;
        public uint c_cflag;
        public uint c_lflag;
        public byte c_line;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 19)]
        public byte[] c_cc;
        public uint c_ispeed;
        public uint c_ospeed;
    }

#else // ── macOS ────────────────────────────────────────────────────────

    const int   O_RDWR          = 2;
    const int   O_NOCTTY        = 0x20000;
    const uint  TCSANOW         = 0u;
    const ulong CSIZE_MASK      = 0x0300UL;
    const ulong CS8             = 0x0300UL;
    const ulong CREAD           = 0x0800UL;
    const ulong CLOCAL          = 0x8000UL;
    const ulong IGNPAR          = 0x0004UL;
    const int   CC_SIZE         = 20;
    const int   VTIME_IDX       = 17;
    const int   VMIN_IDX        = 16;
    const int   EINTR           = 4;

    [DllImport("libc", SetLastError = true, EntryPoint = "cfsetispeed")]
    static extern int libc_cfsetispeed(ref TermiosNative t, ulong speed);
    [DllImport("libc", SetLastError = true, EntryPoint = "cfsetospeed")]
    static extern int libc_cfsetospeed(ref TermiosNative t, ulong speed);

    static ulong BaudConst(int baud) => (ulong)baud;

    [StructLayout(LayoutKind.Sequential)]
    struct TermiosNative
    {
        public ulong c_iflag;
        public ulong c_oflag;
        public ulong c_cflag;
        public ulong c_lflag;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public byte[] c_cc;
        public uint   _pad;
        public ulong  c_ispeed;
        public ulong  c_ospeed;
    }

#endif // Linux vs macOS

    sealed class PosixSerialPort : ISerialPort
    {
        int _fd = -1;

        public bool Open(string port, int baud)
        {
            // NOTE: O_NONBLOCK is intentionally NOT set. We want read() to
            // block until at least one byte is available (VMIN=1, VTIME=0).
            // The kernel wakes us as soon as data arrives — zero polling lag.
            _fd = libc_open(port, O_RDWR | O_NOCTTY);
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

            var t = new TermiosNative();
            if (libc_tcgetattr(_fd, ref t) < 0)
            {
                Debug.LogError($"[Serial] tcgetattr failed. errno={Marshal.GetLastWin32Error()}");
                libc_close(_fd); _fd = -1;
                return false;
            }

            libc_cfmakeraw(ref t);

            t.c_cflag = (t.c_cflag & ~CSIZE_MASK) | CS8 | CREAD | CLOCAL;
            t.c_iflag |= IGNPAR;

            if (t.c_cc == null) t.c_cc = new byte[CC_SIZE];
            // VMIN=1, VTIME=0 → blocking read returns as soon as ≥1 byte arrives.
            t.c_cc[VMIN_IDX]  = 1;
            t.c_cc[VTIME_IDX] = 0;

            if (libc_cfsetispeed(ref t, BaudConst(baud)) < 0 ||
                libc_cfsetospeed(ref t, BaudConst(baud)) < 0)
            {
                Debug.LogError($"[Serial] cfsetispeed/cfsetospeed failed. errno={Marshal.GetLastWin32Error()}");
                libc_close(_fd); _fd = -1;
                return false;
            }

            if (libc_tcsetattr(_fd, TCSANOW, ref t) < 0)
            {
                Debug.LogError($"[Serial] tcsetattr failed. errno={Marshal.GetLastWin32Error()}");
                libc_close(_fd); _fd = -1;
                return false;
            }

            Debug.Log($"[Serial] Opened {port} @ {baud} baud (fd={_fd}, libc termios, blocking)");
            return true;
        }

        public int Read(byte[] buf, int max)
        {
            if (_fd < 0) return 0;
            int n = libc_read(_fd, buf, max);
            if (n < 0)
            {
                int err = Marshal.GetLastWin32Error();
                if (err == EINTR) return 0;        // signal interrupted; just retry
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
    //  macOS — native plugin (preferred)
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
            catch (DllNotFoundException) { return false; }
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
    //  Windows — System.IO.Ports, BLOCKING reads via BaseStream
    // ═════════════════════════════════════════════════════════════════════
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    sealed class WindowsSerialPort : ISerialPort
    {
        System.IO.Ports.SerialPort _port;

        public bool Open(string port, int baud)
        {
            try
            {
                _port = new System.IO.Ports.SerialPort(port, baud)
                {
                    ReadTimeout = System.IO.Ports.SerialPort.InfiniteTimeout
                };
                _port.Open();
                Debug.Log($"[Serial] Opened {port} @ {baud} baud (System.IO.Ports, blocking)");
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
                // BaseStream.Read blocks until ≥1 byte is available, exactly
                // like POSIX read with VMIN=1. Avoids the BytesToRead polling
                // pattern that introduces latency on Windows.
                return _port.BaseStream.Read(buf, 0, max);
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
    //  Packet protocol  (0xAA 0x55 | 16 byte name | int32 rotation | byte pushed)
    // ─────────────────────────────────────────────────────────────────────
    const byte Header1     = 0xAA;
    const byte Header2     = 0x55;
    const int  NameLen     = 16;
    const int  PacketSize  = NameLen + 4 + 1;     // 21 bytes
    const int  FullSize    = 2 + PacketSize +1;      // 23 bytes (header + payload)

    // ─────────────────────────────────────────────────────────────────────
    //  Runtime state
    // ─────────────────────────────────────────────────────────────────────
    readonly object _lock      = new object();
    readonly byte[] _readBuf   = new byte[256];
    readonly byte[] _streamBuf = new byte[1024];   // fixed-size, no GC
    int             _streamLen;

    byte[] _player1IdBytes;
    byte[] _player2IdBytes;

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

        // Pre-encode the controller IDs once so the parser can compare
        // raw bytes — no string allocation per packet.
        _player1IdBytes = Encoding.ASCII.GetBytes(player1Id ?? string.Empty);
        _player2IdBytes = Encoding.ASCII.GetBytes(player2Id ?? string.Empty);

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
    //  Background read loop — blocking I/O, no Sleep
    // ─────────────────────────────────────────────────────────────────────
    void ReadLoop()
    {
        while (_running)
        {
            int n = _serial.Read(_readBuf, _readBuf.Length);
            if (n <= 0) continue;   // retryable error or shutdown

            // Append to the parse buffer. If the buffer would overflow
            // (shouldn't happen in practice), drop the oldest unparsed bytes.
            if (_streamLen + n > _streamBuf.Length)
            {
                int drop = (_streamLen + n) - _streamBuf.Length;
                if (drop >= _streamLen) _streamLen = 0;
                else
                {
                    Buffer.BlockCopy(_streamBuf, drop, _streamBuf, 0, _streamLen - drop);
                    _streamLen -= drop;
                }
            }

            Buffer.BlockCopy(_readBuf, 0, _streamBuf, _streamLen, n);
            _streamLen += n;

            ParsePackets();
        }
    }
    
    // ─────────────────────────────────────────────────────────────────────
//  Packet parsing — validates name BEFORE consuming, recovers from
//  false-positive headers (AA 55 inside a previous packet's payload)
//  by advancing only 1 byte instead of FullSize.
// ─────────────────────────────────────────────────────────────────────
    void ParsePackets()
    {
        int read = 0;

        while (read + FullSize <= _streamLen)
        {
            if (_streamBuf[read] != Header1 || _streamBuf[read + 1] != Header2)
            {
                read++;
                continue;
            }

            int payloadStart = read + 2;

            // XOR checksum over the 21-byte payload
            byte cksum = 0;
            for (int i = 0; i < PacketSize; i++)
                cksum ^= _streamBuf[payloadStart + i];

            if (cksum != _streamBuf[payloadStart + PacketSize])
            {
                // Bad checksum — this header is a false positive.
                // Advance ONE byte and keep scanning.
                read++;
                continue;
            }

            bool isP1 = MatchesId(_streamBuf, payloadStart, NameLen, _player1IdBytes);
            bool isP2 = !isP1 && MatchesId(_streamBuf, payloadStart, NameLen, _player2IdBytes);

            if (!isP1 && !isP2)
            {
                read++;
                continue;
            }

            int  rotation = BitConverter.ToInt32(_streamBuf, payloadStart + NameLen);
            bool pushed   = _streamBuf[payloadStart + NameLen + 4] != 0;

            lock (_lock)
            {
                if (isP1)
                {
                    player1.rotation  = rotation;
                    player1.pushed    = pushed;
                    player1.connected = true;
                }
                else
                {
                    player2.rotation  = rotation;
                    player2.pushed    = pushed;
                    player2.connected = true;
                }
            }

            read += FullSize;
        }

        if (read > 0)
        {
            int leftover = _streamLen - read;
            if (leftover > 0)
                Buffer.BlockCopy(_streamBuf, read, _streamBuf, 0, leftover);
            _streamLen = leftover;
        }
    }

    
    

    // ─────────────────────────────────────────────────────────────────────
    //  Packet parsing — zero allocations in the hot path
    // ─────────────────────────────────────────────────────────────────────
    /*void ParsePackets()
    {
        int read = 0;

        while (read + FullSize <= _streamLen)
        {
            if (_streamBuf[read] == Header1 && _streamBuf[read + 1] == Header2)
            {
                DecodePacket(_streamBuf, read + 2);
                read += FullSize;
            }
            else
            {
                read++;   // resync: skip one byte and keep looking
            }
        }

        // Compact: move any unconsumed tail to the front of the buffer.
        if (read > 0)
        {
            int leftover = _streamLen - read;
            if (leftover > 0)
                Buffer.BlockCopy(_streamBuf, read, _streamBuf, 0, leftover);
            _streamLen = leftover;
        }
    }

    void DecodePacket(byte[] buf, int offset)
    {
        
        
        
       /* // Layout: [0..15] name, [16..19] int32 rotation, [20] pushed
        int  rotation = BitConverter.ToInt32(buf, offset + NameLen);
        bool pushed   = buf[offset + NameLen + 4] != 0;
/*
       // TEMP DIAGNOSTIC — remove after debugging
       var sb = new System.Text.StringBuilder();
       for (int i = 0; i < PacketSize; i++) sb.Append(buf[offset + i].ToString("X2") + " ");
       int  rotation = BitConverter.ToInt32(buf, offset + NameLen);
       bool pushed   = buf[offset + NameLen + 4] != 0;
       Debug.Log($"[Pkt] rot={rotation} pushed={pushed} raw={sb}");
       
       
        if (MatchesId(buf, offset, NameLen, _player1IdBytes))
        {
            lock (_lock)
            {
                player1.rotation  = rotation;
                player1.pushed    = pushed;
                player1.connected = true;
            }
        }
        else if (MatchesId(buf, offset, NameLen, _player2IdBytes))
        {
            lock (_lock)
            {
                player2.rotation  = rotation;
                player2.pushed    = pushed;
                player2.connected = true;
            }
        }
        

    }

    */

    static bool MatchesId(byte[] buf, int offset, int fieldLen, byte[] id)
    {
        if (id == null || id.Length > fieldLen) return false;
        for (int i = 0; i < id.Length; i++)
            if (buf[offset + i] != id[i]) return false;
        // Field must be either exactly the ID, or null-terminated right after it.
        if (id.Length < fieldLen && buf[offset + id.Length] != 0) return false;
        return true;
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

    public ControllerState GetInput(int ID) => ID == 1 ? player1 : player2;

    // ─────────────────────────────────────────────────────────────────────
    //  PlayerInputManager wiring
    // ─────────────────────────────────────────────────────────────────────
    private PlayerInputManager inputManager;

    void Update()
    {
        if (inputManager.playerCount == 0)
        {
            if (player1.connected) inputManager.JoinPlayer();
        }
        else if (inputManager.playerCount == 1)
        {
            if (player2.connected) inputManager.JoinPlayer();
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Shutdown
    // ─────────────────────────────────────────────────────────────────────
    void OnDisable()         => Shutdown();
    void OnApplicationQuit() => Shutdown();

    void Shutdown()
    {
        _running = false;
        try { _serial?.Close(); } catch { /* ignore */ }
        // Closing the fd unblocks the pending read() so the thread exits.
        _thread?.Join(500);
        _serial = null;
    }
}
