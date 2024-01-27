using System.Runtime.InteropServices;

namespace Imageflow.Bindings
{

    internal static class NativeMethods
    {
        public enum Lifetime
        {

            /// OutlivesFunctionCall -> 0
            OutlivesFunctionCall = 0,

            /// OutlivesContext -> 1
            OutlivesContext = 1,
        }
        
        public const int ABI_MAJOR = 3;
        public const int ABI_MINOR = 0;

        [DllImport("imageflow", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool imageflow_abi_compatible(uint imageflowAbiVerMajor, uint imageflowAbiVerMinor);

        [DllImport("imageflow", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint imageflow_abi_version_major();

        [DllImport("imageflow", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint imageflow_abi_version_minor();

        [DllImport("imageflow", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr imageflow_context_create(uint imageflowAbiVerMajor, uint imageflowAbiVerMinor);

        [DllImport("imageflow", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool imageflow_context_begin_terminate(JobContextHandle context);

        [DllImport("imageflow", CallingConvention = CallingConvention.Cdecl)]
        public static extern void imageflow_context_destroy(IntPtr context);

        [DllImport("imageflow", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool imageflow_context_has_error(JobContextHandle context);

        [DllImport("imageflow", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool imageflow_context_error_recoverable(JobContextHandle context);

        [DllImport("imageflow", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool imageflow_context_error_try_clear(JobContextHandle context);

        /// Return Type: int32_t->int
        [DllImport("imageflow", CallingConvention = CallingConvention.Cdecl)]
        public static extern int imageflow_context_error_code(JobContextHandle context);

        [DllImport("imageflow", CallingConvention = CallingConvention.Cdecl)]
        public static extern int imageflow_context_error_as_exit_code(JobContextHandle context);

        [DllImport("imageflow", CallingConvention = CallingConvention.Cdecl)]
        public static extern int imageflow_context_error_as_http_code(JobContextHandle context);

        [DllImport("imageflow", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool imageflow_context_print_and_exit_if_error(JobContextHandle context);

        ///response_in: void*
        ///status_code_out: int64_t*
        ///buffer_utf8_no_nulls_out: uint8_t**
        ///buffer_size_out: size_t*
        [DllImport("imageflow", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool imageflow_json_response_read(JobContextHandle context, JsonResponseHandle responseIn,
            out int statusCodeOut, out IntPtr bufferUtf8NoNullsOut, out UIntPtr bufferSizeOut);



        [DllImport("imageflow", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool imageflow_json_response_destroy(JobContextHandle context, IntPtr response);




        ///pointer: void*
        ///filename: char*
        ///line: int32_t->int
        [DllImport("imageflow", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool imageflow_context_memory_free(JobContextHandle context, IntPtr pointer,
            IntPtr filename, int line);


        [DllImport("imageflow", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool imageflow_context_error_write_to_buffer(JobContextHandle context, IntPtr buffer,
            UIntPtr bufferLength,
            out UIntPtr bytesWritten);


        [DllImport("imageflow", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr imageflow_context_send_json(JobContextHandle context, IntPtr method,
            IntPtr jsonBuffer, UIntPtr jsonBufferSize);




        [DllImport("imageflow", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr imageflow_context_memory_allocate(JobContextHandle context, IntPtr bytes,
            IntPtr filename, int line);


        [DllImport("imageflow", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool  imageflow_context_add_input_buffer(JobContextHandle context, int ioId, IntPtr buffer,
            UIntPtr bufferByteCount, Lifetime lifetime);

        [DllImport("imageflow", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool imageflow_context_add_output_buffer(JobContextHandle context, int ioId);


        [DllImport("imageflow", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool imageflow_context_get_output_buffer_by_id(JobContextHandle context,
            int ioId, out IntPtr resultBuffer, out UIntPtr resultBufferLength);


    }
}

