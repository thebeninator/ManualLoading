using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GHPC.AI;
using GHPC.Camera;
using GHPC.Player;
using GHPC.State;
using GHPC.Vehicle;
using ManualLoading;
using MelonLoader;
using UnityEngine;
using GHPC.Weapons;
using System.Runtime.Remoting.Channels;
using HarmonyLib;
using GHPC.UI.Hud;

[assembly: MelonInfo(typeof(ManualLoadingMod), "Manual Loading", "1.0.0", "ATLAS")]
[assembly: MelonGame("Radian Simulations LLC", "GHPC")]

namespace ManualLoading
{
    public class ManualLoadingMod : MelonMod
    {
        public static GameObject[] vic_gos;
        static MelonPreferences_Entry<bool> human_loader;
        static MelonPreferences_Entry<bool> auto_loader;

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

                vic_go.AddComponent<Util.AlreadyConverted>();

                WeaponSystem main_gun = vic.WeaponsManager.Weapons[0].Weapon;

                if ((main_gun.Feed.HumanLoaded && human_loader.Value) || (main_gun.Feed.Autoloader != null && main_gun.Feed.LoadedClipType.Capacity == 1 && auto_loader.Value))
                {
                    main_gun.Feed.AutoReload = false;
                }
            }

            yield break;
        }

        public override void OnSceneWasLoaded(int idx, string scene_name)
        {
            if (scene_name == "MainMenu2_Scene" || scene_name == "LOADER_MENU" || scene_name == "LOADER_INITIAL" || scene_name == "t64_menu") return;

            StateController.RunOrDefer(GameState.GameReady, new GameStateEventHandler(GetVics), GameStatePriority.Medium);
            StateController.RunOrDefer(GameState.GameReady, new GameStateEventHandler(Convert), GameStatePriority.Medium);
        }


        [HarmonyPatch(typeof(AlertHud), "AddAlertMessage")]
        public static class ReplaceSound
        {
            public static bool Prefix(AlertHud __instance, object[] __args)
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
