using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using System.Runtime.InteropServices;

class ZorxConsoleBox
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private static LowLevelKeyboardProc _proc = HookCallback;
    private static IntPtr _hookID = IntPtr.Zero;

    private static Process[] pidList = null;
    private static Process fallbackWindow = null;

    public static void Main()
    {
        pidList = initMinionApps();
        fallbackWindow = Process.GetProcessesByName("ZorxConsoleBox.vshost")[0];
        SetForegroundWindow(fallbackWindow.MainWindowHandle);

        _hookID = SetHook(_proc);
        Application.Run();        
        UnhookWindowsHookEx(_hookID);

    }

    private static Process[] initMinionApps()
    {
        // Populate list containing Processes we want to inject keys into.
        Process[] wowList = Process.GetProcessesByName("notepad");
        foreach (Process item in wowList)
        {
            Console.WriteLine(item.Id);
        }
        return wowList;
    }

    private static void sendKeyToProcess(Keys key, Process p)
    {
        IntPtr recvHandle = p.MainWindowHandle;
        SetForegroundWindow(recvHandle);
        SendKeys.SendWait(key.ToString());
        SetForegroundWindow(fallbackWindow.MainWindowHandle);
    }



    private static IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        using (Process curProcess = Process.GetCurrentProcess())
        using (ProcessModule curModule = curProcess.MainModule)
        {
            return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                GetModuleHandle(curModule.ModuleName), 0);
        }
    }

    private delegate IntPtr LowLevelKeyboardProc(
        int nCode, IntPtr wParam, IntPtr lParam);

    private static IntPtr HookCallback(
        int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
        {
            int vkCode = Marshal.ReadInt32(lParam);
            foreach (Process item in pidList)
            {
                sendKeyToProcess((Keys)vkCode, item);
                Thread.Sleep(5);
                
            }
            Console.WriteLine((Keys)vkCode);
        }
        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    }

    //[DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
    //private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("User32.dll")]
    static extern int SetForegroundWindow(IntPtr point);

    [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook,
        LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
        IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);
}