using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

// Compiled by Windows PowerShell on the ephemeral CI runner, not shipped in the app.
public static class UnelevatedProcess {
  [StructLayout(LayoutKind.Sequential)]
  private struct SidAndAttributes { public IntPtr Sid; public uint Attributes; }
  [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
  private struct StartupInfo {
    public int Size; public string Reserved; public string Desktop; public string Title;
    public uint X, Y, XSize, YSize, XCountChars, YCountChars, FillAttribute, Flags;
    public short ShowWindow, Reserved2Size; public IntPtr Reserved2, StdInput, StdOutput, StdError;
  }
  [StructLayout(LayoutKind.Sequential)]
  private struct ProcessInfo { public IntPtr Process, Thread; public uint ProcessId, ThreadId; }
  [DllImport("advapi32.dll", SetLastError = true)]
  private static extern bool OpenProcessToken(IntPtr process, uint access, out IntPtr token);
  [DllImport("advapi32.dll", SetLastError = true)]
  private static extern bool CreateRestrictedToken(IntPtr token, uint flags, uint count, ref SidAndAttributes disabled, uint deleteCount, IntPtr deleted, uint restrictedCount, IntPtr restricted, out IntPtr result);
  [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
  private static extern bool ConvertStringSidToSid(string text, out IntPtr sid);
  [DllImport("advapi32.dll", SetLastError = true)]
  private static extern bool SetTokenInformation(IntPtr token, int informationClass, ref SidAndAttributes information, int size);
  [DllImport("advapi32.dll")]
  private static extern uint GetLengthSid(IntPtr sid);
  [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
  private static extern bool CreateProcessWithTokenW(IntPtr token, uint logonFlags, string application, StringBuilder command, uint flags, IntPtr environment, string directory, ref StartupInfo startup, out ProcessInfo process);
  [DllImport("kernel32.dll")]
  private static extern IntPtr GetCurrentProcess();
  [DllImport("kernel32.dll")]
  private static extern bool CloseHandle(IntPtr handle);
  [DllImport("kernel32.dll")]
  private static extern IntPtr LocalFree(IntPtr memory);
  [DllImport("kernel32.dll")]
  private static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);
  [DllImport("kernel32.dll", SetLastError = true)]
  private static extern bool GetExitCodeProcess(IntPtr handle, out uint code);
  private static void Check(bool success) {
    if (!success) { throw new Win32Exception(Marshal.GetLastWin32Error()); }
  }
  public static void Run(string application, string commandLine, string directory) {
    IntPtr original = IntPtr.Zero, reduced = IntPtr.Zero, adminSid = IntPtr.Zero, mediumSid = IntPtr.Zero;
    var process = new ProcessInfo();
    try {
      Check(OpenProcessToken(GetCurrentProcess(), 0xF01FF, out original));
      Check(ConvertStringSidToSid("S-1-5-32-544", out adminSid));
      var disabled = new SidAndAttributes { Sid = adminSid };
      // Disable administrator membership and privileges. No policy-bypass flags.
      Check(CreateRestrictedToken(original, 0x5, 1, ref disabled, 0, IntPtr.Zero, 0, IntPtr.Zero, out reduced));
      Check(ConvertStringSidToSid("S-1-16-8192", out mediumSid));
      var integrity = new SidAndAttributes { Sid = mediumSid, Attributes = 0x20 };
      Check(SetTokenInformation(reduced, 25, ref integrity, Marshal.SizeOf(typeof(SidAndAttributes)) + (int)GetLengthSid(mediumSid)));
      var startup = new StartupInfo { Size = Marshal.SizeOf(typeof(StartupInfo)) };
      Check(CreateProcessWithTokenW(reduced, 0, application, new StringBuilder(commandLine), 0x08000000, IntPtr.Zero, directory, ref startup, out process));
      if (WaitForSingleObject(process.Process, 30000) != 0) { throw new TimeoutException("Unelevated activation controller timed out."); }
      uint exitCode;
      Check(GetExitCodeProcess(process.Process, out exitCode));
      if (exitCode != 0) { throw new InvalidOperationException("Unelevated activation controller exited with code " + exitCode); }
    } finally {
      if (process.Thread != IntPtr.Zero) { CloseHandle(process.Thread); }
      if (process.Process != IntPtr.Zero) { CloseHandle(process.Process); }
      if (reduced != IntPtr.Zero) { CloseHandle(reduced); }
      if (original != IntPtr.Zero) { CloseHandle(original); }
      if (mediumSid != IntPtr.Zero) { LocalFree(mediumSid); }
      if (adminSid != IntPtr.Zero) { LocalFree(adminSid); }
    }
  }
}
