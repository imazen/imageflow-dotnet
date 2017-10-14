using System;
using System.Runtime.ConstrainedExecution;
using Microsoft.Win32.SafeHandles;

namespace Imageflow.Bindings
{
    internal sealed class JsonResponseHandle : SafeHandleZeroOrMinusOneIsInvalid, IAssertReady
    {
        public JsonResponseHandle(JobContextHandle parent, IntPtr ptr)
            : base(true)
        {
            ParentContext = parent ?? throw new ArgumentNullException("parent");
            SetHandle(ptr);

        }

        public JobContextHandle ParentContext { get; }

        public bool IsValid => !IsInvalid && !IsClosed && ParentContext.IsValid;

        public void AssertReady()
        {
            if (!ParentContext.IsValid) throw new ObjectDisposedException("Imageflow JobContextHandle");
            if (!IsValid) throw new ObjectDisposedException("Imageflow JsonResponseHandle");
        }
        
        public ImageflowException DisposeAllowingException()
        {
            if (!IsValid) return null;
            
            try
            {
                if (!NativeMethods.imageflow_json_response_destroy(ParentContext, DangerousGetHandle()))
                {
                    return ImageflowException.FromContext(ParentContext);
                }
            }
            finally
            {
                Dispose();
            }
            return null;
        }
        

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        protected override bool ReleaseHandle()
        {
            return !ParentContext.IsValid || NativeMethods.imageflow_json_response_destroy(ParentContext, handle);
        }
    }
}