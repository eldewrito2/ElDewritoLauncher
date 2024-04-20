using Microsoft.Win32;
using System;
using System.Runtime.InteropServices;
using System.Security;

namespace ElDewritoLauncher.Toasts
{
    class ToastManager : IDisposable
    {
        private IToastNotificationManagerStatics? _toastManager;
        private IToastNotifier? _toastNotifier;
        private IToastNotificationFactory? _toastNotificationFactory;
        private uint _classFactoryToken;
        private bool _disposed;

        public ToastManager(Guid appGuid, string appId, Func<INotificationActivationCallback> callbackFactory)
        {
            uint CLSCTX_LOCAL_SERVER = 0x4;
            uint REGCLS_MULTIPLEUSE = 1;
            // uint COINIT_MULTITHREADED = 0;
            // uint RO_INIT_MULTITHREADED = 1;

            // Marshal.ThrowExceptionForHR(CoInitializeEx(IntPtr.Zero, COINIT_MULTITHREADED));
            // Marshal.ThrowExceptionForHR(RoInitialize(RO_INIT_MULTITHREADED));

            Guid toastNotificationManagerStaticsIid = new Guid("50ac103f-d235-4598-bbef-98fe4d1a3ad4");
            Guid toastNotificationFactoryIid = new Guid("04124b20-82c6-4229-b109-fd9ed4662b53");

            using HSTRING hsAppId = HSTRING.FromString(appId);
            using HSTRING hsToastNotificationManager = HSTRING.FromString("Windows.UI.Notifications.ToastNotificationManager");
            using HSTRING hsToastNotification = HSTRING.FromString("Windows.UI.Notifications.ToastNotification");
            using HSTRING hsXmlDocument = HSTRING.FromString("Windows.Data.Xml.Dom.XmlDocument");

            Marshal.ThrowExceptionForHR(RoGetActivationFactory(hsToastNotificationManager, toastNotificationManagerStaticsIid, out IUnknown toastManagerUnk));
            _toastManager = (IToastNotificationManagerStatics)toastManagerUnk;

            Marshal.ThrowExceptionForHR(_toastManager.CreateToastNotifierWithId(hsAppId, out IUnknown toastNotifierUnk));
            _toastNotifier = (IToastNotifier)toastNotifierUnk;

            Marshal.ThrowExceptionForHR(RoGetActivationFactory(hsToastNotification, toastNotificationFactoryIid, out IUnknown toastNotificationFactoryUnk));
            _toastNotificationFactory = (IToastNotificationFactory)toastNotificationFactoryUnk;

            var classFactory = new NotificationActivationCallbackClassFactory(callbackFactory);
            Marshal.ThrowExceptionForHR(CoRegisterClassObject(appGuid, classFactory, CLSCTX_LOCAL_SERVER, REGCLS_MULTIPLEUSE, out _classFactoryToken));
        }

        ~ToastManager()
        {
            Dispose(false);
        }

        public static void Install(string activatorPath, string args, Guid appGuid, string appId, string displayName, string iconUrl)
        {
            string guidString = appGuid.ToString("B").ToUpper();
            string localServer32Path = $"SOFTWARE\\Classes\\CLSID\\{guidString}\\LocalServer32";
            string aumPath = $"SOFTWARE\\Classes\\AppUserModelId\\{appId}";

            using var comServerKey = Registry.CurrentUser.CreateSubKey(localServer32Path);
            using var aumKey = Registry.CurrentUser.CreateSubKey(aumPath);

            string exePath = $"\"{activatorPath!}\" {args}";

            comServerKey.SetValue(null, exePath, RegistryValueKind.String);
            aumKey.SetValue("DisplayName", displayName, RegistryValueKind.String);
            aumKey.SetValue("CustomActivator", guidString, RegistryValueKind.String);
            aumKey.SetValue("IconUri", iconUrl, RegistryValueKind.String);
        }

        public static void Uninstall(Guid appGuid, string appId)
        {
            string guidString = appGuid.ToString("B").ToUpper();
            string localServer32Path = $"SOFTWARE\\Classes\\CLSID\\{guidString}\\LocalServer32";
            string aumPath = $"SOFTWARE\\Classes\\AppUserModelId\\{appId}";

            Registry.CurrentUser.DeleteSubKey(localServer32Path);
            Registry.CurrentUser.DeleteSubKey(aumPath);
        }

        public void ShowToast(string xml)
        {
            if (_toastNotificationFactory == null || _toastNotifier == null)
                throw new InvalidOperationException("Not initialized");

            using HSTRING hsXmlDocument = HSTRING.FromString("Windows.Data.Xml.Dom.XmlDocument");
            using HSTRING hsXml = HSTRING.FromString(xml);

            IXmlDocument? xmlDoc = null;
            IUnknown? toastNotificationUnk = null;

            try
            {
                Marshal.ThrowExceptionForHR(RoActivateInstance(hsXmlDocument, out IUnknown inputXmlUnk));
                xmlDoc = (IXmlDocument)inputXmlUnk;
                var xmlDocIo = (IXmlDocumentIO)inputXmlUnk;

                Marshal.ThrowExceptionForHR(xmlDocIo.LoadXML(hsXml));
                Marshal.ThrowExceptionForHR(_toastNotificationFactory.CreateToastNotification(xmlDoc, out toastNotificationUnk));
                Marshal.ThrowExceptionForHR(_toastNotifier.Show(toastNotificationUnk));
            }
            finally
            {
                toastNotificationUnk?.Release();
                xmlDoc?.Release();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;

                CoRevokeClassObject(_classFactoryToken);
                _toastNotificationFactory?.Release();
                _toastNotifier?.Release();
                _toastManager?.Release();
            }
        }

        [ComVisible(true)]
        [ClassInterface(ClassInterfaceType.None)]
        [Guid("B032089D-0E09-4514-B1FA-660FE6520117")]
        public class NotificationActivationCallbackClassFactory : IClassFactory
        {
            private readonly Func<INotificationActivationCallback> _callbackFactory;

            public NotificationActivationCallbackClassFactory(Func<INotificationActivationCallback> callbackFactory)
            {
                _callbackFactory = callbackFactory;
            }

            public void CreateInstance(object pUnkOuter, ref Guid riid, out IntPtr ppvObject)
            {
                if (pUnkOuter != null)
                {
                    throw new NotSupportedException("Aggregation not supported.");
                }

                var instance = _callbackFactory();

                if (instance is IClassFactory)
                {
                    ppvObject = Marshal.GetComInterfaceForObject(instance, typeof(IClassFactory));
                }
                else
                {
                    ppvObject = Marshal.GetComInterfaceForObject(instance, typeof(INotificationActivationCallback));
                }
            }

            public void LockServer(bool fLock)
            {

            }
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("AF86E2E0-B12D-4c6a-9C5A-D7AA65101E90")]
        public interface IUnknown
        {
            IntPtr QueryInterface(ref Guid iid, out IntPtr ppv);
            uint AddRef();
            uint Release();
        }

        public enum TrustLevel
        {
            BaseTrust,
            PartialTrust,
            FullTrust
        }

        [ComImport]
        [Guid("00000001-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IClassFactory
        {
            void CreateInstance([MarshalAs(UnmanagedType.IUnknown)] object pUnkOuter, ref Guid riid, out IntPtr ppvObject);
            void LockServer(bool fLock);
        }

        // IInspectable can't be cast for whatever reason, so we need to duplicate parts of the vtable

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("04124b20-82c6-4229-b109-fd9ed4662b53")]
        public interface IToastNotificationFactory : IUnknown
        {
            void GetIids(out uint iidCount, out IntPtr iids);
            void GetRuntimeClassName(out IntPtr className);
            void GetTrustLevel(out TrustLevel trustLevel);

            int CreateToastNotification(
                [MarshalAs(UnmanagedType.Interface)] IXmlDocument content,
                [Out, MarshalAs(UnmanagedType.Interface)] out IUnknown value);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("f7f3a506-1e87-42d6-bcfb-b8c809fa5494")]
        public interface IXmlDocument : IUnknown
        {
            void GetIids(out uint iidCount, out IntPtr iids);
            void GetRuntimeClassName(out IntPtr className);
            void GetTrustLevel(out TrustLevel trustLevel);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("6cd0e74e-ee65-4489-9ebf-ca43e87ba637")]
        public interface IXmlDocumentIO : IUnknown
        {
            int GetIids(out uint iidCount, out IntPtr iids);
            int GetRuntimeClassName(out IntPtr className);
            int GetTrustLevel(out TrustLevel trustLevel);

            int LoadXML(HSTRING xml);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("50ac103f-d235-4598-bbef-98fe4d1a3ad4")]
        public interface IToastNotificationManagerStatics : IUnknown
        {
            int GetIids(out uint iidCount, out IntPtr iids);
            int GetRuntimeClassName(out IntPtr className);
            int GetTrustLevel(out TrustLevel trustLevel);

            int CreateToastNotifier(out IntPtr result);
            int CreateToastNotifierWithId(HSTRING applicationId, [Out, MarshalAs(UnmanagedType.Interface)] out IUnknown result);
            int GetTemplateContent([In] uint type, out IntPtr result);
        }

        [ComImport]
        [Guid("75927B93-03F3-41EC-91D3-6E5BAC1B38E7")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IToastNotifier : IUnknown
        {
            void GetIids(out uint iidCount, out IntPtr iids);
            void GetRuntimeClassName(out IntPtr className);
            void GetTrustLevel(out TrustLevel trustLevel);

            int Show([In, MarshalAs(UnmanagedType.Interface)] object notification);
            int Hide([In, MarshalAs(UnmanagedType.Interface)] object notification);
        }


        [StructLayout(LayoutKind.Sequential), SecuritySafeCritical]
        public struct NOTIFICATION_USER_INPUT_DATA
        {
            [MarshalAs(UnmanagedType.LPWStr)] public string Key;
            [MarshalAs(UnmanagedType.LPWStr)] public string Data;
        }

        [ComImport]
        [Guid("53E31837-6600-4A81-9395-75CFFE746F94")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface INotificationActivationCallback
        {
            void Activate(
                [MarshalAs(UnmanagedType.LPWStr)] string appUserModelId,
                [MarshalAs(UnmanagedType.LPWStr)] string invokedArgs,
                [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)]
            NOTIFICATION_USER_INPUT_DATA[]  data,
                int count);
        }


        [DllImport("combase.dll", SetLastError = true)]
        public static extern int RoInitialize(uint initType);

        [DllImport("combase.dll", SetLastError = true)]
        public static extern void RoUninitialize();

        [DllImport("combase.dll", SetLastError = true)]
        public static extern int WindowsCreateStringReference(
            [MarshalAs(UnmanagedType.LPWStr)] string sourceString,
            int length,
            out IntPtr hstringHeader,
            out IntPtr hstring);

        [DllImport("combase.dll", SetLastError = true)]
        public static extern int RoActivateInstance(HSTRING classId, [Out, MarshalAs(UnmanagedType.Interface)] out IUnknown instance);

        [DllImport("combase.dll", SetLastError = true, PreserveSig = false)]
        static extern int RoGetActivationFactory(
            HSTRING classId,
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid guid,
            [Out, MarshalAs(UnmanagedType.Interface)] out IUnknown iface);

        [DllImport("combase.dll", CallingConvention = CallingConvention.StdCall)]
        static extern int WindowsCreateString([MarshalAs(UnmanagedType.LPWStr)] string sourceString, int length, [Out] IntPtr hstring);

        [DllImport("combase.dll", CallingConvention = CallingConvention.StdCall)]
        static extern int WindowsDeleteString(IntPtr hstring);

        [DllImport("ole32.dll")]
        static extern int CoInitializeEx(IntPtr pvReserved, int dwCoInit);

        [DllImport("ole32.dll")]
        private static extern int CoRegisterClassObject(
            [MarshalAs(UnmanagedType.LPStruct)] Guid rclsid,
            [MarshalAs(UnmanagedType.IUnknown)] object pUnk,
            uint dwClsContext,
            uint flags,
            out uint lpdwRegister);

        [DllImport("ole32.dll")]
        public static extern int CoRevokeClassObject(uint dwRegister);

        [StructLayout(LayoutKind.Sequential), SecuritySafeCritical]
        public struct HSTRING : IDisposable
        {
            readonly IntPtr _handle;

            public static HSTRING FromString(string s)
            {
                IntPtr h = Marshal.AllocHGlobal(IntPtr.Size);
                try
                {
                    Marshal.ThrowExceptionForHR(WindowsCreateString(s, s.Length, h));
                    return Marshal.PtrToStructure<HSTRING>(h);
                }
                finally
                {
                    Marshal.FreeHGlobal(h);
                }
            }

            public void Dispose()
            {
                if (_handle != default)
                {
                    WindowsDeleteString(_handle);
                }
            }
        }
    }
}