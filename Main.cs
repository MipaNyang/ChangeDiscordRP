using System;
using System.IO;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityModManagerNet;
using static UnityModManagerNet.UnityModManager;
using Newtonsoft.Json;

namespace ChangeDiscordRP
{
    public class Main
    {
        private static Harmony harmony;
        private static ModSettings settings;
        private static ModEntry modEntry;

        [Serializable]
        public class ModSettings : UnityModManager.ModSettings
        {
            public string CustomState = "Default State";
            public string CustomDetails = "Default Details";
            public string CustomLargeText = "Default Large Text";

            private static string GetSettingsFilePath()
            {
                string folderPath = modEntry.Path;
                string filePath = Path.Combine(folderPath, "Settings.json");
                return filePath;
            }

            public override void Save(UnityModManager.ModEntry modEntry)
            {
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                string filePath = GetSettingsFilePath();
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                File.WriteAllText(filePath, json);
            }

            public static ModSettings Load()
            {
                string filePath = GetSettingsFilePath();

                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    return JsonConvert.DeserializeObject<ModSettings>(json);
                }

                return new ModSettings();
            }
        }

        public static bool Start(ModEntry entry)
        {
            modEntry = entry;
            settings = ModSettings.Load();

            entry.OnToggle = (e, value) =>
            {
                if (value)
                {
                    harmony = new Harmony(entry.Info.Id);
                    harmony.PatchAll(Assembly.GetExecutingAssembly());
                }
                else
                {
                    harmony?.UnpatchAll(entry.Info.Id);
                }
                return true;
            };

            entry.OnGUI = (e) =>
            {
                DrawSettingsUI();
            };

            return true;
        }

        private static void DrawSettingsUI()
        {
            GUILayout.BeginVertical();

            GUILayout.Label("<b><size=30>ChangeDiscordRP</size></b>", GUILayout.Width(300));

            GUILayout.Label("Custom State");
            settings.CustomState = GUILayout.TextField(settings.CustomState, GUILayout.Width(300));

            GUILayout.Label("Custom Details");
            settings.CustomDetails = GUILayout.TextField(settings.CustomDetails, GUILayout.Width(300));

            GUILayout.Label("Custom Large Text");
            settings.CustomLargeText = GUILayout.TextField(settings.CustomLargeText, GUILayout.Width(300));

            if (GUILayout.Button("Save Settings"))
            {
                settings.Save(modEntry);
            }

            GUILayout.EndVertical();
        }

        [HarmonyPatch(typeof(DiscordController), "UpdatePresence")]
        internal class DiscordMessagePatch
        {
            private static bool Prefix(DiscordController __instance)
            {
                var discord = Traverse.Create(__instance).Field<global::Discord.Discord>("discord").Value;

                if (discord == null)
                {
                    return false;
                }

                string customState = settings.CustomState;
                string customDetails = settings.CustomDetails;
                string customLargeText = settings.CustomLargeText;

                var activity = new global::Discord.Activity
                {
                    State = Validate(customState),
                    Details = Validate(customDetails),
                    Assets =
                    {
                        LargeImage = "planets_icon_stars",
                        LargeText = Validate(customLargeText)
                    }
                };

                discord.GetActivityManager().UpdateActivity(activity, result =>
                {
                });

                return false;
            }

            private static string Validate(string s)
            {
                if (s.Length <= 60)
                {
                    return s;
                }
                return s.Substring(0, 57) + "...";
            }
        }
    }
}
