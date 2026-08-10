using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using System.IO;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

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

        private enum AutoDetectionResult
        {
            NoSourceMatch,
            NoTargetMatch,
            SameAvatar,
            NoProfile,
            Detected
        }

        // Debug logging toggle - set to true to enable debug output
        private static readonly bool DEBUG_LOGGING = false;
        private const float UNIT_SCALE_TOLERANCE = 0.01f;
        private const string LANGUAGE_PREF_KEY = "IKUSIA_Scaler_UILanguage";
        private const string LANGUAGE_PROMPT_DONE_KEY = "IKUSIA_Scaler_LanguagePromptDone";
        private const string AUTO_CONVERT_ENABLED_KEY = "IKUSIA_Scaler_AutoConvertEnabled";
        private const string AUTO_CONVERT_DISCOVERY_PROMPT_DONE_KEY = "IKUSIA_Scaler_AutoConvertDiscoveryPromptDone";
        private const string AUTO_DETECTION_TRACE_LOGGING_ENABLED_KEY = "IKUSIA_Scaler_AutoDetectTraceLoggingEnabled";

        private static readonly HashSet<int> knownHierarchyObjectIds = new HashSet<int>();
        private static readonly Dictionary<int, int> knownHierarchyParentIds = new Dictionary<int, int>();
        private static readonly HashSet<int> processedPrefabRootIds = new HashSet<int>();
        private static readonly HashSet<int> pendingPrefabRootIds = new HashSet<int>();
        private static readonly HashSet<int> autoConvertedArmatureIds = new HashSet<int>();
        private static bool hierarchyTrackingInitialized;

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
                    IKUSIAScalerSettingsWindow.ShowWindow(true);
                }
            };
        }

        [InitializeOnLoadMethod]
        private static void InitializeAutomaticDetection()
        {
            EditorApplication.delayCall += () =>
            {
                if (hierarchyTrackingInitialized)
                {
                    return;
                }

                hierarchyTrackingInitialized = true;
                BuildHierarchySnapshot();
                EditorSceneManager.sceneOpened += OnSceneOpened;
                EditorApplication.hierarchyChanged += OnHierarchyChanged;
            };
        }

        [MenuItem("Window/IKUSIA Scaler Settings")]
        private static void OpenIKUSIAScalerSettings()
        {
            IKUSIAScalerSettingsWindow.ShowWindow(false);
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

            // Include avatar root object naming as a practical fallback (e.g., "Mizuki fbx").
            if (avatarRoot != null)
            {
                searchCandidates.Add(avatarRoot.name);
            }

            if (animator.avatar != null)
            {
                searchCandidates.Add(animator.avatar.name);

                string avatarAssetPath = AssetDatabase.GetAssetPath(animator.avatar);
                if (!string.IsNullOrEmpty(avatarAssetPath))
                {
                    searchCandidates.Add(avatarAssetPath);
                    AddPathSegmentsToCandidates(searchCandidates, avatarAssetPath);
                }
            }

            if (animator.runtimeAnimatorController != null)
            {
                searchCandidates.Add(animator.runtimeAnimatorController.name);

                string controllerAssetPath = AssetDatabase.GetAssetPath(animator.runtimeAnimatorController);
                if (!string.IsNullOrEmpty(controllerAssetPath))
                {
                    searchCandidates.Add(controllerAssetPath);
                    AddPathSegmentsToCandidates(searchCandidates, controllerAssetPath);
                }
            }

            if (searchCandidates.Count == 0)
            {
                return AvatarType.Unknown;
            }

            return DetectAvatarTypeFromCandidates(searchCandidates);
        }

        private static AvatarType DetectAvatarTypeFromCandidates(List<string> candidates)
        {
            if (candidates == null || candidates.Count == 0)
            {
                return AvatarType.Unknown;
            }

            foreach (string candidate in candidates)
            {
                AvatarType detected = DetectAvatarTypeFromSingleCandidate(candidate);
                if (detected != AvatarType.Unknown)
                {
                    return detected;
                }
            }

            return AvatarType.Unknown;
        }

        private static AvatarType DetectAvatarTypeFromSingleCandidate(string candidate)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                return AvatarType.Unknown;
            }

            string normalized = candidate.ToLowerInvariant();

            bool hasMizuki = normalized.Contains("mizuki");
            bool hasRurune = normalized.Contains("rurune");
            bool hasKaguya = normalized.Contains("kaguya");

            if (hasMizuki && !hasRurune && !hasKaguya)
            {
                return AvatarType.Mizuki;
            }

            if (hasRurune && !hasMizuki && !hasKaguya)
            {
                return AvatarType.Rurune;
            }

            if (hasKaguya && !hasMizuki && !hasRurune)
            {
                return AvatarType.Kaguya;
            }

            return AvatarType.Unknown;
        }

        private static void AddPathSegmentsToCandidates(List<string> candidates, string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return;
            }

            string[] pathSegments = assetPath.Split('/');
            foreach (string segment in pathSegments)
            {
                if (!string.IsNullOrWhiteSpace(segment))
                {
                    candidates.Add(segment);
                }
            }
        }

        private static AvatarType DetectAvatarTypeFromPrefab(GameObject prefabRoot)
        {
            if (prefabRoot == null)
            {
                return AvatarType.Unknown;
            }

            List<string> candidates = new List<string> { prefabRoot.name };

            GameObject sourcePrefab = PrefabUtility.GetCorrespondingObjectFromSource(prefabRoot);
            if (sourcePrefab != null)
            {
                candidates.Add(sourcePrefab.name);
                string assetPath = AssetDatabase.GetAssetPath(sourcePrefab);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    candidates.Add(assetPath);

                    string fileName = Path.GetFileNameWithoutExtension(assetPath);
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        candidates.Add(fileName);
                    }

                    string containingFolder = Path.GetFileName(Path.GetDirectoryName(assetPath));
                    if (!string.IsNullOrEmpty(containingFolder))
                    {
                        candidates.Add(containingFolder);
                    }
                }
            }

            return DetectAvatarTypeFromCandidates(candidates);
        }

        private static ScalingProfile FindConversionProfile(AvatarType sourceType, AvatarType targetType)
        {
            string sourceName = AvatarTypeToProfileName(sourceType);
            string targetName = AvatarTypeToProfileName(targetType);

            if (string.IsNullOrEmpty(sourceName) || string.IsNullOrEmpty(targetName))
            {
                return null;
            }

            foreach (ScalingProfile profile in conversionProfiles)
            {
                if (profile.sourceAvatar.Equals(sourceName, StringComparison.OrdinalIgnoreCase) &&
                    profile.targetAvatar.Equals(targetName, StringComparison.OrdinalIgnoreCase))
                {
                    return profile;
                }
            }

            return null;
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
            ApplyConversion(profile, Selection.activeGameObject, true);
        }

        /// <summary>
        /// Core conversion logic - applies the scaling profile to the target GameObject
        /// </summary>
        private static bool ApplyConversion(ScalingProfile profile, GameObject selectedObject, bool showUserDialogs)
        {
            if (selectedObject == null)
            {
                if (showUserDialogs)
                {
                    EditorUtility.DisplayDialog(
                        L("IKUSIA Scaler", "IKUSIA Scaler"),
                        L("Please select an outfit GameObject in the Hierarchy, then try again.", "Hierarchyで衣装のGameObjectを選択してから、もう一度お試しください。"),
                        L("OK", "OK")
                    );
                }

                return false;
            }

            DebugLog($"Processing conversion: {profile.GetDisplayName()}");
            DebugLog($"Selected object: {selectedObject.name}");

            // Find the Armature
            Transform armature = FindArmature(selectedObject.transform);

            if (armature == null)
            {
                Debug.LogWarning($"[IKUSIA Scaler] No Armature found in '{selectedObject.name}' or its children. " +
                    "Please ensure the selected GameObject contains an object with 'Armature' in its name.");
                if (showUserDialogs)
                {
                    EditorUtility.DisplayDialog(
                        L("IKUSIA Scaler - Couldn’t Find Armature", "IKUSIA Scaler - Armatureが見つかりません"),
                        L(
                            $"I couldn’t find an Armature under '{selectedObject.name}'.\n\nQuick check: this tool looks for any child GameObject whose name contains 'Armature' (case-insensitive).",
                            $"'{selectedObject.name}' 配下にArmatureが見つかりませんでした。\n\n補足: このツールは名前に 'Armature' を含む子GameObjectを検索します（大文字小文字は区別しません）。"
                        ),
                        L("OK", "OK")
                    );
                }

                return false;
            }

            DebugLog($"Found Armature: {armature.name}");

            if (!RunPreflightValidation(selectedObject.transform, armature, profile, showUserDialogs))
            {
                DebugLog("Conversion cancelled by user during preflight validation.");
                return false;
            }

            // Record undo for the armature
            Undo.RecordObject(armature, $"IKUSIA Scaler: {profile.GetDisplayName()}");

            // Apply armature scaling
            Vector3 previousScale = armature.localScale;
            Transform referenceArmature = FindReferenceAvatarArmature(selectedObject.transform, armature);
            if (referenceArmature != null)
            {
                armature.localScale = MultiplyScale(referenceArmature.localScale, profile.armatureMultiplier);
                DebugLog($"Armature scale changed using avatar reference '{referenceArmature.name}': {previousScale} → {armature.localScale}");
            }
            else
            {
                // Fallback when no avatar armature context is available.
                armature.localScale = MultiplyScale(armature.localScale, profile.armatureMultiplier);
                DebugLog($"Armature scale changed (fallback multiply): {previousScale} → {armature.localScale}");
            }

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
                if (showUserDialogs)
                {
                    EditorUtility.DisplayDialog(
                        L("IKUSIA Scaler - Partial Success", "IKUSIA Scaler - 一部成功"),
                        resultMessage,
                        L("OK", "OK")
                    );
                }
            }
            else
            {
                // Only show success dialog in debug mode or if user wants confirmation
                // For production, silent success is better UX
                DebugLog(resultMessage);
            }

            return true;
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
        /// Finds the destination avatar armature to use as scaling baseline.
        /// Excludes the selected outfit subtree so we don't accidentally use the outfit armature itself.
        /// </summary>
        private static Transform FindReferenceAvatarArmature(Transform selectedRoot, Transform outfitArmature)
        {
            if (selectedRoot == null)
            {
                return null;
            }

            Transform avatarRoot = selectedRoot.root;
            if (avatarRoot == null)
            {
                return null;
            }

            Animator animator = avatarRoot.GetComponent<Animator>();
            if (animator != null)
            {
                Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
                Transform fromHumanoidRig = FindArmatureFromBoneChain(hips, avatarRoot);
                if (IsValidReferenceArmature(fromHumanoidRig, selectedRoot, outfitArmature))
                {
                    return fromHumanoidRig;
                }
            }

            Transform fromNameSearch = FindArmatureExcludingSubtree(avatarRoot, selectedRoot);
            if (IsValidReferenceArmature(fromNameSearch, selectedRoot, outfitArmature))
            {
                return fromNameSearch;
            }

            return null;
        }

        /// <summary>
        /// Walks up from a humanoid bone to find the closest armature root under the avatar root.
        /// </summary>
        private static Transform FindArmatureFromBoneChain(Transform startBone, Transform avatarRoot)
        {
            Transform current = startBone;
            Transform bestMatch = null;

            while (current != null && current != avatarRoot)
            {
                if (IsArmature(current.name))
                {
                    bestMatch = current;
                }

                current = current.parent;
            }

            return bestMatch;
        }

        /// <summary>
        /// Finds an armature by name while skipping a subtree (typically the selected outfit root).
        /// </summary>
        private static Transform FindArmatureExcludingSubtree(Transform root, Transform excludedSubtreeRoot)
        {
            if (root == null)
            {
                return null;
            }

            if (excludedSubtreeRoot != null && root == excludedSubtreeRoot)
            {
                return null;
            }

            if (IsArmature(root.name))
            {
                return root;
            }

            foreach (Transform child in root)
            {
                Transform found = FindArmatureExcludingSubtree(child, excludedSubtreeRoot);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static bool IsValidReferenceArmature(Transform candidate, Transform selectedRoot, Transform outfitArmature)
        {
            if (candidate == null)
            {
                return false;
            }

            if (outfitArmature != null && candidate == outfitArmature)
            {
                return false;
            }

            if (selectedRoot != null && candidate.IsChildOf(selectedRoot))
            {
                return false;
            }

            return true;
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
        private static bool RunPreflightValidation(Transform selectedRoot, Transform armature, ScalingProfile profile, bool showUserDialogs)
        {
            string avatarRootMarkers;
            if (LooksLikeAvatarRootSelection(selectedRoot, armature, out avatarRootMarkers))
            {
                if (!showUserDialogs)
                {
                    Debug.LogWarning($"[IKUSIA Scaler] Skipped automatic conversion for '{selectedRoot.name}' because it looks like an avatar root.");
                    return false;
                }

                string avatarRootMessage =
                    L(
                        $"Heads up: '{selectedRoot.name}' looks like an Avatar Root (top-level object with an Armature child).\n\nIKUSIA Scaler is usually meant for outfit roots, so applying this here may scale parts you didn’t intend.\n\nDetected avatar markers: {avatarRootMarkers}\nDetected Armature: {armature.name}\nRequested conversion: {profile.GetDisplayName()}\n\nDo you want to continue anyway?",
                        $"ご注意: '{selectedRoot.name}' はAvatar Rootの可能性があります（Armatureを子に持つ最上位オブジェクト）。\n\nIKUSIA Scalerは通常、衣装のRootで使う想定です。ここで適用すると、意図しない箇所までスケールされる場合があります。\n\n検出されたアバターマーカー: {avatarRootMarkers}\n検出されたArmature: {armature.name}\n要求された変換: {profile.GetDisplayName()}\n\nこのまま続行しますか？"
                    );

                bool continueFromAvatarRoot = EditorUtility.DisplayDialog(
                    L("IKUSIA Scaler - Quick Warning", "IKUSIA Scaler - 注意"),
                    avatarRootMessage,
                    L("Continue Anyway", "このまま続行"),
                    L("Go Back", "戻る")
                );

                if (!continueFromAvatarRoot)
                {
                    return false;
                }
            }

            if (!IsApproximatelyUnitScale(armature.localScale, UNIT_SCALE_TOLERANCE))
            {
                if (!showUserDialogs)
                {
                    Debug.LogWarning($"[IKUSIA Scaler] Skipped automatic conversion for '{selectedRoot.name}' because Armature '{armature.name}' is already scaled.");
                    return false;
                }

                string scaleWarningMessage =
                    L(
                        $"Quick heads-up: the Armature '{armature.name}' is not close to 1/1/1 scale.\n\nCurrent Armature scale: X {armature.localScale.x:F4}, Y {armature.localScale.y:F4}, Z {armature.localScale.z:F4}\nUnit tolerance: +/- {UNIT_SCALE_TOLERANCE:F2}\n\nThis usually means it was edited before. If you continue, the scale will be multiplied again and previous edits may stack.\n\nRequested conversion: {profile.GetDisplayName()}\n\nContinue anyway?",
                        $"確認です: Armature '{armature.name}' は1/1/1スケール付近ではありません。\n\n現在のArmatureスケール: X {armature.localScale.x:F4}, Y {armature.localScale.y:F4}, Z {armature.localScale.z:F4}\n判定許容値: +/- {UNIT_SCALE_TOLERANCE:F2}\n\nすでに調整済みの可能性があります。このまま続行するとさらに乗算され、以前の調整が重なる場合があります。\n\n要求された変換: {profile.GetDisplayName()}\n\nこのまま続行しますか？"
                    );

                bool continueFromScaleWarning = EditorUtility.DisplayDialog(
                    L("IKUSIA Scaler - Scale Check", "IKUSIA Scaler - スケール確認"),
                    scaleWarningMessage,
                    L("Continue Anyway", "このまま続行"),
                    L("Go Back", "戻る")
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

        private static bool IsAutomaticConversionEnabled()
        {
            return EditorPrefs.GetBool(AUTO_CONVERT_ENABLED_KEY, false);
        }

        private static void SetCurrentLanguage(UILanguage language)
        {
            EditorPrefs.SetString(LANGUAGE_PREF_KEY, language.ToString());
            EditorPrefs.SetBool(LANGUAGE_PROMPT_DONE_KEY, true);
        }

        private static void SetAutomaticConversionEnabled(bool enabled)
        {
            EditorPrefs.SetBool(AUTO_CONVERT_ENABLED_KEY, enabled);
        }

        private static bool IsAutoDetectionTraceLoggingEnabled()
        {
            return EditorPrefs.GetBool(AUTO_DETECTION_TRACE_LOGGING_ENABLED_KEY, false);
        }

        private static void SetAutoDetectionTraceLoggingEnabled(bool enabled)
        {
            EditorPrefs.SetBool(AUTO_DETECTION_TRACE_LOGGING_ENABLED_KEY, enabled);
        }

        private static void ResetAllUserSettingsAndRuntimeState()
        {
            EditorPrefs.DeleteKey(LANGUAGE_PREF_KEY);
            EditorPrefs.DeleteKey(LANGUAGE_PROMPT_DONE_KEY);
            EditorPrefs.DeleteKey(AUTO_CONVERT_ENABLED_KEY);
            EditorPrefs.DeleteKey(AUTO_CONVERT_DISCOVERY_PROMPT_DONE_KEY);
            EditorPrefs.DeleteKey(AUTO_DETECTION_TRACE_LOGGING_ENABLED_KEY);

            processedPrefabRootIds.Clear();
            pendingPrefabRootIds.Clear();
            autoConvertedArmatureIds.Clear();
            BuildHierarchySnapshot();
        }

        private static string L(string english, string japanese)
        {
            return GetCurrentLanguage() == UILanguage.Japanese ? japanese : english;
        }

        private class IKUSIAScalerSettingsWindow : EditorWindow
        {
            private UILanguage selectedLanguage;
            private bool automaticConversionEnabled;
            private bool autoDetectionTraceLoggingEnabled;
            private bool markPromptDoneOnClose;
            private bool applied;
            private GUIStyle pageTitleStyle;
            private GUIStyle pageSubtitleStyle;
            private GUIStyle sectionStyle;
            private GUIStyle sectionTitleStyle;
            private GUIStyle bodyTextStyle;
            private GUIStyle resetButtonStyle;

            public static void ShowWindow(bool isFirstRun)
            {
                IKUSIAScalerSettingsWindow window = GetWindow<IKUSIAScalerSettingsWindow>(true, L("IKUSIA Scaler Settings", "IKUSIA Scaler 設定"));
                window.minSize = new Vector2(460f, 300f);
                window.maxSize = new Vector2(760f, 520f);
                window.markPromptDoneOnClose = isFirstRun;
                window.applied = false;
                window.selectedLanguage = GetCurrentLanguage();
                window.Show();
                window.Focus();
            }

            private void OnEnable()
            {
                selectedLanguage = GetCurrentLanguage();
                automaticConversionEnabled = IsAutomaticConversionEnabled();
                autoDetectionTraceLoggingEnabled = IsAutoDetectionTraceLoggingEnabled();
            }

            private void OnDisable()
            {
                if (markPromptDoneOnClose && !applied)
                {
                    EditorPrefs.SetBool(LANGUAGE_PROMPT_DONE_KEY, true);
                }
            }

            private void OnGUI()
            {
                EnsureStyles();

                EditorGUILayout.Space(8f);
                DrawHeader();
                EditorGUILayout.Space(6f);

                if (markPromptDoneOnClose)
                {
                    DrawOnboardingIntro();
                }

                DrawLanguageSetting();

                if (!markPromptDoneOnClose)
                {
                    DrawGeneralSettings();
                }

                GUILayout.FlexibleSpace();
                DrawFooterButtons();
            }

            private void EnsureStyles()
            {
                if (pageTitleStyle == null)
                {
                    pageTitleStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 17,
                        alignment = TextAnchor.MiddleLeft,
                        richText = true
                    };
                }

                if (pageSubtitleStyle == null)
                {
                    pageSubtitleStyle = new GUIStyle(EditorStyles.label)
                    {
                        wordWrap = true,
                        richText = true
                    };
                }

                if (sectionStyle == null)
                {
                    sectionStyle = new GUIStyle("box")
                    {
                        padding = new RectOffset(12, 12, 10, 10),
                        margin = new RectOffset(6, 6, 4, 4)
                    };
                }

                if (sectionTitleStyle == null)
                {
                    sectionTitleStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 12
                    };
                }

                if (bodyTextStyle == null)
                {
                    bodyTextStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
                    {
                        richText = true
                    };
                }

                if (resetButtonStyle == null)
                {
                    resetButtonStyle = new GUIStyle(GUI.skin.button)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontStyle = FontStyle.Bold
                    };
                }
            }

            private void DrawHeader()
            {
                EditorGUILayout.BeginVertical(sectionStyle);
                EditorGUILayout.LabelField(L("IKUSIA Scaler Settings", "IKUSIA Scaler 設定"), pageTitleStyle);
                EditorGUILayout.Space(2f);
                EditorGUILayout.LabelField(
                    L("Customize how IKUSIA Scaler behaves in your editor.", "IKUSIA Scaler の動作をお好みに合わせて設定できます。"),
                    pageSubtitleStyle
                );
                EditorGUILayout.EndVertical();
            }

            private void DrawOnboardingIntro()
            {
                EditorGUILayout.BeginVertical(sectionStyle);
                EditorGUILayout.LabelField(L("First-Time Setup", "初回セットアップ"), sectionTitleStyle);
                EditorGUILayout.Space(2f);
                EditorGUILayout.LabelField(
                    L("Welcome! Pick the language you’d like to use.", "ようこそ。使用する言語を選んでください。"),
                    bodyTextStyle
                );
                EditorGUILayout.EndVertical();
            }

            private void DrawLanguageSetting()
            {
                EditorGUILayout.BeginVertical(sectionStyle);
                EditorGUILayout.LabelField(L("Language", "言語"), sectionTitleStyle);
                EditorGUILayout.Space(2f);

                EditorGUI.BeginChangeCheck();
                {
                    string[] languageOptions = { "English", "日本語" };
                    int currentIndex = selectedLanguage == UILanguage.Japanese ? 1 : 0;
                    int selectedIndex = EditorGUILayout.Popup(L("Display Language", "表示言語"), currentIndex, languageOptions);
                    selectedLanguage = selectedIndex == 1 ? UILanguage.Japanese : UILanguage.English;
                    if (EditorGUI.EndChangeCheck())
                    {
                        Repaint();
                    }
                }

                EditorGUILayout.EndVertical();
            }

            // Keep settings drawing isolated so new options can be added cleanly.
            private void DrawGeneralSettings()
            {
                EditorGUILayout.Space();
                EditorGUILayout.BeginVertical(sectionStyle);
                EditorGUILayout.LabelField(L("General", "一般"), sectionTitleStyle);
                EditorGUILayout.Space(2f);

                automaticConversionEnabled = EditorGUILayout.Toggle(
                    L("Automatic Conversion", "自動変換"),
                    automaticConversionEnabled
                );

                EditorGUILayout.LabelField(
                    L(
                        "Automatically converts dropped outfits when avatar names match on both sides (avatar root + outfit prefab or folder).",
                        "アバター側（Avatar Root）と衣装側（Prefab名またはフォルダ名）の両方でアバター名が一致したときに、自動で変換を適用します。"
                    ),
                    bodyTextStyle
                );

                EditorGUILayout.Space();
                autoDetectionTraceLoggingEnabled = EditorGUILayout.Toggle(
                    L("Console Logs", "コンソールログ"),
                    autoDetectionTraceLoggingEnabled
                );

                EditorGUILayout.LabelField(
                    L(
                        "For Troubleshooting.",
                        "トラブルシュート用。"
                    ),
                    bodyTextStyle
                );

                EditorGUILayout.EndVertical();
            }

            private void DrawFooterButtons()
            {
                EditorGUILayout.Space(6f);
                EditorGUILayout.BeginVertical(sectionStyle);
                EditorGUILayout.BeginHorizontal();

                if (!markPromptDoneOnClose && GUILayout.Button(L("Reset All Settings", "すべての設定をリセット"), resetButtonStyle, GUILayout.Width(190f), GUILayout.Height(24f)))
                {
                    bool isJapanese = selectedLanguage == UILanguage.Japanese;
                    bool shouldReset = EditorUtility.DisplayDialog(
                        isJapanese ? "IKUSIA Scaler - リセット確認" : "IKUSIA Scaler - Confirm Reset",
                        isJapanese
                            ? "IKUSIA Scalerのユーザー設定をすべて初期化します。\n\n対象:\n- 言語設定\n- 初回オンボーディング状態\n- 自動変換設定\n- 自動変換の初回案内表示状態\n- 自動検出ログ設定\n\nリセット後は初回オンボーディングが再表示されます。実行しますか？"
                            : "You’re about to reset all IKUSIA Scaler user settings.\n\nThis includes:\n- Language setting\n- First-time onboarding state\n- Automatic conversion setting\n- Automatic conversion discovery prompt state\n- Auto-detection logging setting\n\nAfter reset, first-time onboarding will appear again. Continue?",
                        isJapanese ? "リセットする" : "Yes, Reset",
                        isJapanese ? "やめる" : "Not Now"
                    );

                    if (shouldReset)
                    {
                        ResetAllUserSettingsAndRuntimeState();
                        selectedLanguage = GetCurrentLanguage();
                        automaticConversionEnabled = IsAutomaticConversionEnabled();
                        applied = true;
                        markPromptDoneOnClose = false;
                        Close();
                        IKUSIAScalerSettingsWindow.ShowWindow(true);
                    }

                    return;
                }

                GUILayout.FlexibleSpace();

                if (GUILayout.Button(L("Cancel", "キャンセル"), GUILayout.Width(110f), GUILayout.Height(24f)))
                {
                    Close();
                    return;
                }

                if (GUILayout.Button(L("Apply", "適用"), GUILayout.Width(110f), GUILayout.Height(24f)))
                {
                    SetCurrentLanguage(selectedLanguage);
                    if (!markPromptDoneOnClose)
                    {
                        SetAutomaticConversionEnabled(automaticConversionEnabled);
                        SetAutoDetectionTraceLoggingEnabled(autoDetectionTraceLoggingEnabled);
                    }
                    applied = true;
                    Close();
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(4f);
            }
        }

        private static void OnHierarchyChanged()
        {
            AutoDetectLog("Hierarchy changed callback fired.");

            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            HashSet<int> currentIds = CollectSceneObjectIds();
            Dictionary<int, int> currentParentIds = CollectSceneObjectParentIds();
            List<int> addedIds = new List<int>();
            List<int> parentChangedIds = new List<int>();

            foreach (int id in currentIds)
            {
                if (!knownHierarchyObjectIds.Contains(id))
                {
                    addedIds.Add(id);
                }
                else
                {
                    int oldParentId;
                    int newParentId;
                    bool hadOldParent = knownHierarchyParentIds.TryGetValue(id, out oldParentId);
                    bool hasNewParent = currentParentIds.TryGetValue(id, out newParentId);
                    if (hadOldParent && hasNewParent && oldParentId != newParentId)
                    {
                        parentChangedIds.Add(id);
                    }
                }
            }

            knownHierarchyObjectIds.Clear();
            knownHierarchyObjectIds.UnionWith(currentIds);
            knownHierarchyParentIds.Clear();
            foreach (KeyValuePair<int, int> pair in currentParentIds)
            {
                knownHierarchyParentIds[pair.Key] = pair.Value;
            }

            if (addedIds.Count > 0)
            {
                AutoDetectLog($"Hierarchy changed. New objects detected: {addedIds.Count}");
            }

            if (parentChangedIds.Count > 0)
            {
                AutoDetectLog($"Hierarchy changed. Parent changes detected: {parentChangedIds.Count}");
            }

            HashSet<int> seenRoots = new HashSet<int>();
            foreach (int id in addedIds)
            {
                QueuePrefabRootForAutoDetection(id, seenRoots);
            }

            foreach (int id in parentChangedIds)
            {
                QueuePrefabRootForAutoDetection(id, seenRoots);
            }

            EvaluatePendingPrefabRoots();
        }

        private static void QueuePrefabRootForAutoDetection(int objectId, HashSet<int> seenRoots)
        {
            GameObject addedObject = EditorUtility.InstanceIDToObject(objectId) as GameObject;
            if (addedObject == null || !addedObject.scene.IsValid())
            {
                return;
            }

            GameObject prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(addedObject);
            if (prefabRoot == null)
            {
                return;
            }

            int prefabRootId = prefabRoot.GetInstanceID();
            if (!seenRoots.Add(prefabRootId))
            {
                return;
            }

            if (processedPrefabRootIds.Contains(prefabRootId))
            {
                return;
            }

            pendingPrefabRootIds.Add(prefabRootId);
            AutoDetectLog($"Queued prefab root for evaluation: '{prefabRoot.name}' (ID: {prefabRootId})");
        }

        private static void EvaluatePendingPrefabRoots()
        {
            if (pendingPrefabRootIds.Count == 0)
            {
                return;
            }

            AutoDetectLog($"Evaluating pending prefab roots: {pendingPrefabRootIds.Count}");

            List<int> resolvedIds = new List<int>();
            foreach (int prefabRootId in pendingPrefabRootIds)
            {
                GameObject prefabRoot = EditorUtility.InstanceIDToObject(prefabRootId) as GameObject;
                if (prefabRoot == null || !prefabRoot.scene.IsValid())
                {
                    AutoDetectLog($"Dropping pending entry for missing/invalid object ID: {prefabRootId}");
                    resolvedIds.Add(prefabRootId);
                    continue;
                }

                AutoDetectionResult result = EvaluateAutoConversionForDroppedPrefab(prefabRoot);
                AutoDetectLog($"Evaluation result for '{prefabRoot.name}': {result}");
                if (result == AutoDetectionResult.Detected ||
                    result == AutoDetectionResult.NoSourceMatch ||
                    result == AutoDetectionResult.SameAvatar ||
                    result == AutoDetectionResult.NoProfile)
                {
                    processedPrefabRootIds.Add(prefabRootId);
                    resolvedIds.Add(prefabRootId);
                }
            }

            foreach (int resolvedId in resolvedIds)
            {
                pendingPrefabRootIds.Remove(resolvedId);
            }
        }

        private static AutoDetectionResult EvaluateAutoConversionForDroppedPrefab(GameObject prefabRoot)
        {
            if (prefabRoot == null)
            {
                AutoDetectLog("Evaluate called with null prefab root.");
                return AutoDetectionResult.NoSourceMatch;
            }

            GameObject sourcePrefab = PrefabUtility.GetCorrespondingObjectFromSource(prefabRoot);
            string sourcePath = sourcePrefab != null ? AssetDatabase.GetAssetPath(sourcePrefab) : "(no source prefab path)";
            AutoDetectLog($"Evaluating dropped prefab '{prefabRoot.name}'. Source path: {sourcePath}");

            AvatarType outfitAvatarType = DetectAvatarTypeFromPrefab(prefabRoot);
            if (outfitAvatarType == AvatarType.Unknown)
            {
                AutoDetectLog($"No supported source avatar keyword found for '{prefabRoot.name}'.");
                return AutoDetectionResult.NoSourceMatch;
            }

            AutoDetectLog($"Detected outfit source avatar: {AvatarTypeToProfileName(outfitAvatarType)}");

            AvatarType targetAvatarType = DetectAvatarTypeFromContext(prefabRoot.transform);
            if (targetAvatarType == AvatarType.Unknown)
            {
                AutoDetectLog($"Target avatar not detected yet for '{prefabRoot.name}'. Waiting for next hierarchy update.");
                return AutoDetectionResult.NoTargetMatch;
            }

            AutoDetectLog($"Detected avatar root target: {AvatarTypeToProfileName(targetAvatarType)}");

            if (targetAvatarType == outfitAvatarType)
            {
                AutoDetectLog("Source and target avatars are the same. Skipping conversion.");
                return AutoDetectionResult.SameAvatar;
            }

            ScalingProfile profile = FindConversionProfile(outfitAvatarType, targetAvatarType);
            if (profile == null)
            {
                AutoDetectLog($"No conversion profile found for {AvatarTypeToProfileName(outfitAvatarType)} -> {AvatarTypeToProfileName(targetAvatarType)}.");
                return AutoDetectionResult.NoProfile;
            }

            AutoDetectLog($"Matched conversion profile: {profile.GetDisplayName()}");

            Transform outfitArmature = FindArmature(prefabRoot.transform);
            if (outfitArmature != null)
            {
                int armatureId = outfitArmature.gameObject.GetInstanceID();
                if (autoConvertedArmatureIds.Contains(armatureId))
                {
                    AutoDetectLog($"Skipping duplicate auto conversion for already processed armature '{outfitArmature.name}' (ID: {armatureId}).");
                    return AutoDetectionResult.Detected;
                }
            }

            TryShowAutoConversionDiscoveryPrompt(profile, prefabRoot.name);

            if (!IsAutomaticConversionEnabled())
            {
                Debug.Log($"[IKUSIA Scaler] Good news: detected '{profile.GetDisplayName()}' for '{prefabRoot.name}'. Turn on Automatic Conversion in Window > IKUSIA Scaler Settings to apply this automatically next time.");
                AutoDetectLog("Automatic conversion is currently disabled in settings.");
                return AutoDetectionResult.Detected;
            }

            bool applied = ApplyConversion(profile, prefabRoot, false);
            if (applied)
            {
                if (outfitArmature != null)
                {
                    autoConvertedArmatureIds.Add(outfitArmature.gameObject.GetInstanceID());
                }

                Debug.Log($"[IKUSIA Scaler] Auto-applied conversion '{profile.GetDisplayName()}' to dropped prefab '{prefabRoot.name}'.");
                AutoDetectLog("Auto conversion applied successfully.");
            }
            else
            {
                AutoDetectLog("Auto conversion attempted but was not applied (preflight guard or missing armature). Check warnings above.");
            }

            return AutoDetectionResult.Detected;
        }

        private static void TryShowAutoConversionDiscoveryPrompt(ScalingProfile profile, string outfitName)
        {
            if (EditorPrefs.GetBool(AUTO_CONVERT_DISCOVERY_PROMPT_DONE_KEY, false))
            {
                return;
            }

            string message = L(
                $"Nice, I found a matching avatar conversion setup.\n\nOutfit: {outfitName}\nSuggested profile: {profile.GetDisplayName()}\n\nWould you like to enable Automatic Conversion so matching outfits are converted for you in the future?",
                $"対応する変換の組み合わせを見つけました。\n\n衣装: {outfitName}\n推奨プロファイル: {profile.GetDisplayName()}\n\n今後、条件が一致した衣装を自動で変換するために「自動変換」を有効にしますか？"
            );

            bool enableNow = EditorUtility.DisplayDialog(
                L("IKUSIA Scaler - Automatic Conversion", "IKUSIA Scaler - 自動変換"),
                message,
                L("Yes, Enable It", "有効にする"),
                L("Maybe Later", "あとで")
            );

            if (enableNow)
            {
                SetAutomaticConversionEnabled(true);
            }

            EditorPrefs.SetBool(AUTO_CONVERT_DISCOVERY_PROMPT_DONE_KEY, true);
        }

        private static void BuildHierarchySnapshot()
        {
            knownHierarchyObjectIds.Clear();
            knownHierarchyObjectIds.UnionWith(CollectSceneObjectIds());

            knownHierarchyParentIds.Clear();
            Dictionary<int, int> parentIds = CollectSceneObjectParentIds();
            foreach (KeyValuePair<int, int> pair in parentIds)
            {
                knownHierarchyParentIds[pair.Key] = pair.Value;
            }
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            BuildHierarchySnapshot();
            processedPrefabRootIds.Clear();
            pendingPrefabRootIds.Clear();
            autoConvertedArmatureIds.Clear();
        }

        private static HashSet<int> CollectSceneObjectIds()
        {
            HashSet<int> ids = new HashSet<int>();

            int sceneCount = SceneManager.sceneCount;
            for (int i = 0; i < sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    continue;
                }

                GameObject[] roots = scene.GetRootGameObjects();
                foreach (GameObject root in roots)
                {
                    AddGameObjectHierarchyIds(root.transform, ids);
                }
            }

            return ids;
        }

        private static Dictionary<int, int> CollectSceneObjectParentIds()
        {
            Dictionary<int, int> parentIds = new Dictionary<int, int>();

            int sceneCount = SceneManager.sceneCount;
            for (int i = 0; i < sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    continue;
                }

                GameObject[] roots = scene.GetRootGameObjects();
                foreach (GameObject root in roots)
                {
                    AddGameObjectHierarchyParentIds(root.transform, parentIds);
                }
            }

            return parentIds;
        }

        private static void AddGameObjectHierarchyIds(Transform root, HashSet<int> ids)
        {
            if (root == null)
            {
                return;
            }

            ids.Add(root.gameObject.GetInstanceID());
            foreach (Transform child in root)
            {
                AddGameObjectHierarchyIds(child, ids);
            }
        }

        private static void AddGameObjectHierarchyParentIds(Transform root, Dictionary<int, int> parentIds)
        {
            if (root == null)
            {
                return;
            }

            int objectId = root.gameObject.GetInstanceID();
            int parentId = root.parent != null ? root.parent.gameObject.GetInstanceID() : 0;
            parentIds[objectId] = parentId;

            foreach (Transform child in root)
            {
                AddGameObjectHierarchyParentIds(child, parentIds);
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

        private static void AutoDetectLog(string message)
        {
            if (IsAutoDetectionTraceLoggingEnabled())
            {
                Debug.Log($"[IKUSIA Scaler][AutoDetect] {message}");
            }
        }
    }
}
