using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

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

        public event Action? KeyPressed;
        public event Action? KeyReleased;
        public event Action<string>? ErrorOccurred;

        public void Register(Window window, uint vkCode)
        {
            try
            {
                _currentVk = vkCode;
                Debug.WriteLine($"[HotKey] Setting up for VK {vkCode}...");

                if (_hookID != IntPtr.Zero)
                {
                    UnhookWindowsHookEx(_hookID);
                }

                _proc = HookCallback;
                _hookID = SetHook(_proc);

                if (_hookID == IntPtr.Zero)
                {
                    ErrorOccurred?.Invoke($"Key hook failed for VK {vkCode}. Use mic button instead.");
                }
                else
                {
                    Debug.WriteLine($"[HotKey] Enabled for VK {vkCode}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HotKey] Exception: {ex.Message}");
                ErrorOccurred?.Invoke($"Hook Error: {ex.Message}");
            }
        }

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(null), 0);
        }


        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int vkCode = Marshal.ReadInt32(lParam);

                if (vkCode == (int)_currentVk)
                {
                    if (wParam == (IntPtr)WM_KEYDOWN) KeyPressed?.Invoke();
                    else if (wParam == (IntPtr)WM_KEYUP) KeyReleased?.Invoke();
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
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
