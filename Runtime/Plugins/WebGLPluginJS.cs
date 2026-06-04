using System.Runtime.InteropServices;

public static class WebGLPluginJS
{
    [DllImport("__Internal")]
    public static extern void OpenURL(string URL);

    [DllImport("__Internal")]
    public static extern void FullScreen();

    [DllImport("__Internal")]
    public static extern bool IsIOSBrowser();
}