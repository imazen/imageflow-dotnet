using System.Runtime.ConstrainedExecution;
using Imageflow.Internal.Helpers;
using Microsoft.Win32.SafeHandles;

namespace Imageflow.Bindings
{
    /// <summary>
    /// A child SafeHandle that increments the reference count on JobContextHandle when created and decrements it when disposed.
    /// 
    /// </summary>
    internal sealed class JsonResponseHandle : SafeHandleZeroOrMinusOneIsInvalid, IAssertReady
    {
        public JsonResponseHandle(JobContextHandle parent, IntPtr ptr)
            : base(true)
        {
            ParentContext = parent ?? throw new ArgumentNullException(nameof(parent));
            SetHandle(ptr);
            
            var addRefSucceeded = false;
            parent.DangerousAddRef(ref addRefSucceeded);
            if (!addRefSucceeded)
            {
                throw new ArgumentException("SafeHandle.DangerousAddRef failed", nameof(parent));
            }
            _handleReferenced = addRefSucceeded ? 1 : 0;

        }

        private int _handleReferenced = 0;
        public JobContextHandle ParentContext { get; }

        public bool IsValid => !IsInvalid && !IsClosed && ParentContext.IsValid;

        public void AssertReady()
        {
            if (!ParentContext.IsValid) throw new ObjectDisposedException("Imageflow JobContextHandle");
            if (!IsValid) throw new ObjectDisposedException("Imageflow JsonResponseHandle");
        }
        
        

#pragma warning disable SYSLIB0004
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
#pragma warning restore SYSLIB0004
        protected override bool ReleaseHandle()
        {
            // The base class, the caller, handles interlocked / sync and preventing multiple calls.
            // We check ParentContext just in case someone went wild with DangerousRelease elsewhere.
            // It's a process-ending error if ParentContext is invalid. 
            if (ParentContext.IsValid) NativeMethods.imageflow_json_response_destroy(ParentContext, handle);
            ParentContext.DangerousRelease();
            return true;
        }
    }
}