using System.Linq;
using System.Reflection;
using HarmonyLib;
using NoteTweaks.Configuration;
using NoteTweaks.Utils;
using UnityEngine;

namespace NoteTweaks.Patches
{
    [HarmonyPatch]
    [HarmonyPriority(Priority.LowerThanNormal)]
    internal class NoteColorTweaks
    {
        private static PluginConfig Config => PluginConfig.Instance;

        internal static ColorScheme PrePatchedScheme;
        internal static ColorScheme PatchedScheme;
        
        private static ColorScheme PatchColors(ColorSchemeSO schemeObj)
        {
            PatchedScheme = new ColorScheme(schemeObj)
            {
                _colorSchemeId = "NoteTweaksPatched",
                _colorSchemeNameLocalizationKey = "NoteTweaksPatched",
                _useNonLocalizedName = true,
                _nonLocalizedName = "NoteTweaksPatched",
                _isEditable = false
            };
            
            float leftScale = 1.0f + Config.ColorBoostLeft;
            float rightScale = 1.0f + Config.ColorBoostRight;
            
            Color leftColor = PatchedScheme._saberAColor;
            float leftBrightness = leftColor.Brightness();
            Color rightColor = PatchedScheme._saberBColor;
            float rightBrightness = rightColor.Brightness();

            if (leftBrightness > Config.LeftMaxBrightness)
            {
                leftColor = leftColor.LerpRGBUnclamped(Color.black, Mathf.InverseLerp(leftBrightness, 0.0f, Config.LeftMaxBrightness));
            }
            else if (leftBrightness < Config.LeftMinBrightness)
            {
                leftColor = leftColor.LerpRGBUnclamped(Color.white, Mathf.InverseLerp(leftBrightness, 1.0f, Config.LeftMinBrightness));
            }
            
            if (rightBrightness > Config.RightMaxBrightness)
            {
                rightColor = rightColor.LerpRGBUnclamped(Color.black, Mathf.InverseLerp(rightBrightness, 0.0f, Config.RightMaxBrightness));
            }
            else if (rightBrightness < Config.RightMinBrightness)
            {
                rightColor = rightColor.LerpRGBUnclamped(Color.white, Mathf.InverseLerp(rightBrightness, 1.0f, Config.RightMinBrightness));
            }
            
            PatchedScheme._saberAColor = leftColor * leftScale;
            PatchedScheme._saberAColor.a = 1f;
            PatchedScheme._saberBColor = rightColor * rightScale;
            PatchedScheme._saberBColor.a = 1f;

            return PatchedScheme;
        }
        
        private static void ApplyPatchedColors(StandardLevelScenesTransitionSetupDataSO setupData)
        {
            if (!Config.Enabled || NotePhysicalTweaks.AutoDisable)
            {
                return;
            }

            PrePatchedScheme = setupData.colorScheme;
            
            ColorSchemeSO schemeObj = ScriptableObject.CreateInstance<ColorSchemeSO>();
            schemeObj._colorScheme = setupData.colorScheme;

            ColorScheme patchedColors = PatchColors(schemeObj);

            setupData.usingOverrideColorScheme = true;
            setupData.colorScheme = patchedColors;
        }
        
        private static void ApplyPatchedColors(MultiplayerLevelScenesTransitionSetupDataSO setupData)
        {
            if (!Config.Enabled || NotePhysicalTweaks.AutoDisable)
            {
                return;
            }
            
            PrePatchedScheme = setupData.colorScheme;
            
            ColorSchemeSO schemeObj = ScriptableObject.CreateInstance<ColorSchemeSO>();
            schemeObj._colorScheme = setupData.colorScheme;

            ColorScheme patchedColors = PatchColors(schemeObj);

            setupData.usingOverrideColorScheme = true;
            setupData.colorScheme = patchedColors;
        }

        [HarmonyPatch]
        [HarmonyPriority(Priority.LowerThanNormal)]
        private static class StandardLevelScenesTransitionSetupDataColorPatch
        {
            private static MethodInfo TargetMethod() => AccessTools.FirstMethod(
                typeof(StandardLevelScenesTransitionSetupDataSO),
                m => m.Name == nameof(StandardLevelScenesTransitionSetupDataSO.Init) &&
#if V1_40_8
                     m.GetParameters().All(p => p.ParameterType != typeof(IBeatmapLevelData)));
#else
                     m.GetParameters().Any(p => p.ParameterType == typeof(IBeatmapLevelData)));
#endif

            [HarmonyPostfix]
            // ReSharper disable once InconsistentNaming
            private static void Postfix(StandardLevelScenesTransitionSetupDataSO __instance)
            {
                ApplyPatchedColors(__instance);
            }
        }

        [HarmonyPatch(typeof(MultiplayerLevelScenesTransitionSetupDataSO), "Init")]
        [HarmonyPriority(Priority.LowerThanNormal)]
        private static class MultiplayerLevelScenesTransitionSetupDataColorPatch
        {
            [HarmonyPostfix]
            // ReSharper disable once InconsistentNaming
            private static void Postfix(MultiplayerLevelScenesTransitionSetupDataSO __instance)
            {
                ApplyPatchedColors(__instance);
            }
        }

        [HarmonyPatch(typeof(StandardLevelRestartController), "RestartLevel")]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.LowerThanNormal)]
        // ReSharper disable once InconsistentNaming
        private static void StandardLevelRestartControllerPatch(StandardLevelRestartController __instance)
        {
            if (!Config.Enabled|| NotePhysicalTweaks.AutoDisable)
            {
                return;
            }
            
            ColorSchemeSO schemeObj = ScriptableObject.CreateInstance<ColorSchemeSO>();
            schemeObj._colorScheme = PrePatchedScheme;
            ColorScheme patchedColors = PatchColors(schemeObj);

            __instance._standardLevelSceneSetupData.usingOverrideColorScheme = true;
            __instance._standardLevelSceneSetupData.colorScheme = patchedColors;
        }
    }
}