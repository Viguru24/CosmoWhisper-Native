using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.IO;

namespace CosmoWhisper.Managers
{
    public class HotKeyManager : IDisposable
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string? lpModuleName);

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private LowLevelKeyboardProc? _proc;
        private IntPtr _hookID = IntPtr.Zero;
        private uint _currentVk = 0;
        private bool _isKeyCurrentlyPressed = false; // Track key state to ignore repeats

        public event Action? KeyPressed;
        public event Action? KeyReleased;
        public event Action<string>? ErrorOccurred;

        public void Register(Window window, uint vkCode)
        {
            try
            {
                _currentVk = vkCode;
                _isKeyCurrentlyPressed = false; // Reset state when changing keys
                LogEvent($"[HotKey] Setting up for VK {vkCode}...");

                if (_hookID != IntPtr.Zero)
                {
                    UnhookWindowsHookEx(_hookID);
                }

                _proc = HookCallback;
                _hookID = SetHook(_proc);

                if (_hookID == IntPtr.Zero)
                {
                    LogEvent($"[HotKey] Key hook FAILED for VK {vkCode}. Error Code: {Marshal.GetLastWin32Error()}");
                    ErrorOccurred?.Invoke($"Key hook failed for VK {vkCode}. Use mic button instead.");
                }
                else
                {
                    LogEvent($"[HotKey] Enabled for VK {vkCode} (HookID: {_hookID})");
                }
            }
            catch (Exception ex)
            {
                LogEvent($"[HotKey] Exception: {ex.Message}");
                ErrorOccurred?.Invoke($"Hook Error: {ex.Message}");
            }
        }

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule!)
            {
                // For SingleFile apps, GetModuleHandle(null) is safer to get the EXE handle
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(null), 0);
            }
        }


        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode < 0) return CallNextHookEx(_hookID, nCode, wParam, lParam);

            int vkCode = Marshal.ReadInt32(lParam);
            if (vkCode == (int)_currentVk)
            {
                // Process all Down events (including auto-repeats and Alt+System keys)
                if (wParam == (IntPtr)0x0100 || wParam == (IntPtr)0x0104) // WM_KEYDOWN, WM_SYSKEYDOWN
                {
                    if (!_isKeyCurrentlyPressed)
                    {
                        _isKeyCurrentlyPressed = true;
                        // Fire-off action on background thread instantly
                        System.Threading.ThreadPool.QueueUserWorkItem(_ => KeyPressed?.Invoke());
                    }
                    return (IntPtr)1; // Consume message immediately to prevent leakage
                }
                // Process all Up events
                else if (wParam == (IntPtr)0x0101 || wParam == (IntPtr)0x0105) // WM_KEYUP, WM_SYSKEYUP
                {
                    _isKeyCurrentlyPressed = false;
                    System.Threading.ThreadPool.QueueUserWorkItem(_ => KeyReleased?.Invoke());
                    return (IntPtr)1; // Consume message immediately
                }

                // Total lockdown for this key - suppress any other WM messages associated with it
                return (IntPtr)1;
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private void LogEvent(string msg)
        {
            try
            {
                string logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CosmoWhisper", "logs");
                Directory.CreateDirectory(logDir);
                string logPath = Path.Combine(logDir, "cosmo_debug.log");
                File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {msg}\n");
            }
            catch { }
        }

        public void Dispose()
        {
            if (_hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
            }
        }
    }
}


