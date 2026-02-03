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

        public async Task PasteText(string text, bool autoSubmit = false, bool restoreClipboard = true)
        {
            string? oldText = null;
            if (restoreClipboard)
            {
                // Must be on UI thread for System.Windows.Clipboard access
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    if (System.Windows.Clipboard.ContainsText())
                        oldText = System.Windows.Clipboard.GetText();
                });
            }

            System.Windows.Application.Current.Dispatcher.Invoke(() => {
                System.Windows.Clipboard.SetText(text);
            });

            // Small delay to allow clipboard to sync
            await Task.Delay(100);

            // Ctrl + V
            SendModifierKey(VK_CONTROL, false);
            SendKey(0x56, false); // V
            SendKey(0x56, true);
            SendModifierKey(VK_CONTROL, true);

            if (autoSubmit)
            {
                await Task.Delay(150);
                SendKey(0x0D, false); // Enter
                SendKey(0x0D, true);
            }

            if (restoreClipboard && oldText != null)
            {
                await Task.Delay(500);
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    System.Windows.Clipboard.SetText(oldText);
                });
            }
        }

        public async Task TypeText(string text, bool autoSubmit)
        {
            // Real Direct Typing using SendInput with Unicode support
            foreach (char c in text)
            {
                INPUT[] inputs = new INPUT[2];
                
                // Key Down
                inputs[0].type = INPUT_KEYBOARD;
                inputs[0].ki.wVk = 0;
                inputs[0].ki.wScan = (ushort)c;
                inputs[0].ki.dwFlags = 0x0004; // KEYEVENTF_UNICODE
                
                // Key Up
                inputs[1].type = INPUT_KEYBOARD;
                inputs[1].ki.wVk = 0;
                inputs[1].ki.wScan = (ushort)c;
                inputs[1].ki.dwFlags = 0x0004 | 0x0002; // KEYEVENTF_UNICODE | KEYEVENTF_KEYUP
                
                SendInput(2, inputs, Marshal.SizeOf(typeof(INPUT)));
                
                // Small delay to simulate natural typing if needed, 
                // but usually instant is fine for "Direct Typing" mode
                // await Task.Delay(1); 
            }

            if (autoSubmit)
            {
                await Task.Delay(50);
                SendKey(0x0D, false); // Enter
                SendKey(0x0D, true);
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
            public int type;
            public KEYBDINPUT ki;
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
    }
}
