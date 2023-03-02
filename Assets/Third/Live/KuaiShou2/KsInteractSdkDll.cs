using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace kuaishou
{
    public class KsInteractSdkDll
    {
        #region Dll_Interface_Define

        [DllImport(@"KsGameLiveInteractionDll.dll", EntryPoint = "CreateKsInteractDllCLI", SetLastError = true,
            CharSet = CharSet.Ansi, ExactSpelling = false, CallingConvention = CallingConvention.Cdecl)]
        private extern static IntPtr CreateKsInteractDll(IntPtr appId, ulong len, KsInteractCallbackStruct callback);

        [DllImport(@"KsGameLiveInteractionDll.dll", EntryPoint = "ReleaseKsInteractDll", SetLastError = true,
            CharSet = CharSet.Ansi, ExactSpelling = false, CallingConvention = CallingConvention.Cdecl)]
        private extern static void ReleaseKsInteractDll(IntPtr sdk);

        [DllImport(@"KsGameLiveInteractionDll.dll", EntryPoint = "Connect", SetLastError = true, CharSet = CharSet.Ansi,
            ExactSpelling = false, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private extern static bool Connect(IntPtr sdk, IntPtr code, ulong codeLen, IntPtr extra, ulong extraLen);

        [DllImport(@"KsGameLiveInteractionDll.dll", EntryPoint = "Disconnect", SetLastError = true,
            CharSet = CharSet.Ansi,
            ExactSpelling = false, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private extern static bool Disconnect(IntPtr sdk);

        [DllImport(@"KsGameLiveInteractionDll.dll", EntryPoint = "IsConnected", SetLastError = true,
            CharSet = CharSet.Ansi,
            ExactSpelling = false, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private extern static bool IsConnected(IntPtr sdk);

        [DllImport(@"KsGameLiveInteractionDll.dll", EntryPoint = "SendData", SetLastError = true,
            CharSet = CharSet.Ansi,
            ExactSpelling = false, CallingConvention = CallingConvention.Cdecl)]
        private extern static ulong SendData(IntPtr sdk, int cmd, IntPtr data, ulong len);

        [StructLayoutAttribute(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        public struct KsInteractCallbackStruct
        {
            public IntPtr OnConnected;
            public IntPtr OnDisconnected;
            public IntPtr OnDataReceived;
            public IntPtr OnError;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void OnConnectedDelegate(IntPtr data, ulong len);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void OnDisconnectedDelegate();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void OnDataReceivedDelegate(int cmd, IntPtr msg, ulong len);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void OnErrorDelegate(int cmd, IntPtr msg, ulong len);

        #endregion

        #region Private_Class_And_Method

        private class KsInteractCallbackWrapper
        {
            private KsInteractCallback mCallback;
            private KsInteractCallbackStruct mCallbackStruct;

            private OnConnectedDelegate mOnConnectedDelegate;
            private OnDisconnectedDelegate mOnDisconnectedDelegate;
            private OnDataReceivedDelegate mOnDataReceivedDelegate;
            private OnErrorDelegate mOnErrorDelegate;

            public KsInteractCallbackWrapper(KsInteractCallback callback)
            {
                mCallback = callback;
                mCallbackStruct.OnConnected = Marshal.GetFunctionPointerForDelegate(mOnConnectedDelegate = new OnConnectedDelegate(OnConnected));
                mCallbackStruct.OnDisconnected = Marshal.GetFunctionPointerForDelegate(mOnDisconnectedDelegate = new OnDisconnectedDelegate(OnDisconnected));
                mCallbackStruct.OnDataReceived = Marshal.GetFunctionPointerForDelegate(mOnDataReceivedDelegate = new OnDataReceivedDelegate(OnDataReceived));
                mCallbackStruct.OnError = Marshal.GetFunctionPointerForDelegate(mOnErrorDelegate = new OnErrorDelegate(OnError));
            }

            public void OnConnected(IntPtr msg, ulong len)
            {
                var json = UTF8ToString(msg, len);
                Debug.Log("OnConnected: "+json);
                mCallback?.OnConnected(JsonConvert.DeserializeObject<ConnectData>(json));
            }

            public void OnDisconnected()
            {
                mCallback?.OnDisconnected();
            }

            public void OnDataReceived(int cmd, IntPtr msg, ulong len)
            {
                try
                {
                    var json = UTF8ToString(msg, len);
                    switch (cmd)
                    {
                        case 2: //送礼
                            mCallback?.OnGift(JsonConvert.DeserializeObject<GiftData>(json));
                            break;
                        case 3: //弹幕
                            mCallback?.OnDanMu(JsonConvert.DeserializeObject<DanMuData>(json));
                            break;
                        case 7: //点赞
                            mCallback?.OnDianZhan(JsonConvert.DeserializeObject<DianZhanData>(json));
                            break;
                        case 9: //分享
                            mCallback?.OnShare(JsonConvert.DeserializeObject<ShareData>(json));
                            break;
                        case 10: //关注
                            mCallback?.OnFollow(JsonConvert.DeserializeObject<FollowData>(json));
                            break;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }

            public void OnError(int code, IntPtr msg, ulong len)
            {
                mCallback?.OnError(code, UTF8ToString(msg, len));
            }

            public KsInteractCallbackStruct Callback()
            {
                return mCallbackStruct;
            }
        }

        private static ulong StringLen(string str)
        {
            return (ulong)Encoding.Default.GetBytes(str).Length;
        }

        private static string UTF8ToString(IntPtr ptr, ulong len)
        {
            byte[] b = new byte[(int)len];
            Marshal.Copy(ptr, b, 0, b.Length);
            b = Encoding.Convert(Encoding.UTF8, Encoding.Default, b);
            return Encoding.Default.GetString(b);
        }

        private static IntPtr StringToIntPtr(string str, out ulong outLen)
        {
            if (str == null || str.Length == 0)
            {
                outLen = 0;
                return IntPtr.Zero;
            }

            byte[] bytes = Encoding.Default.GetBytes(str);
            bytes = Encoding.Convert(Encoding.Default, Encoding.UTF8, bytes);
            outLen = (ulong)bytes.Length;
            IntPtr ptr = Marshal.AllocHGlobal(bytes.Length);
            Marshal.Copy(bytes, 0, ptr, bytes.Length);
            return ptr;
        }

        #endregion

        private IntPtr mSdkDll = IntPtr.Zero;
        private KsInteractCallbackWrapper mCallback = null;

        public KsInteractSdkDll(string appId, KsInteractCallback callback)
        {
            mCallback = new KsInteractCallbackWrapper(callback);
            // 没有中文不用转码
            mSdkDll = CreateKsInteractDll(Marshal.StringToHGlobalAnsi(appId), StringLen(appId), mCallback.Callback());
        }

        ~KsInteractSdkDll()
        {
            Release();
        }

        public void Release()
        {
            if (mSdkDll != IntPtr.Zero)
            {
                ReleaseKsInteractDll(mSdkDll);
                mSdkDll = IntPtr.Zero;
            }

            mCallback = null;
        }

        public Task<bool> Connect(string code, string extra)
        {
            return Task.Run(() =>
            {
                ulong codeLen, extraLen;
                IntPtr codePtr = StringToIntPtr(code, out codeLen);
                IntPtr extraPtr = StringToIntPtr(extra, out extraLen);
                bool ret = Connect(mSdkDll, codePtr, codeLen, extraPtr, extraLen);
                if (codePtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(codePtr);
                }

                if (extraPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(extraPtr);
                }

                return ret;
            });
        }

        public Task<bool> Disconnect()
        {
            return Task.Run(() => { return Disconnect(mSdkDll); });
        }

        public bool IsConnected()
        {
            return IsConnected(mSdkDll);
        }

        public ulong SendData(int cmd, string data)
        {
            ulong len;
            IntPtr ptr = StringToIntPtr(data, out len);
            ulong result = SendData(mSdkDll, cmd, ptr, len);
            if (ptr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(ptr);
            }

            return result;
        }
    }
}