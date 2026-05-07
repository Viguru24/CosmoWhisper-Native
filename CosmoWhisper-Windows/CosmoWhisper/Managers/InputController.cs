using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace CosmoWhisper.Managers
{
    public class InputController
    {
        public static InputController Shared { get; } = new InputController();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

        private const int INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const byte VK_CONTROL = 0x11;
        private const byte VK_SHIFT = 0x10;
        private const byte VK_MENU = 0x12; // Alt
        private const byte VK_LWIN = 0x5B;

        private string? SafeGetClipboardText()
        {
            if (System.Windows.Application.Current == null) return null;
            
            string? result = null;
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                for (int i = 0; i < 5; i++)
                {
                    try
                    {
                        if (System.Windows.Clipboard.ContainsText())
                        {
                            result = System.Windows.Clipboard.GetText();
                            break;
                        }
                    }
                    catch (COMException) { Thread.Sleep(50); }
                    catch (Exception) { break; }
                }
            });
            return result;
        }

        public void SafeSetClipboardText(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    System.Windows.Clipboard.SetText(text);
                    return;
                }
                catch (COMException) { Thread.Sleep(50); }
                catch (Exception) { break; }
            }
        }

        public async Task<string> CaptureSelectedText()
        {
            // 1. Clear clipboard on UI thread
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                try { System.Windows.Clipboard.Clear(); } catch { }
            });

            // 2. Send Ctrl+C via Win32 (Thread-safe)
            ExecuteKeystroke("c", ctrl: true);

            // 3. Wait and check
            string captured = "";
            for (int i = 0; i < 6; i++) // Check for up to 1.5 seconds
            {
                await Task.Delay(250);

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        if (System.Windows.Clipboard.ContainsText())
                            captured = System.Windows.Clipboard.GetText();
                    }
                    catch { }
                });

                if (!string.IsNullOrWhiteSpace(captured)) return captured;
            }

            return "";
        }

        public async Task PasteText(string text, bool autoSubmit = false, bool restoreClipboard = true)
        {
            string? oldText = null;
            if (restoreClipboard)
            {
                // Must be on UI thread for System.Windows.Clipboard access
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    oldText = SafeGetClipboardText();
                });
            }

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                SafeSetClipboardText(text);
            });

            // Small delay to allow clipboard to sync (Reduced from 100ms for speed)
            await Task.Delay(50);

            // Ctrl + V
            SendModifierKey(VK_CONTROL, false);
            SendKey(0x56, false); // V
            SendKey(0x56, true);
            SendModifierKey(VK_CONTROL, true);

            if (autoSubmit)
            {
                await Task.Delay(100); // Reduced from 150ms
                SendKey(0x0D, false); // Enter
                SendKey(0x0D, true);
            }

            if (restoreClipboard && oldText != null)
            {
                await Task.Delay(200); // Reduced from 500ms
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    SafeSetClipboardText(oldText);
                });
            }
        }



        public void SendKey(byte virtualKey, bool keyUp)
        {
            keybd_event(virtualKey, 0, keyUp ? KEYEVENTF_KEYUP : 0, 0);
        }

        public void SendModifierKey(byte virtualKey, bool keyUp)
        {
            keybd_event(virtualKey, 0, keyUp ? KEYEVENTF_KEYUP : 0, 0);
        }

        public void ExecuteKeystroke(string key, bool ctrl = false, bool shift = false, bool alt = false, bool win = false)
        {
            byte vk = GetVirtualKey(key);
            if (vk == 0) return;

            if (ctrl) SendModifierKey(VK_CONTROL, false);
            if (shift) SendModifierKey(VK_SHIFT, false);
            if (alt) SendModifierKey(VK_MENU, false);
            if (win) SendModifierKey(VK_LWIN, false);

            SendKey(vk, false);
            SendKey(vk, true);

            if (win) SendModifierKey(VK_LWIN, true);
            if (alt) SendModifierKey(VK_MENU, true);
            if (shift) SendModifierKey(VK_SHIFT, true);
            if (ctrl) SendModifierKey(VK_CONTROL, true);
        }

        private byte GetVirtualKey(string key)
        {
            return (key.ToLower()) switch
            {
                "a" => 0x41,
                "c" => 0x43,
                "v" => 0x56,
                "x" => 0x58,
                "z" => 0x5A,
                "y" => 0x59,
                "s" => 0x53,
                "f" => 0x46,
                "b" => 0x42,
                "i" => 0x49,
                "u" => 0x55,
                "return" or "enter" => 0x0D,
                "backspace" or "delete" => 0x08,
                "tab" => 0x09,
                "esc" or "escape" => 0x1B,
                "space" => 0x20,
                _ => 0
            };
        }

        // Structs for SendInput (more precise than keybd_event but overhead is higher)
        [StructLayout(LayoutKind.Sequential)]
        struct INPUT
        {
            public uint type;
            public InputUnion U;
            public static int Size => Marshal.SizeOf(typeof(INPUT));
        }

        [StructLayout(LayoutKind.Explicit)]
        struct InputUnion
        {
            [FieldOffset(0)]
            public MOUSEINPUT mi;
            [FieldOffset(0)]
            public KEYBDINPUT ki;
            [FieldOffset(0)]
            public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        public async Task TypeText(string text, bool autoSubmit)
        {
            // Real Direct Typing using SendInput with Unicode support
            foreach (char c in text)
            {
                INPUT[] inputs = new INPUT[2];

                // Key Down
                inputs[0].type = INPUT_KEYBOARD;
                inputs[0].U.ki.wVk = 0;
                inputs[0].U.ki.wScan = (ushort)c;
                inputs[0].U.ki.dwFlags = 0x0004; // KEYEVENTF_UNICODE

                // Key Up
                inputs[1].type = INPUT_KEYBOARD;
                inputs[1].U.ki.wVk = 0;
                inputs[1].U.ki.wScan = (ushort)c;
                inputs[1].U.ki.dwFlags = 0x0004 | 0x0002; // KEYEVENTF_UNICODE | KEYEVENTF_KEYUP

                SendInput(2, inputs, INPUT.Size);

                // Small delay to simulate natural typing and prevent UI freeze
                await Task.Delay(1);
            }

            if (autoSubmit)
            {
                await Task.Delay(50);
                SendKey(0x0D, false); // Enter
                SendKey(0x0D, true);
            }
        }
    }
}


