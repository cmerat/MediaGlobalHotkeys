using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

using WindowsInput;

namespace MediaGlobalHotkeys
{
    public class Program : ApplicationContext
    {
        private const string FirefoxProcessName = "firefox";

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;

        private readonly Win32.LowLevelKeyboardProc keyboard_proc;
        private readonly IntPtr keyboard_hook;

        private bool is_control_key_down;
        private bool is_alt_key_down;

        public Program()
        {
            try
            {
                this.keyboard_proc = new Win32.LowLevelKeyboardProc(this.KeyboardProc);
                using (var currentProcess = Process.GetCurrentProcess())
                {
                    using (var mainModule = currentProcess.MainModule)
                    {
                        this.keyboard_hook = Win32.SetWindowsHookEx(WH_KEYBOARD_LL, this.keyboard_proc, Win32.GetModuleHandle(mainModule.ModuleName), 0u);
                    }
                }
            }
            catch
            {
                if (this.keyboard_hook != IntPtr.Zero)
                {
                    Win32.UnhookWindowsHookEx(this.keyboard_hook);
                }

                throw;
            }
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                using (var program = new Program())
                {
                    Icon icon;

                    using (var s = typeof(Program).Assembly.GetManifestResourceStream("MediaGlobalHotkeys.juk.ico"))
                    {
                        icon = new Icon(s);
                    }

                    var tray = new NotifyIcon
                        {
                            ContextMenu = new ContextMenu(),
                            Text = "Media Global Hotkeys",
                            Icon = icon,
                        };

                    var exit = new MenuItem("Exit");
                    exit.Click += (sender, e) =>
                        {
                            tray.Visible = false;
                            Application.Exit();
                        };

                    tray.ContextMenu.MenuItems.Add(exit);

                    tray.Visible = true;

                    Application.Run(program);
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private IntPtr KeyboardProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int virtualKey = Marshal.ReadInt32(lParam);
                int num = wParam.ToInt32();
                Key key = KeyInterop.KeyFromVirtualKey(virtualKey);
                bool flag = num == WM_KEYDOWN || num == WM_SYSKEYDOWN;
                if (key == Key.LeftCtrl || key == Key.RightCtrl)
                {
                    this.is_control_key_down = flag;
                }
                else if (key == Key.LeftAlt)
                {
                    this.is_alt_key_down = flag;
                }
                else if (flag)
                {
                    Debug.WriteLine($"Key: {key} flag: {flag}");

                    if (this.is_alt_key_down && this.is_control_key_down)
                    {
                        switch (key)
                        {
                            case Key.Home:
                                InputSimulator.SimulateKeyDown(VirtualKeyCode.MEDIA_PLAY_PAUSE);
                                return new IntPtr(1);

                            case Key.End:
                                InputSimulator.SimulateKeyDown(VirtualKeyCode.MEDIA_STOP);
                                return new IntPtr(1);

                            case Key.PageUp:
                                InputSimulator.SimulateKeyDown(VirtualKeyCode.MEDIA_PREV_TRACK);
                                return new IntPtr(1);

                            case Key.PageDown:
                                InputSimulator.SimulateKeyDown(VirtualKeyCode.MEDIA_NEXT_TRACK);
                                return new IntPtr(1);

                            case Key.Up:
                                InputSimulator.SimulateKeyDown(VirtualKeyCode.VOLUME_UP);
                                return new IntPtr(1);

                            case Key.Down:
                                InputSimulator.SimulateKeyDown(VirtualKeyCode.VOLUME_DOWN);
                                return new IntPtr(1);
                        }
                    }
                    
                    // send mediakey press messages to specific process windows directly
                    switch (key)
                    {
                        case Key.MediaPlayPause:
                        case Key.MediaStop:
                        case Key.MediaPreviousTrack:
                        case Key.MediaNextTrack:
                            SendToProcessIfNeeded(FirefoxProcessName, key, lParam);
                            break;
                    }
                }
            }

            return Win32.CallNextHookEx(this.keyboard_hook, nCode, wParam, lParam);
        }

        private static bool SendToProcessIfNeeded(string processName, Key key, IntPtr lParam)
        {
            var hWnd = GetProcessHandle(processName);

            var shouldSend = Win32.GetForegroundWindow() != hWnd;
            if (hWnd != IntPtr.Zero && shouldSend)
            {
                switch (key)
                {
                    case Key.MediaPlayPause:
                        SendKeyMessageToWindow(VirtualKeyCode.MEDIA_PLAY_PAUSE, hWnd, lParam);
                        return true;

                    case Key.MediaStop:
                        SendKeyMessageToWindow(VirtualKeyCode.MEDIA_STOP, hWnd, lParam);
                        return true;

                    case Key.MediaPreviousTrack:
                        SendKeyMessageToWindow(VirtualKeyCode.MEDIA_PREV_TRACK, hWnd, lParam);
                        return true;

                    case Key.MediaNextTrack:
                        SendKeyMessageToWindow(VirtualKeyCode.MEDIA_NEXT_TRACK, hWnd, lParam);
                        return true;
                }
            }

            return false;
        }

        private static IntPtr GetProcessHandle(string processName)
        {
            var proc = Process.GetProcessesByName(processName);

            var wnd = proc.Where(x => x.MainWindowHandle != IntPtr.Zero).Select(x => x.MainWindowHandle);

            return wnd.FirstOrDefault();
        }

        private static void SendKeyMessageToWindow(VirtualKeyCode key, IntPtr hWnd, IntPtr lParam)
        {
            if (hWnd != IntPtr.Zero)
            {
                ThreadPool.QueueUserWorkItem(x =>
                {
                    // need to sleep before sending WM or the message will fail
                    Thread.Sleep(200);
                    Win32.SendMessage(hWnd, Win32.WM_KEYDOWN, new IntPtr((int)key), lParam);

                    Debug.WriteLine($"Sent message: {key} to hwnd {hWnd}");
                });
            }
        }
    }
}
