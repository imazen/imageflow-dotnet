namespace Imageflow.Bindings
{
    /// <inheritdoc />
    /// <summary>
    /// For bugs
    /// </summary>
    public class ImageflowAssertionFailed(string message) : Exception(message);
}
