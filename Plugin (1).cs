using BepInEx;

namespace Console
{
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        // Put this snippet of code in your BaseUnityPlugin
        void Start() => Console.LoadConsole();
    }
}
