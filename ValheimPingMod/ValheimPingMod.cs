using BepInEx;
using HarmonyLib;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine;

namespace ValheimPingMod
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInProcess("valheim.exe")]
    //[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal class ValheimPingMod : BaseUnityPlugin
    {
        public const string PluginGUID = "com.rage311.ValheimPingMod";
        public const string PluginName = "ValheimPingMod";
        public const string PluginVersion = "0.0.1";

        private readonly Harmony harmony = new Harmony("my.ValheimPingMod");

        private void Awake()
        {
            harmony.PatchAll();
        }

        static CancellationTokenSource tokenSource;
        static bool tokenInstantiated = false;

        private void Update()
        {
            // Since our Update function in our BepInEx mod class will load BEFORE Valheim loads,
            // we need to check that ZInput is ready to use first.
            if (ZInput.instance != null && ZInput.GetKeyDown(KeyCode.Delete))
            {
                Debug.Log(message: $"Delete pressed");
                if (tokenInstantiated)
                {
                    tokenSource.Cancel();
                    //tokenSource.Dispose();
                    //tokenSource = null;
                    tokenSource = new CancellationTokenSource();
                }
            }
        }

        static Task pingTask;

        [HarmonyPatch(typeof(Chat), nameof(Chat.SendPing))]
        class Ping_Patch
        {
            static void Postfix(ref Vector3 position)
            {
                Debug.Log(message: $"Ping_Patch.Postfix called, it wants its haircut back");

                Player localPlayer = Player.m_localPlayer;
                Vector3 vector = position;
                vector.y = localPlayer.transform.position.y;

                if (tokenInstantiated)
                {
                    Debug.Log(message: $"new token");
                    tokenSource.Cancel();
                    //tokenSource.Dispose();
                    //tokenSource = null;
                }

                tokenSource = new CancellationTokenSource();
                tokenInstantiated = true;
                CancellationToken cancelToken = tokenSource.Token;

                Debug.Log(message: $"Starting new pingTask");
                pingTask = null;

                UserInfo localUser = UserInfo.GetLocalUser();
                string networkUserId = PrivilegeManager.GetNetworkUserId();

                pingTask = new Task(async () =>
                {
                    //Debug.Log(message: $"new pingTask");
                    while (!cancelToken.IsCancellationRequested)
                    {
                        ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "ChatMessage", vector, 3, localUser, "", networkUserId);
                        await Task.Delay(7500, cancelToken);
                    }
                    //Debug.Log(message: $"pingTask cancelled");
                }, cancelToken);
                pingTask.Start();
            }
        }
    }
}

