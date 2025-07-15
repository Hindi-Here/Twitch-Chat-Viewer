using AutoMapper;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace TwitchChatView
{
    internal static class Win32
    {
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_NOACTIVATE = 0x08000000;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_LAYERED = 0x00080000;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_ASYNCWINDOWPOS = 0x4000;

        [StructLayout(LayoutKind.Sequential)]
        private struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out Rect lpRect);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
                                             int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        public static void TopmostBehavior(Window window)
        {
            window.SourceInitialized += (s, e) =>
            {
                SetupWindowBehavior(window);
                Task.Delay(100).ContinueWith(_ =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        UpdateWindowZOrder(window);
                    });
                });
            };

            window.Activated += (s, e) => UpdateWindowZOrder(window);
        }

        private static void SetupWindowBehavior(Window window)
        {
            var helper = new WindowInteropHelper(window);
            IntPtr hWnd = helper.EnsureHandle();

            int extendedStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
            _ = SetWindowLong(hWnd, GWL_EXSTYLE,
                        extendedStyle | WS_EX_NOACTIVATE | WS_EX_TRANSPARENT |
                        WS_EX_TOOLWINDOW | WS_EX_LAYERED);

            window.Focusable = false;
            window.IsHitTestVisible = false;

            UpdateWindowZOrder(window);
        }

        private static void UpdateWindowZOrder(Window window)
        {
            var helper = new WindowInteropHelper(window);
            IntPtr hWnd = helper.EnsureHandle();
            IntPtr hTaskbar = FindWindow("Shell_TrayWnd", null!);

            if (hTaskbar != IntPtr.Zero)
            {
                SetWindowPos(hWnd, hTaskbar, 0, 0, 0, 0,
                            SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE | SWP_ASYNCWINDOWPOS);
            }

            SetWindowPos(hWnd, new IntPtr(-1), 0, 0, 0, 0,
                        SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE | SWP_ASYNCWINDOWPOS);

            if (hTaskbar != IntPtr.Zero)
            {
                SetWindowPos(hTaskbar, new IntPtr(-1), 0, 0, 0, 0,
                            SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE | SWP_ASYNCWINDOWPOS);
            }
        }

    }
}