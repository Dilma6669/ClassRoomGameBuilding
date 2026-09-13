#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;

[InitializeOnLoad]
public static class DependencyChecker
{
    private const string KINEMATIC_CC_SYMBOL = "HAS_KINEMATIC_CC";

    static DependencyChecker()
    {
        CheckDependencies();
    }

    public static void CheckDependencies()
    {
        // Check if KinematicCharacterMotor exists in any loaded assembly
        bool hasKinematicCC = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => {
                try { return a.GetTypes(); }
                catch { return new Type[0]; }
            })
            .Any(t => t.Name == "KinematicCharacterMotor");

        BuildTargetGroup buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
        
        #if UNITY_2023_1_OR_NEWER
        NamedBuildTarget namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
        string currentSymbols = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);
        #else
        string currentSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
        #endif

        var defines = currentSymbols.Split(';').Select(s => s.Trim()).ToList();

        if (hasKinematicCC && !defines.Contains(KINEMATIC_CC_SYMBOL))
        {
            defines.Add(KINEMATIC_CC_SYMBOL);
            UpdateDefines(buildTargetGroup, defines);
        }
        else if (!hasKinematicCC && defines.Contains(KINEMATIC_CC_SYMBOL))
        {
            defines.Remove(KINEMATIC_CC_SYMBOL);
            UpdateDefines(buildTargetGroup, defines);
        }
    }

    private static void UpdateDefines(BuildTargetGroup group, System.Collections.Generic.List<string> defines)
    {
        string newSymbols = string.Join(";", defines.ToArray());
        #if UNITY_2023_1_OR_NEWER
        NamedBuildTarget namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(group);
        PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, newSymbols);
        #else
        PlayerSettings.SetScriptingDefineSymbolsForGroup(group, newSymbols);
        #endif
    }
}
#endif