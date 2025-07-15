using System;
using System.Runtime.InteropServices;
using System.Security;
using Microsoft.Extensions.Logging;
using UiDesktopApp2.Helpers;

namespace UiDesktopApp2.Services
{
    public class WindowsCredentialManager : ICredentialManager
    {
        private readonly ILogger<WindowsCredentialManager> _logger;
        private const string CREDENTIAL_PREFIX = "UiDesktopApp2_";

        public WindowsCredentialManager(ILogger<WindowsCredentialManager> logger)
        {
            _logger = logger;
        }

        public void SavePassword(string key, string userName, SecureString password)
        {
            IntPtr passwordPtr = IntPtr.Zero;
            try
            {
                var credentialKey = CREDENTIAL_PREFIX + key;
                passwordPtr = Marshal.SecureStringToCoTaskMemUnicode(password);
                var passwordString = Marshal.PtrToStringUni(passwordPtr);

                var credential = new NativeMethods.CREDENTIAL
                {
                    TargetName = credentialKey,
                    UserName = userName,
                    CredentialBlob = passwordString,
                    CredentialBlobSize = (uint)(passwordString.Length * 2),
                    Persist = NativeMethods.CRED_PERSIST_LOCAL_MACHINE,
                    Type = NativeMethods.CRED_TYPE_GENERIC
                };

                if (!NativeMethods.CredWrite(ref credential, 0))
                {
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                }

                _logger.LogInformation("Saved credential for {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save credential for {Key}", key);
                throw;
            }
            finally
            {
                if (passwordPtr != IntPtr.Zero)
                {
                    Marshal.ZeroFreeCoTaskMemUnicode(passwordPtr);
                }
            }
        }

        public SecureString GetPassword(string key)
        {
            IntPtr credPtr = IntPtr.Zero;
            try
            {
                var credentialKey = CREDENTIAL_PREFIX + key;
                if (!NativeMethods.CredRead(credentialKey, NativeMethods.CRED_TYPE_GENERIC, 0, out credPtr))
                {
                    return new SecureString(); // Return empty if not found
                }

                using (var credentialHandle = new NativeMethods.CriticalCredentialHandle(credPtr))
                {
                    var cred = credentialHandle.GetCredential();
                    var securePassword = new SecureString();
                    foreach (var c in cred.CredentialBlob)
                    {
                        securePassword.AppendChar(c);
                    }
                    securePassword.MakeReadOnly();
                    return securePassword;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get credential for {Key}", key);
                return new SecureString();
            }
        }

        public void DeletePassword(string key)
        {
            try
            {
                var credentialKey = CREDENTIAL_PREFIX + key;
                if (!NativeMethods.CredDelete(credentialKey, NativeMethods.CRED_TYPE_GENERIC, 0))
                {
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                }
                _logger.LogInformation("Deleted credential for {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete credential for {Key}", key);
                throw;
            }
        }

        private static class NativeMethods
        {
            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            public struct CREDENTIAL
            {
                public uint Flags;
                public uint Type;
                public string TargetName;
                public string Comment;
                public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
                public uint CredentialBlobSize;
                public string CredentialBlob;
                public uint Persist;
                public uint AttributeCount;
                public IntPtr Attributes;
                public string TargetAlias;
                public string UserName;
            }

            public const uint CRED_TYPE_GENERIC = 1;
            public const uint CRED_PERSIST_LOCAL_MACHINE = 2;

            [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            public static extern bool CredWrite([In] ref CREDENTIAL userCredential, [In] uint flags);

            [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            public static extern bool CredRead(string target, uint type, uint flags, out IntPtr credential);

            [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            public static extern bool CredDelete(string target, uint type, uint flags);

            [DllImport("advapi32.dll", SetLastError = true)]
            public static extern bool CredFree([In] IntPtr cred);

            public class CriticalCredentialHandle : Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid
            {
                public CriticalCredentialHandle(IntPtr preexistingHandle) : base(true)
                {
                    SetHandle(preexistingHandle);
                }

                public CREDENTIAL GetCredential()
                {
                    if (!IsInvalid)
                    {
                        return Marshal.PtrToStructure<CREDENTIAL>(handle);
                    }
                    throw new InvalidOperationException("Invalid CREDENTIAL handle");
                }

                protected override bool ReleaseHandle()
                {
                    if (!IsInvalid)
                    {
                        CredFree(handle);
                        SetHandleAsInvalid();
                        return true;
                    }
                    return false;
                }
            }
        }
    }
}
