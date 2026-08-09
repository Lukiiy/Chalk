using HarmonyLib;
using Mirror;

namespace Chalk;

[HarmonyPatch(typeof(TextChatManager))]
internal static class TextManager
{
    public static void SendToAll(string text)
    {
        if (!NetworkServer.active || !SingletonNetworkBehaviour<TextChatManager>.HasInstance) return;

        foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
        {
            if (conn == null) continue;

            SingletonNetworkBehaviour<TextChatManager>.Instance.RpcServerMsg(conn, text);
        }
    }

    [TargetRpc]
    private static void RpcServerMsg(this TextChatManager self, NetworkConnectionToClient connection, string text) => TextChatUi.ShowMessage($"<color=#ff405c>[Chalk]</color> {text}");
}