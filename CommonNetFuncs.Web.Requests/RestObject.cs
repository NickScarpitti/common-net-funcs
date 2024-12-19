namespace CommonNetFuncs.Web.Requests;

/// <summary>
/// Helper class to get around not being able to pass primitive types directly to a generic type
/// </summary>
/// <typeparam name="T">Primitive type to pass to the REST request</typeparam>
public class RestObject<T>// where T : class
{
    public T? Result { get; set; }
    public HttpResponseMessage? Response { get; set; }
}
