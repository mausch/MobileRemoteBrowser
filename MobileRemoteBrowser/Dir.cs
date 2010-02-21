using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using Microsoft.Win32.SafeHandles;

namespace ServerFileBrowser {
    // From http://weblogs.asp.net/podwysocki/archive/2008/10/16/functional-net-fighting-friction-in-the-bcl-with-directory-getfiles.aspx
    public class Dir {
        public static IEnumerable<string> GetDirectories(string directory) {
            return GetAll(directory)
                .Where(d => (d.dwFileAttributes & FileAttributes.Directory) != 0)
                .Select(d => Path.Combine(directory, d.cFileName));
        }

        public static IEnumerable<string> GetFiles(string directory) {
            return GetAll(directory)
                .Where(d => (d.dwFileAttributes & FileAttributes.Directory) == 0)
                .Select(d => Path.Combine(directory, d.cFileName));
        }

        private static IEnumerable<NativeMethods.WIN32_FIND_DATA> GetAll(string directory) {
            var findData = new NativeMethods.WIN32_FIND_DATA();
            using (SafeFindHandle findHandle = NativeMethods.FindFirstFile(directory + @"\*", findData)) {
                if (!findHandle.IsInvalid) {
                    do {
                        if (findData.cFileName != "." && findData.cFileName != "..")
                            yield return findData;
                    } while (NativeMethods.FindNextFile(findHandle, findData));
                }
            }
        }

        #region Nested type: NativeMethods

        internal static class NativeMethods {
            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            internal static extern SafeFindHandle FindFirstFile(string fileName, [In, Out] WIN32_FIND_DATA data);

            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            internal static extern bool FindNextFile(SafeFindHandle hndFindFile,
                                                     [In, Out, MarshalAs(UnmanagedType.LPStruct)] WIN32_FIND_DATA
                                                         lpFindFileData);

            [DllImport("kernel32.dll"), ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            internal static extern bool FindClose(IntPtr handle);

            #region Nested type: WIN32_FIND_DATA

            [Serializable, StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto), BestFitMapping(false)]
            internal class WIN32_FIND_DATA {
                internal FileAttributes dwFileAttributes;
                internal int ftCreationTime_dwLowDateTime;
                internal int ftCreationTime_dwHighDateTime;
                internal int ftLastAccessTime_dwLowDateTime;
                internal int ftLastAccessTime_dwHighDateTime;
                internal int ftLastWriteTime_dwLowDateTime;
                internal int ftLastWriteTime_dwHighDateTime;
                internal int nFileSizeHigh;
                internal int nFileSizeLow;
                internal int dwReserved0;
                internal int dwReserved1;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] internal string cFileName;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)] internal string cAlternateFileName;
            }

            #endregion
        }

        #endregion

        #region Nested type: SafeFindHandle

        internal sealed class SafeFindHandle : SafeHandleZeroOrMinusOneIsInvalid {
            [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
            internal SafeFindHandle() : base(true) {}

            protected override bool ReleaseHandle() {
                return NativeMethods.FindClose(handle);
            }
        }

        #endregion
    }
}