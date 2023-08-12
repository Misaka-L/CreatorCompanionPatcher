using System.Reflection;
using HarmonyLib;

namespace CreatorCompanionPatcher.Patch;

public interface IPatch
{
    public void ApplyPatch(Harmony harmony, Assembly vccAssembly, Assembly vccLibAssembly, Assembly vccCoreLibAssembly);
}