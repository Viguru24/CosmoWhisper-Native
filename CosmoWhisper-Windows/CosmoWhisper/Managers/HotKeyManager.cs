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
            try 
            {
                if (nCode >= 0)
                {
                    int vkCode = Marshal.ReadInt32(lParam);
                    
                    // DEBUG: Log *attempt* for F9 (120) specifically to see if hook is alive
                    if (vkCode == 120 || vkCode == (int)_currentVk) 
                    {
                        // LogEvent($"[Hook-Debug] Saw VK {vkCode} Action {wParam}");
                    }

                    if (vkCode == (int)_currentVk)
                    {
                        if (wParam == (IntPtr)WM_KEYDOWN)
                        {
                            if (!_isKeyCurrentlyPressed)
                            {
                                _isKeyCurrentlyPressed = true;
                                LogEvent($"[HotKey] MATCHED VK {vkCode} DOWN - Triggering Action");
                                KeyPressed?.Invoke();
                            }
                        }
                        else if (wParam == (IntPtr)WM_KEYUP)
                        {
                            _isKeyCurrentlyPressed = false;
                            LogEvent($"[HotKey] MATCHED VK {vkCode} UP");
                            KeyReleased?.Invoke();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogEvent($"[Hook-Error] {ex.Message}");
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


