namespace Imageflow.Fluent;

public enum SourceLifetime
{
    /// <summary>
    /// The function you are invoking will dispose the source when the task is complete (or cancelled/failed). The source and underlying memory/stream must remain valid until the task is complete.
    /// </summary>
    NowOwnedAndDisposedByTask,
    /// <summary>
    /// By using this, you solemnly swear to not close, dispose, or reuse the data source object or its underlying memory/stream until after the job is disposed. The job will dispose the source when the job is disposed.
    /// </summary>
    TransferOwnership,
    /// <summary>
    /// You swear not to close, dispose, or reuse the data source object or its underlying memory/stream until after the job is disposed. You remain responsible for disposing and cleaning up the source after the job is disposed.
    /// </summary>
    Borrowed
}
