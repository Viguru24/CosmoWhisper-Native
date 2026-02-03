using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace CosmoWhisper.Managers
{
    public class MouseButtonManager : IDisposable
    {
        private const int WH_MOUSE_LL = 14;
        private const int WM_MBUTTONDOWN = 0x0207;
        private const int WM_MBUTTONUP = 0x0208;
        private const int WM_XBUTTONDOWN = 0x020B;
        private const int WM_XBUTTONUP = 0x020C;

        private LowLevelMouseProc _proc;
        private IntPtr _hookID = IntPtr.Zero;
        private string _configuredButton = "None";

        public event Action? ButtonPressed;
        public event Action? ButtonReleased;
        public event Action<string>? ErrorOccurred;

        public MouseButtonManager()
        {
            _proc = HookCallback;
        }

        public void Register(string buttonName)
        {
            try
            {
                Unregister();
                _configuredButton = buttonName;

                if (buttonName != "None" && !string.IsNullOrEmpty(buttonName))
                {
                    _hookID = SetHook(_proc);
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Failed to register mouse button: {ex.Message}");
            }
        }

        public void Unregister()
        {
            if (_hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
            }
        }

        private IntPtr SetHook(LowLevelMouseProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule? curModule = curProcess.MainModule)
            {
                if (curModule != null)
                {
                    return SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
                }
            }
            return IntPtr.Zero;
        }

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int mouseData = Marshal.ReadInt32(lParam, 8);
                int xButton = (mouseData >> 16) & 0xFFFF;

                bool isPress = false;
                bool isRelease = false;
                string detectedButton = "";

                // Middle button
                if (wParam == (IntPtr)WM_MBUTTONDOWN)
                {
                    isPress = true;
                    detectedButton = "Middle";
                }
                else if (wParam == (IntPtr)WM_MBUTTONUP)
                {
                    isRelease = true;
                    detectedButton = "Middle";
                }
                // XButtons (side buttons)
                else if (wParam == (IntPtr)WM_XBUTTONDOWN)
                {
                    isPress = true;
                    detectedButton = xButton == 1 ? "XButton1" : "XButton2";
                }
                else if (wParam == (IntPtr)WM_XBUTTONUP)
                {
                    isRelease = true;
                    detectedButton = xButton == 1 ? "XButton1" : "XButton2";
                }

                // Check if this is the configured button
                if (detectedButton == _configuredButton)
                {
                    if (isPress)
                    {
                        ButtonPressed?.Invoke();
                    }
                    else if (isRelease)
                    {
                        ButtonReleased?.Invoke();
                    }
                }
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            Unregister();
        }

        #region Win32 API
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
        #endregion
    }
}
