using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;

namespace Imageflow.Internal.Helpers;

internal class PinBox : CriticalFinalizerObject, IDisposable
{
    private List<GCHandle>? _pinned;
    internal void AddPinnedData(GCHandle handle)
    {
        _pinned ??= new List<GCHandle>();
        _pinned.Add(handle);
    }

    public void Dispose()
    {
        UnpinAll();
        GC.SuppressFinalize(this);
    }

    private void UnpinAll()
    {
        //Unpin GCHandles
        if (_pinned != null)
        {
            foreach (var active in _pinned)
            {
                if (active.IsAllocated)
                {
                    active.Free();
                }
            }
            _pinned = null;
        }
    }

    ~PinBox()
    {
        UnpinAll();
    }
}
