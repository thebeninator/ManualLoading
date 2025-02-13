using System;
using System.Collections;
using GHPC.Player;
using GHPC.State;
using GHPC.Vehicle;
using ManualLoading;
using MelonLoader;
using UnityEngine;
using GHPC.Weapons;
using HarmonyLib;
using GHPC.UI.Hud;
using System.Linq;

[assembly: MelonInfo(typeof(ManualLoadingMod), "Manual Loading", "1.0.1", "ATLAS")]
[assembly: MelonGame("Radian Simulations LLC", "GHPC")]

namespace ManualLoading
{
    public class ManualLoadingMod : MelonMod
    {
        public static GameObject[] vic_gos;
        static MelonPreferences_Entry<bool> human_loader;
        static MelonPreferences_Entry<bool> auto_loader;

        public class ManualLoadingHandler : MonoBehaviour
        {
            public AmmoFeed feed;
            public static PlayerInput player_manager;

            void Awake()
            {
                if (player_manager != null) return;
                player_manager = GameObject.Find("_APP_GHPC_").GetComponent<PlayerInput>();
            }

            void Update()
            {
                feed.AutoReload = player_manager.CurrentPlayerUnit.gameObject.GetInstanceID() != gameObject.GetInstanceID();
            }
        }


        public IEnumerator GetVics(GameState _)
        {
            vic_gos = GameObject.FindGameObjectsWithTag("Vehicle");

            yield break;
        }

        public override void OnInitializeMelon()
        {
            MelonPreferences_Category cfg = MelonPreferences.CreateCategory("ManualLoading");
            human_loader = cfg.CreateEntry<bool>("Manual Human Loaders", true);
            auto_loader = cfg.CreateEntry<bool>("Manual Autoloaders", true);
        }

        public static IEnumerator Convert(GameState _)
        {
            foreach (GameObject vic_go in vic_gos)
            {
                Vehicle vic = vic_go.GetComponent<Vehicle>();

                if (vic == null) continue;

                if (vic_go.GetComponent<Util.AlreadyConverted>() != null) continue;
                if (vic.WeaponsManager == null || vic.WeaponsManager.Weapons.Length == 0) continue;
                WeaponSystem main_gun = vic.WeaponsManager.Weapons[0].Weapon;
                AmmoFeed feed = main_gun.Feed;

                if 
                (
                    ((feed.HumanLoaded && human_loader.Value) || (feed.Autoloader != null && auto_loader.Value)) 
                    && feed.LoadedClipType.Capacity == 1)
                {
                    vic_go.AddComponent<Util.AlreadyConverted>();
                    ManualLoadingHandler mlh = vic_go.AddComponent<ManualLoadingHandler>();
                    mlh.feed = feed;

                    if (vic._friendlyName.Contains("BMP-1")) {
                        feed.ClipReloadStages[0].StageAudio = feed.RoundCycleStages[1].StageAudio;
                        feed.RoundCycleStages[1].StageAudio = null;
                    }
                }
            }

            yield break;
        }

        public override void OnSceneWasLoaded(int idx, string scene_name)
        {
            if (Util.menu_screens.Contains(scene_name)) return;

            StateController.RunOrDefer(GameState.GameReady, new GameStateEventHandler(GetVics), GameStatePriority.Medium);
            StateController.RunOrDefer(GameState.GameReady, new GameStateEventHandler(Convert), GameStatePriority.Medium);
        }


        [HarmonyPatch(typeof(VehicleHudText), "AddAlertMessage")]
        public static class SuppressNextAmmo
        { 
            public static bool Prefix(VehicleHudText __instance, object[] __args)
            {
                if ((String)__args[0] == "Select next ammunition with 1, 2, 3, 4 or use Q to restock" || 
                    (String)__args[0] == "Select next ammunition with 1, 2, 3, 4") 
                {
                    return false;
                }

                return true;
            }
        }
    }
}
