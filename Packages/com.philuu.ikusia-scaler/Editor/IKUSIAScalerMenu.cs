using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;

namespace IKUSIAScaler.Editor
{
    /// <summary>
    /// Data structure for avatar conversion scaling profiles
    /// </summary>
    [System.Serializable]
    public class ScalingProfile
    {
        public string sourceAvatar;
        public string targetAvatar;
        public float armatureMultiplier;
        public Dictionary<string, float> boneMultipliers;

        public ScalingProfile(string source, string target, float armature, Dictionary<string, float> bones = null)
        {
            sourceAvatar = source;
            targetAvatar = target;
            armatureMultiplier = armature;
            boneMultipliers = bones ?? new Dictionary<string, float>();
        }

        public string GetMenuPath()
        {
            return $"IKUSIA Scaler/{sourceAvatar} → {targetAvatar}";
        }

        public string GetDisplayName()
        {
            return $"{sourceAvatar} → {targetAvatar}";
        }
    }

    public static class IKUSIAScalerMenu
    {
        private enum AvatarType
        {
            Unknown,
            Mizuki,
            Rurune,
            Kaguya
        }

        private enum UILanguage
        {
            English,
            Japanese
        }

        // Debug logging toggle - set to true to enable debug output
        private const bool DEBUG_LOGGING = false;
        private const float UNIT_SCALE_TOLERANCE = 0.01f;
        private const string LANGUAGE_PREF_KEY = "IKUSIA_Scaler_UILanguage";
        private const string LANGUAGE_PROMPT_DONE_KEY = "IKUSIA_Scaler_LanguagePromptDone";

        // All conversion profiles
        private static readonly List<ScalingProfile> conversionProfiles = new List<ScalingProfile>()
        {
            // Mizuki conversions
            new ScalingProfile("Mizuki", "Rurune", 0.95f, new Dictionary<string, float> { { "Neck", 1.01f } }),
            new ScalingProfile("Mizuki", "Kaguya", 0.8075f),

            // Rurune conversions
            new ScalingProfile("Rurune", "Mizuki", 1.0526316f),
            new ScalingProfile("Rurune", "Kaguya", 0.85f),

            // Kaguya conversions
            new ScalingProfile("Kaguya", "Mizuki", 1.23839f, new Dictionary<string, float> { { "Neck", 0.97015f } }),
            new ScalingProfile("Kaguya", "Rurune", 1.17647f)
        };

        // Menu item priority for proper ordering
        private const int MENU_PRIORITY = 0;

        [InitializeOnLoadMethod]
        private static void InitializeLanguagePreferencePrompt()
        {
            EditorApplication.delayCall += () =>
            {
                if (!EditorPrefs.GetBool(LANGUAGE_PROMPT_DONE_KEY, false))
                {
                    ShowLanguageSelectionDialog(true);
                }
            };
        }

        [MenuItem("Window/IKUSIA Scaler Settings")]
        private static void OpenIKUSIAScalerSettings()
        {
            ShowLanguageSelectionDialog(false);
        }

        [MenuItem("GameObject/IKUSIA Scaler/Mizuki → Rurune", false, MENU_PRIORITY)]
        private static void ConvertMizukiToRuruneEnglish()
        {
            ApplyConversion(conversionProfiles[0]);
        }

        [MenuItem("GameObject/IKUSIA Scaler/Mizuki → Rurune", true, MENU_PRIORITY)]
        private static bool ValidateConvertMizukiToRuruneEnglish()
        {
            return ValidateConversionMenuForLanguage(0, UILanguage.English);
        }

        [MenuItem("GameObject/IKUSIA Scaler/Mizuki → Kaguya", false, MENU_PRIORITY + 1)]
        private static void ConvertMizukiToKaguyaEnglish()
        {
            ApplyConversion(conversionProfiles[1]);
        }

        [MenuItem("GameObject/IKUSIA Scaler/Mizuki → Kaguya", true, MENU_PRIORITY + 1)]
        private static bool ValidateConvertMizukiToKaguyaEnglish()
        {
            return ValidateConversionMenuForLanguage(1, UILanguage.English);
        }

        [MenuItem("GameObject/IKUSIA Scaler/Rurune → Mizuki", false, MENU_PRIORITY + 2)]
        private static void ConvertRuruneToMizukiEnglish()
        {
            ApplyConversion(conversionProfiles[2]);
        }

        [MenuItem("GameObject/IKUSIA Scaler/Rurune → Mizuki", true, MENU_PRIORITY + 2)]
        private static bool ValidateConvertRuruneToMizukiEnglish()
        {
            return ValidateConversionMenuForLanguage(2, UILanguage.English);
        }

        [MenuItem("GameObject/IKUSIA Scaler/Rurune → Kaguya", false, MENU_PRIORITY + 3)]
        private static void ConvertRuruneToKaguyaEnglish()
        {
            ApplyConversion(conversionProfiles[3]);
        }

        [MenuItem("GameObject/IKUSIA Scaler/Rurune → Kaguya", true, MENU_PRIORITY + 3)]
        private static bool ValidateConvertRuruneToKaguyaEnglish()
        {
            return ValidateConversionMenuForLanguage(3, UILanguage.English);
        }

        [MenuItem("GameObject/IKUSIA Scaler/Kaguya → Mizuki", false, MENU_PRIORITY + 4)]
        private static void ConvertKaguyaToMizukiEnglish()
        {
            ApplyConversion(conversionProfiles[4]);
        }

        [MenuItem("GameObject/IKUSIA Scaler/Kaguya → Mizuki", true, MENU_PRIORITY + 4)]
        private static bool ValidateConvertKaguyaToMizukiEnglish()
        {
            return ValidateConversionMenuForLanguage(4, UILanguage.English);
        }

        [MenuItem("GameObject/IKUSIA Scaler/Kaguya → Rurune", false, MENU_PRIORITY + 5)]
        private static void ConvertKaguyaToRuruneEnglish()
        {
            ApplyConversion(conversionProfiles[5]);
        }

        [MenuItem("GameObject/IKUSIA Scaler/Kaguya → Rurune", true, MENU_PRIORITY + 5)]
        private static bool ValidateConvertKaguyaToRuruneEnglish()
        {
            return ValidateConversionMenuForLanguage(5, UILanguage.English);
        }

        [MenuItem("GameObject/IKUSIA Scaler/瑞希 → ルルネ", false, MENU_PRIORITY + 6)]
        private static void ConvertMizukiToRuruneJapanese()
        {
            ApplyConversion(conversionProfiles[0]);
        }

        [MenuItem("GameObject/IKUSIA Scaler/瑞希 → ルルネ", true, MENU_PRIORITY + 6)]
        private static bool ValidateConvertMizukiToRuruneJapanese()
        {
            return ValidateConversionMenuForLanguage(0, UILanguage.Japanese);
        }

        [MenuItem("GameObject/IKUSIA Scaler/瑞希 → 輝夜", false, MENU_PRIORITY + 7)]
        private static void ConvertMizukiToKaguyaJapanese()
        {
            ApplyConversion(conversionProfiles[1]);
        }

        [MenuItem("GameObject/IKUSIA Scaler/瑞希 → 輝夜", true, MENU_PRIORITY + 7)]
        private static bool ValidateConvertMizukiToKaguyaJapanese()
        {
            return ValidateConversionMenuForLanguage(1, UILanguage.Japanese);
        }

        [MenuItem("GameObject/IKUSIA Scaler/ルルネ → 瑞希", false, MENU_PRIORITY + 8)]
        private static void ConvertRuruneToMizukiJapanese()
        {
            ApplyConversion(conversionProfiles[2]);
        }

        [MenuItem("GameObject/IKUSIA Scaler/ルルネ → 瑞希", true, MENU_PRIORITY + 8)]
        private static bool ValidateConvertRuruneToMizukiJapanese()
        {
            return ValidateConversionMenuForLanguage(2, UILanguage.Japanese);
        }

        [MenuItem("GameObject/IKUSIA Scaler/ルルネ → 輝夜", false, MENU_PRIORITY + 9)]
        private static void ConvertRuruneToKaguyaJapanese()
        {
            ApplyConversion(conversionProfiles[3]);
        }

        [MenuItem("GameObject/IKUSIA Scaler/ルルネ → 輝夜", true, MENU_PRIORITY + 9)]
        private static bool ValidateConvertRuruneToKaguyaJapanese()
        {
            return ValidateConversionMenuForLanguage(3, UILanguage.Japanese);
        }

        [MenuItem("GameObject/IKUSIA Scaler/輝夜 → 瑞希", false, MENU_PRIORITY + 10)]
        private static void ConvertKaguyaToMizukiJapanese()
        {
            ApplyConversion(conversionProfiles[4]);
        }

        [MenuItem("GameObject/IKUSIA Scaler/輝夜 → 瑞希", true, MENU_PRIORITY + 10)]
        private static bool ValidateConvertKaguyaToMizukiJapanese()
        {
            return ValidateConversionMenuForLanguage(4, UILanguage.Japanese);
        }

        [MenuItem("GameObject/IKUSIA Scaler/輝夜 → ルルネ", false, MENU_PRIORITY + 11)]
        private static void ConvertKaguyaToRuruneJapanese()
        {
            ApplyConversion(conversionProfiles[5]);
        }

        [MenuItem("GameObject/IKUSIA Scaler/輝夜 → ルルネ", true, MENU_PRIORITY + 11)]
        private static bool ValidateConvertKaguyaToRuruneJapanese()
        {
            return ValidateConversionMenuForLanguage(5, UILanguage.Japanese);
        }

        private static bool ValidateConversionMenu(int profileIndex)
        {
            GameObject selectedObject = Selection.activeGameObject;
            if (selectedObject == null)
            {
                return false;
            }

            if (profileIndex < 0 || profileIndex >= conversionProfiles.Count)
            {
                return false;
            }

            AvatarType detectedAvatarType = DetectAvatarTypeFromContext(selectedObject.transform);
            if (detectedAvatarType == AvatarType.Unknown)
            {
                // If we cannot detect avatar type confidently, keep all conversions available.
                return true;
            }

            string detectedTargetName = AvatarTypeToProfileName(detectedAvatarType);
            return conversionProfiles[profileIndex].targetAvatar.Equals(detectedTargetName, StringComparison.OrdinalIgnoreCase);
        }

        private static bool ValidateConversionMenuForLanguage(int profileIndex, UILanguage language)
        {
            return GetCurrentLanguage() == language && ValidateConversionMenu(profileIndex);
        }

        private static AvatarType DetectAvatarTypeFromContext(Transform selectedTransform)
        {
            if (selectedTransform == null)
            {
                return AvatarType.Unknown;
            }

            // Strict mode: only use the Animator on the selected hierarchy's top-level root.
            Transform avatarRoot = selectedTransform.root;
            return DetectAvatarTypeFromAvatarRoot(avatarRoot);
        }

        private static AvatarType DetectAvatarTypeFromAvatarRoot(Transform avatarRoot)
        {
            if (avatarRoot == null)
            {
                return AvatarType.Unknown;
            }

            Animator animator = avatarRoot.GetComponent<Animator>();
            if (animator == null)
            {
                return AvatarType.Unknown;
            }

            return DetectAvatarTypeFromAnimator(avatarRoot, animator);
        }

        private static AvatarType DetectAvatarTypeFromAnimator(Transform avatarRoot, Animator animator)
        {
            if (animator == null)
            {
                return AvatarType.Unknown;
            }

            List<string> searchCandidates = new List<string>();

            if (animator.avatar != null)
            {
                searchCandidates.Add(animator.avatar.name);
                searchCandidates.Add(AssetDatabase.GetAssetPath(animator.avatar));
            }

            if (searchCandidates.Count == 0)
            {
                return AvatarType.Unknown;
            }

            string combined = string.Join(" ", searchCandidates).ToLowerInvariant();

            bool hasMizuki = combined.Contains("mizuki");
            bool hasRurune = combined.Contains("rurune");
            bool hasKaguya = combined.Contains("kaguya");

            int matchCount = (hasMizuki ? 1 : 0) + (hasRurune ? 1 : 0) + (hasKaguya ? 1 : 0);
            if (matchCount != 1)
            {
                return AvatarType.Unknown;
            }

            if (hasMizuki)
            {
                return AvatarType.Mizuki;
            }

            if (hasRurune)
            {
                return AvatarType.Rurune;
            }

            return AvatarType.Kaguya;
        }

        private static string AvatarTypeToProfileName(AvatarType avatarType)
        {
            switch (avatarType)
            {
                case AvatarType.Mizuki:
                    return "Mizuki";
                case AvatarType.Rurune:
                    return "Rurune";
                case AvatarType.Kaguya:
                    return "Kaguya";
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Core conversion logic - applies the scaling profile to the selected GameObject
        /// </summary>
        private static void ApplyConversion(ScalingProfile profile)
        {
            GameObject selectedObject = Selection.activeGameObject;

            if (selectedObject == null)
            {
                EditorUtility.DisplayDialog(
                    L("IKUSIA Scaler", "IKUSIA Scaler"),
                    L("Please select a GameObject in the hierarchy.", "HierarchyでGameObjectを選択してください。"),
                    L("OK", "OK")
                );
                return;
            }

            DebugLog($"Processing conversion: {profile.GetDisplayName()}");
            DebugLog($"Selected object: {selectedObject.name}");

            // Find the Armature
            Transform armature = FindArmature(selectedObject.transform);

            if (armature == null)
            {
                Debug.LogWarning($"[IKUSIA Scaler] No Armature found in '{selectedObject.name}' or its children. " +
                    "Please ensure the selected GameObject contains an object with 'Armature' in its name.");
                EditorUtility.DisplayDialog(
                    L("IKUSIA Scaler - Armature Not Found", "IKUSIA Scaler - Armatureが見つかりません"),
                    L(
                        $"No Armature found in '{selectedObject.name}' or its children.\n\nThe tool searches for any child GameObject with 'Armature' in its name (case-insensitive).",
                        $"'{selectedObject.name}' またはその子オブジェクト内にArmatureが見つかりません。\n\nこのツールは名前に 'Armature' を含む子GameObjectを検索します（大文字小文字を区別しません）。"
                    ),
                    L("OK", "OK")
                );
                return;
            }

            DebugLog($"Found Armature: {armature.name}");

            if (!RunPreflightValidation(selectedObject.transform, armature, profile))
            {
                DebugLog("Conversion cancelled by user during preflight validation.");
                return;
            }

            // Record undo for the armature
            Undo.RecordObject(armature, $"IKUSIA Scaler: {profile.GetDisplayName()}");

            // Apply armature scaling
            Vector3 previousScale = armature.localScale;
            armature.localScale = MultiplyScale(armature.localScale, profile.armatureMultiplier);
            DebugLog($"Armature scale changed: {previousScale} → {armature.localScale}");

            // Apply bone-specific scaling if defined
            bool allBonesFound = true;
            foreach (var boneEntry in profile.boneMultipliers)
            {
                string boneName = boneEntry.Key;
                float multiplier = boneEntry.Value;

                Transform bone = FindBoneInArmature(armature, boneName);

                if (bone != null)
                {
                    DebugLog($"Found bone: {bone.name}");
                    Undo.RecordObject(bone, $"IKUSIA Scaler: {profile.GetDisplayName()}");

                    Vector3 previousBoneScale = bone.localScale;
                    bone.localScale = MultiplyScale(bone.localScale, multiplier);
                    DebugLog($"{boneName} scale changed: {previousBoneScale} → {bone.localScale}");
                }
                else
                {
                    Debug.LogWarning($"[IKUSIA Scaler] '{boneName}' bone not found in Armature '{armature.name}'. " +
                        $"Skipping {boneName} scaling for {profile.GetDisplayName()} conversion.");
                    allBonesFound = false;
                }
            }

            // Display success message
            string resultMessage = $"Applied {profile.GetDisplayName()} conversion to '{selectedObject.name}'.\n\n" +
                                   $"Armature: {armature.name}\n" +
                                   $"Scale multiplier: {profile.armatureMultiplier}";

            if (profile.boneMultipliers.Count > 0)
            {
                resultMessage += $"\n\nBone adjustments:";
                foreach (var bone in profile.boneMultipliers)
                {
                    resultMessage += $"\n• {bone.Key}: {bone.Value}x";
                }

                if (!allBonesFound)
                {
                    resultMessage += "\n\n⚠ Some bones were not found (see Console for details).";
                }
            }

            DebugLog("Conversion completed successfully");

            if (!allBonesFound)
            {
                EditorUtility.DisplayDialog(
                    L("IKUSIA Scaler - Partial Success", "IKUSIA Scaler - 一部成功"),
                    resultMessage,
                    L("OK", "OK")
                );
            }
            else
            {
                // Only show success dialog in debug mode or if user wants confirmation
                // For production, silent success is better UX
                DebugLog(resultMessage);
            }
        }

        /// <summary>
        /// Finds the Armature GameObject in the hierarchy
        /// Searches recursively through all children
        /// </summary>
        private static Transform FindArmature(Transform root)
        {
            // Check the root itself
            if (IsArmature(root.name))
            {
                return root;
            }

            // Search all children recursively
            foreach (Transform child in root)
            {
                if (IsArmature(child.name))
                {
                    return child;
                }

                // Recursive search
                Transform found = FindArmature(child);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        /// <summary>
        /// Checks if a GameObject name represents an Armature
        /// Case-insensitive, supports Unity duplicate naming
        /// </summary>
        private static bool IsArmature(string name)
        {
            return name.ToLowerInvariant().Contains("armature");
        }

        /// <summary>
        /// Runs preflight warnings and lets the user decide whether to continue.
        /// </summary>
        private static bool RunPreflightValidation(Transform selectedRoot, Transform armature, ScalingProfile profile)
        {
            string avatarRootMarkers;
            if (LooksLikeAvatarRootSelection(selectedRoot, armature, out avatarRootMarkers))
            {
                string avatarRootMessage =
                    L(
                        $"The selected object '{selectedRoot.name}' looks like an Avatar Root (top-level object with an Armature child).\n\nIKUSIA Scaler is meant to be used on an outfit root. Applying conversion from avatar root can scale unexpected parts.\n\nDetected avatar markers: {avatarRootMarkers}\nDetected Armature: {armature.name}\nRequested conversion: {profile.GetDisplayName()}\n\nContinue anyway?",
                        $"選択したオブジェクト '{selectedRoot.name}' はAvatar Rootの可能性があります（Armatureを子に持つ最上位オブジェクト）。\n\nIKUSIA Scalerは衣装のRootに対して使用する想定です。Avatar Rootから変換を適用すると、意図しない部分がスケールされる可能性があります。\n\n検出されたアバターマーカー: {avatarRootMarkers}\n検出されたArmature: {armature.name}\n要求された変換: {profile.GetDisplayName()}\n\nこのまま続行しますか？"
                    );

                bool continueFromAvatarRoot = EditorUtility.DisplayDialog(
                    L("IKUSIA Scaler - Selection Warning", "IKUSIA Scaler - 選択警告"),
                    avatarRootMessage,
                    L("Continue", "続行"),
                    L("Cancel", "キャンセル")
                );

                if (!continueFromAvatarRoot)
                {
                    return false;
                }
            }

            if (!IsApproximatelyUnitScale(armature.localScale, UNIT_SCALE_TOLERANCE))
            {
                string scaleWarningMessage =
                    L(
                        $"The detected Armature '{armature.name}' is not near 1/1/1 scale.\n\nCurrent Armature scale: X {armature.localScale.x:F4}, Y {armature.localScale.y:F4}, Z {armature.localScale.z:F4}\nUnit tolerance: +/- {UNIT_SCALE_TOLERANCE:F2}\n\nThis may indicate it was already modified. Continuing will multiply the current scale and may compound previous edits.\n\nRequested conversion: {profile.GetDisplayName()}\n\nContinue anyway?",
                        $"検出されたArmature '{armature.name}' は1/1/1スケール付近ではありません。\n\n現在のArmatureスケール: X {armature.localScale.x:F4}, Y {armature.localScale.y:F4}, Z {armature.localScale.z:F4}\n判定許容値: +/- {UNIT_SCALE_TOLERANCE:F2}\n\nすでに変更済みである可能性があります。このまま続行すると現在のスケールに乗算され、以前の調整がさらに積み重なる可能性があります。\n\n要求された変換: {profile.GetDisplayName()}\n\nこのまま続行しますか？"
                    );

                bool continueFromScaleWarning = EditorUtility.DisplayDialog(
                    L("IKUSIA Scaler - Armature Scale Warning", "IKUSIA Scaler - Armatureスケール警告"),
                    scaleWarningMessage,
                    L("Continue", "続行"),
                    L("Cancel", "キャンセル")
                );

                if (!continueFromScaleWarning)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Heuristic check for likely avatar root selection.
        /// </summary>
        private static bool LooksLikeAvatarRootSelection(Transform selectedRoot, Transform armature, out string markers)
        {
            markers = "None";

            if (selectedRoot == null || armature == null)
            {
                return false;
            }

            // Most avatar roots are top-level objects with an Armature somewhere under them.
            if (selectedRoot.parent != null || selectedRoot == armature || !armature.IsChildOf(selectedRoot))
            {
                return false;
            }

            List<string> foundMarkers = new List<string>();

            if (selectedRoot.GetComponent<Animator>() != null)
            {
                foundMarkers.Add("Animator");
            }

            if (HasComponentWithTypeName(selectedRoot.gameObject, "VRC.SDK3.Avatars.Components.VRCAvatarDescriptor"))
            {
                foundMarkers.Add("VRCAvatarDescriptor");
            }

            if (HasComponentWithTypeName(selectedRoot.gameObject, "VRC.Core.PipelineManager"))
            {
                foundMarkers.Add("PipelineManager");
            }

            if (foundMarkers.Count == 0)
            {
                return false;
            }

            markers = string.Join(", ", foundMarkers);
            return true;
        }

        /// <summary>
        /// Checks if a GameObject has a component by full type name without hard SDK dependency.
        /// </summary>
        private static bool HasComponentWithTypeName(GameObject target, string fullTypeName)
        {
            Component[] components = target.GetComponents<Component>();

            foreach (Component component in components)
            {
                if (component == null)
                {
                    continue;
                }

                if (component.GetType().FullName == fullTypeName)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if scale is approximately unit scale with tolerance.
        /// </summary>
        private static bool IsApproximatelyUnitScale(Vector3 scale, float tolerance)
        {
            return Mathf.Abs(scale.x - 1f) <= tolerance &&
                   Mathf.Abs(scale.y - 1f) <= tolerance &&
                   Mathf.Abs(scale.z - 1f) <= tolerance;
        }

        /// <summary>
        /// Finds a specific bone within the Armature hierarchy
        /// </summary>
        private static Transform FindBoneInArmature(Transform armature, string boneName)
        {
            // Check direct children first
            foreach (Transform child in armature)
            {
                if (IsBoneMatch(child.name, boneName))
                {
                    return child;
                }
            }

            // Search recursively
            foreach (Transform child in armature)
            {
                Transform found = FindBoneRecursive(child, boneName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        /// <summary>
        /// Recursive bone search helper
        /// </summary>
        private static Transform FindBoneRecursive(Transform parent, string boneName)
        {
            foreach (Transform child in parent)
            {
                if (IsBoneMatch(child.name, boneName))
                {
                    return child;
                }

                Transform found = FindBoneRecursive(child, boneName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        /// <summary>
        /// Checks if a bone name matches the target (case-insensitive, handles Unity naming)
        /// </summary>
        private static bool IsBoneMatch(string actualName, string targetName)
        {
            // Direct case-insensitive match
            if (actualName.Equals(targetName, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Handle Unity duplicate naming: "Neck.1", "Neck (1)", etc.
            string lowerActual = actualName.ToLowerInvariant();
            string lowerTarget = targetName.ToLowerInvariant();

            // Check if it starts with the target name and then has a Unity suffix
            if (lowerActual.StartsWith(lowerTarget))
            {
                string remainder = lowerActual.Substring(lowerTarget.Length);
                // Common Unity suffixes: ".1", " (1)", "(Clone)", etc.
                if (remainder.Length == 0 || 
                    remainder.StartsWith(".") || 
                    remainder.StartsWith(" (") || 
                    remainder.StartsWith("("))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Multiplies each component of a Vector3 scale by a multiplier
        /// This preserves individual axis scaling
        /// </summary>
        private static Vector3 MultiplyScale(Vector3 currentScale, float multiplier)
        {
            return new Vector3(
                currentScale.x * multiplier,
                currentScale.y * multiplier,
                currentScale.z * multiplier
            );
        }

        private static UILanguage GetCurrentLanguage()
        {
            string stored = EditorPrefs.GetString(LANGUAGE_PREF_KEY, UILanguage.English.ToString());
            return stored == UILanguage.Japanese.ToString() ? UILanguage.Japanese : UILanguage.English;
        }

        private static void SetCurrentLanguage(UILanguage language)
        {
            EditorPrefs.SetString(LANGUAGE_PREF_KEY, language.ToString());
            EditorPrefs.SetBool(LANGUAGE_PROMPT_DONE_KEY, true);
        }

        private static string L(string english, string japanese)
        {
            return GetCurrentLanguage() == UILanguage.Japanese ? japanese : english;
        }

        private static void ShowLanguageSelectionDialog(bool isFirstRun)
        {
            int choice = EditorUtility.DisplayDialogComplex(
                "IKUSIA Scaler - Language / 言語設定",
                "Please select your language preference.\n言語設定を選択してください。\n\nEnglish or Japanese can be changed anytime from:\nWindow > IKUSIA Scaler Settings\n\n英語または日本語はいつでも次から変更できます:\nWindow > IKUSIA Scaler Settings",
                "English",
                "日本語",
                isFirstRun ? "Later" : "Cancel"
            );

            if (choice == 0)
            {
                SetCurrentLanguage(UILanguage.English);
                EditorUtility.DisplayDialog(
                    "IKUSIA Scaler",
                    "Language set to English.\n\nYou can reopen this language popup from:\nWindow > IKUSIA Scaler Settings",
                    "OK"
                );
                return;
            }

            if (choice == 1)
            {
                SetCurrentLanguage(UILanguage.Japanese);
                EditorUtility.DisplayDialog(
                    "IKUSIA Scaler",
                    "言語を日本語に設定しました。\n\nこの言語設定ポップアップは次から再表示できます:\nWindow > IKUSIA Scaler Settings",
                    "OK"
                );
                return;
            }

            if (isFirstRun)
            {
                SetCurrentLanguage(UILanguage.English);
            }
        }

        /// <summary>
        /// Debug logging helper
        /// </summary>
        private static void DebugLog(string message)
        {
            if (DEBUG_LOGGING)
            {
                Debug.Log($"[IKUSIA Scaler] {message}");
            }
        }
    }
}
