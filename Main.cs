using System;
using System.IO;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityModManagerNet;
using Newtonsoft.Json;

namespace ChangeDiscordRP
{
    public class Main
    {
        private static Harmony _harmony;
        private static UnityModManager.ModEntry _modEntry;
        private static bool _isEnabled = false;
        private static ModSettings _settings;

        [Serializable]
        public class ModSettings
        {
            public string CustomState = "Default State";
            public string CustomDetails = "Default Details";
            public string CustomLargeText = "Default Large Text";

            private static string GetSettingsPath(UnityModManager.ModEntry modEntry)
            {
                return Path.Combine(modEntry.Path, "Settings.json");
            }

            public void Save(UnityModManager.ModEntry modEntry)
            {
                File.WriteAllText(GetSettingsPath(modEntry), JsonConvert.SerializeObject(this, Formatting.Indented));
            }

            public static ModSettings Load(UnityModManager.ModEntry modEntry)
            {
                string path = GetSettingsPath(modEntry);
                if (File.Exists(path))
                {
                    return JsonConvert.DeserializeObject<ModSettings>(File.ReadAllText(path)) ?? new ModSettings();
                }
                return new ModSettings();
            }
        }

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            _modEntry = modEntry;
            _modEntry.OnToggle = OnToggle;
            _modEntry.OnGUI = OnGUI;
            _modEntry.OnSaveGUI = OnSaveGUI;

            _settings = ModSettings.Load(modEntry);

            return true;
        }

        private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            _isEnabled = value;
            if (value)
            {
                _harmony = new Harmony(modEntry.Info.Id);
                _harmony.PatchAll(Assembly.GetExecutingAssembly());
            }
            else
            {
                _harmony?.UnpatchAll(modEntry.Info.Id);
                _harmony = null;
            }
            return true;
        }

        private static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            DrawSettingsUI();
        }

        private static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            _settings.Save(modEntry);
        }

        private static void DrawSettingsUI()
        {
            GUILayout.Label("DiscordRichPresence", new GUIStyle(GUI.skin.label) { fontSize = 30, fontStyle = FontStyle.Bold });
            GUILayout.Space(20);

            DrawLabeledTextField("State:", ref _settings.CustomState);
            DrawLabeledTextField("Details:", ref _settings.CustomDetails);
            DrawLabeledTextField("Large Text:", ref _settings.CustomLargeText);

            GUILayout.Space(10);
        }

        private static void DrawLabeledTextField(string label, ref string fieldValue)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(120));
            fieldValue = GUILayout.TextField(fieldValue, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();
        }

        [HarmonyPatch(typeof(DiscordController), "UpdatePresence")]
        internal static class DiscordController_UpdatePresence_Patch
        {
            private static bool Prefix(DiscordController __instance)
            {
                if (!_isEnabled)
                {
                    return true;
                }

                var discord = Traverse.Create(__instance).Field<global::Discord.Discord>("discord").Value;

                if (discord == null)
                {
                    _modEntry.Logger.Warning("[ChangeDiscordRP] Discord instance is null. Cannot update presence.");
                    return true;
                }

                try
                {
                    var activity = new global::Discord.Activity
                    {
                        State = Validate(_settings.CustomState),
                        Details = Validate(_settings.CustomDetails),
                        Assets =
                        {
                            LargeImage = "planets_icon_stars",
                            LargeText = Validate(_settings.CustomLargeText)
                        }
                    };

                    discord.GetActivityManager().UpdateActivity(activity, result =>
                    {
                        if (result != global::Discord.Result.Ok)
                        {
                            _modEntry.Logger.Warning($"[ChangeDiscordRP] Discord UpdateActivity failed: {result}");
                        }
                    });
                }
                finally
                {
                }

                return false;
            }

            private static string Validate(string s)
            {
                return s.Length <= 128 ? s : s.Substring(0, 128) + "...";
            }
        }
    }
}