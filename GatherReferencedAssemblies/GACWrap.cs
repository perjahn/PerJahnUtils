﻿using System.Runtime.InteropServices;
using System.Text;

namespace System.GACManagedAccess
{
    //-------------------------------------------------------------
    // Interfaces defined by fusion
    //-------------------------------------------------------------
    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("e707dcde-d1cd-11d2-bab9-00c04f8eceae")]
    internal interface IAssemblyCache
    {
        [PreserveSig()]
        int UninstallAssembly(
            int flags,
            [MarshalAs(UnmanagedType.LPWStr)]
            string assemblyName,
            InstallReference refData,
            out AssemblyCacheUninstallDisposition disposition);

        [PreserveSig()]
        int QueryAssemblyInfo(
            int flags,
            [MarshalAs(UnmanagedType.LPWStr)]
            string assemblyName,
            ref AssemblyInfo assemblyInfo);

        [PreserveSig()]
        int Reserved(
            int flags,
            IntPtr pvReserved,
            out Object ppAsmItem,
            [MarshalAs(UnmanagedType.LPWStr)]
            string assemblyName);

        [PreserveSig()]
        int Reserved(out Object ppAsmScavenger);

        [PreserveSig()]
        int InstallAssembly(
            int flags,
            [MarshalAs(UnmanagedType.LPWStr)]
            string assemblyFilePath,
            InstallReference refData);
    }

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("CD193BC0-B4BC-11d2-9833-00C04FC31D2E")]
    internal interface IAssemblyName
    {
        [PreserveSig()]
        int SetProperty(
            int PropertyId,
            IntPtr pvProperty,
            int cbProperty);

        [PreserveSig()]
        int GetProperty(
            int PropertyId,
            IntPtr pvProperty,
            ref int pcbProperty);

        [PreserveSig()]
        int Finalize();

        [PreserveSig()]
        int GetDisplayName(
            StringBuilder pDisplayName,
            ref int pccDisplayName,
            int displayFlags);

        [PreserveSig()]
        int Reserved(ref Guid guid,
            Object obj1,
            Object obj2,
            string string1,
            Int64 llFlags,
            IntPtr pvReserved,
            int cbReserved,
            out IntPtr ppv);

        [PreserveSig()]
        int GetName(
            ref int pccBuffer,
            StringBuilder pwzName);

        [PreserveSig()]
        int GetVersion(
            out int versionHi,
            out int versionLow);

        [PreserveSig()]
        int IsEqual(
            IAssemblyName pAsmName,
            int cmpFlags);

        [PreserveSig()]
        int Clone(out IAssemblyName pAsmName);
    }

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("21b8916c-f28e-11d2-a473-00c04f8ef448")]
    internal interface IAssemblyEnum
    {
        [PreserveSig()]
        int GetNextAssembly(
            IntPtr pvReserved,
            out IAssemblyName ppName,
            int flags);

        [PreserveSig()]
        int Reset();

        [PreserveSig()]
        int Clone(out IAssemblyEnum ppEnum);
    }

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("582dac66-e678-449f-aba6-6faaec8a9394")]
    internal interface IInstallReferenceItem
    {
        // A pointer to a FUSION_INSTALL_REFERENCE structure.
        // The memory is allocated by the GetReference method and is freed when
        // IInstallReferenceItem is released. Callers must not hold a reference to this
        // buffer after the IInstallReferenceItem object is released.
        // This uses the InstallReferenceOutput object to avoid allocation
        // issues with the interop layer.
        // This cannot be marshaled directly - must use IntPtr
        [PreserveSig()]
        int GetReference(
            out IntPtr pRefData,
            int flags,
            IntPtr pvReserced);
    }

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("56b1a988-7c0c-4aa2-8639-c3eb5a90226f")]
    internal interface IInstallReferenceEnum
    {
        [PreserveSig()]
        int GetNextInstallReferenceItem(
        out IInstallReferenceItem ppRefItem,
        int flags,
        IntPtr pvReserced);
    }

    public enum AssemblyCommit
    {
        None = 0,
        Default = 1,
        Force = 2
    }

    public enum AssemblyCacheUninstallDisposition
    {
        Unknown = 0,
        Uninstalled = 1,
        StillInUse = 2,
        AlreadyUninstalled = 3,
        DeletePending = 4,
        HasInstallReference = 5,
        ReferenceNotFound = 6
    }

    [Flags]
    internal enum AssemblyCacheFlags
    {
        GAC = 2,
    }

    internal enum CreateAssemblyNameObjectFlags
    {
        CANOF_DEFAULT = 0,
        CANOF_PARSE_DISPLAY_NAME = 1,
    }

    [Flags]
    internal enum AssemblyNameDisplayFlags
    {
        VERSION = 0x01,
        CULTURE = 0x02,
        PUBLIC_KEY_TOKEN = 0x04,
        PROCESSORARCHITECTURE = 0x20,
        RETARGETABLE = 0x80,
        // This enum will change in the future to include more attributes.
        ALL = VERSION
            | CULTURE
            | PUBLIC_KEY_TOKEN
            | PROCESSORARCHITECTURE
            | RETARGETABLE
    }

    [StructLayout(LayoutKind.Sequential)]
    public class InstallReference(Guid guid, string id, string data)
    {
        public Guid GuidScheme => guidScheme;
        public string Identifier => identifier;
        public string Description => description;

        readonly int cbSize = 2 * IntPtr.Size + 16 + (id.Length + data.Length) * 2;
        readonly int flags = 0;
        readonly Guid guidScheme = guid;

        [MarshalAs(UnmanagedType.LPWStr)]
        readonly string identifier = id;

        [MarshalAs(UnmanagedType.LPWStr)]
        readonly string description = data;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct AssemblyInfo
    {
        public int cbAssemblyInfo; // size of this structure for future expansion
        public int assemblyFlags;
        public long assemblySizeInKB;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string currentAssemblyPath;
        public int cchBuf; // size of path buf.
    }

    [ComVisible(false)]
    public class InstallReferenceGuid
    {
        public static bool IsValidGuidScheme(Guid guid)
        {
            return guid.Equals(UninstallSubkeyGuid) || guid.Equals(FilePathGuid) || guid.Equals(OpaqueGuid) || guid.Equals(Guid.Empty);
        }

        public readonly static Guid UninstallSubkeyGuid = new("8cedc215-ac4b-488b-93c0-a50a49cb2fb8");
        public readonly static Guid FilePathGuid = new("b02f9d65-fb77-4f7a-afa5-b391309f11c9");
        public readonly static Guid OpaqueGuid = new("2ec93463-b0c3-45e1-8364-327e96aea856");
        // these GUID cannot be used for installing into GAC.
        public readonly static Guid MsiGuid = new("25df0fc1-7f97-4070-add7-4b13bbfd7cb8");
        public readonly static Guid OsInstallGuid = new("d16d444c-56d8-11d5-882d-0080c847b195");
    }

    [ComVisible(false)]
    public static class AssemblyCache
    {
        public static void InstallAssembly(string assemblyPath, InstallReference reference, AssemblyCommit flags)
        {
            if (reference != null)
            {
                if (!InstallReferenceGuid.IsValidGuidScheme(reference.GuidScheme))
                {
                    throw new ArgumentException("Invalid reference guid.", "guid");
                }
            }

            var hr = Utils.CreateAssemblyCache(out IAssemblyCache ac, 0);
            if (hr >= 0)
            {
                hr = ac.InstallAssembly((int)flags, assemblyPath, reference);
            }
            if (hr < 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }
        }

        // assemblyName has to be fully specified name.
        // A.k.a, for v1.0/v1.1 assemblies, it should be "name, Version=xx, Culture=xx, PublicKeyToken=xx".
        // For v2.0 assemblies, it should be "name, Version=xx, Culture=xx, PublicKeyToken=xx, ProcessorArchitecture=xx".
        // If assemblyName is not fully specified, a random matching assembly will be uninstalled.
        public static void UninstallAssembly(string assemblyName, InstallReference reference, out AssemblyCacheUninstallDisposition disp)
        {
            AssemblyCacheUninstallDisposition dispResult = AssemblyCacheUninstallDisposition.Uninstalled;
            if (reference != null)
            {
                if (!InstallReferenceGuid.IsValidGuidScheme(reference.GuidScheme))
                {
                    throw new ArgumentException("Invalid reference guid.", "guid");
                }
            }

            var hr = Utils.CreateAssemblyCache(out IAssemblyCache ac, 0);
            if (hr >= 0)
            {
                hr = ac.UninstallAssembly(0, assemblyName, reference, out dispResult);
            }
            if (hr < 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            disp = dispResult;
        }

        // See comments in UninstallAssembly
        public static string QueryAssemblyInfo(string assemblyName)
        {
            if (assemblyName == null)
            {
                throw new ArgumentException("Invalid name", "assemblyName");
            }

            AssemblyInfo aInfo = new()
            {
                cchBuf = 1024
            };
            // Get a string with the desired length
            aInfo.currentAssemblyPath = new string('\0', aInfo.cchBuf);

            var hr = Utils.CreateAssemblyCache(out IAssemblyCache ac, 0);
            if (hr >= 0)
            {
                hr = ac.QueryAssemblyInfo(0, assemblyName, ref aInfo);
            }
            if (hr < 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            return aInfo.currentAssemblyPath;
        }
    }

    [ComVisible(false)]
    public class AssemblyCacheEnum
    {
        // null means enumerate all the assemblies
        public AssemblyCacheEnum(string assemblyName)
        {
            IAssemblyName fusionName = null;
            var hr = 0;

            if (assemblyName != null)
            {
                hr = Utils.CreateAssemblyNameObject(
                    out fusionName,
                    assemblyName,
                    CreateAssemblyNameObjectFlags.CANOF_PARSE_DISPLAY_NAME,
                    IntPtr.Zero);
            }

            if (hr >= 0)
            {
                hr = Utils.CreateAssemblyEnum(
                    out m_AssemblyEnum,
                    IntPtr.Zero,
                    fusionName,
                    AssemblyCacheFlags.GAC,
                    IntPtr.Zero);
            }
            if (hr < 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }
        }

        public string GetNextAssembly()
        {
            if (done)
            {
                return null;
            }

            // Now get next IAssemblyName from m_AssemblyEnum
            var hr = m_AssemblyEnum.GetNextAssembly(0, out IAssemblyName fusionName, 0);
            if (hr < 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            if (fusionName != null)
            {
                return GetFullName(fusionName);
            }
            else
            {
                done = true;
                return null;
            }
        }

        private string GetFullName(IAssemblyName fusionAsmName)
        {
            StringBuilder sDisplayName = new(1024);
            var iLen = 1024;

            var hr = fusionAsmName.GetDisplayName(sDisplayName, ref iLen, (int)AssemblyNameDisplayFlags.ALL);
            if (hr < 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            return sDisplayName.ToString();
        }

        private IAssemblyEnum m_AssemblyEnum = null;
        private bool done;
    }

    public class AssemblyCacheInstallReferenceEnum
    {
        public AssemblyCacheInstallReferenceEnum(string assemblyName)
        {
            var hr = Utils.CreateAssemblyNameObject(
                out IAssemblyName fusionName,
                assemblyName,
                CreateAssemblyNameObjectFlags.CANOF_PARSE_DISPLAY_NAME,
                IntPtr.Zero);

            if (hr >= 0)
            {
                hr = Utils.CreateInstallReferenceEnum(out refEnum, fusionName, 0, IntPtr.Zero);
            }
            if (hr < 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }
        }

        public InstallReference GetNextReference()
        {
            var hr = refEnum.GetNextInstallReferenceItem(out IInstallReferenceItem item, 0, IntPtr.Zero);
            if ((uint)hr == 0x80070103)
            {
                // ERROR_NO_MORE_ITEMS
                return null;
            }

            if (hr < 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            InstallReference instRef = new(Guid.Empty, string.Empty, string.Empty);

            hr = item.GetReference(out IntPtr refData, 0, IntPtr.Zero);
            if (hr < 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            Marshal.PtrToStructure(refData, instRef);
            return instRef;
        }

        private IInstallReferenceEnum refEnum;
    }

    internal class Utils
    {
        [DllImport("fusion.dll")]
        internal static extern int CreateAssemblyEnum(
            out IAssemblyEnum ppEnum,
            IntPtr pUnkReserved,
            IAssemblyName pName,
            AssemblyCacheFlags flags,
            IntPtr pvReserved);

        [DllImport("fusion.dll")]
        internal static extern int CreateAssemblyNameObject(
            out IAssemblyName ppAssemblyNameObj,
            [MarshalAs(UnmanagedType.LPWStr)]
            string szAssemblyName,
            CreateAssemblyNameObjectFlags flags,
            IntPtr pvReserved);

        [DllImport("fusion.dll")]
        internal static extern int CreateAssemblyCache(
            out IAssemblyCache ppAsmCache,
            int reserved);

        [DllImport("fusion.dll")]
        internal static extern int CreateInstallReferenceEnum(
            out IInstallReferenceEnum ppRefEnum,
            IAssemblyName pName,
            int dwFlags,
            IntPtr pvReserved);
    }
}
