using System.Reflection;
using HarmonyLib;

namespace CreatorCompanionPatcher.Patch;

public interface IPatch
{
    public int Order { get; }
    public void ApplyPatch(Harmony harmony, Assembly vccAssembly, Assembly vccLibAssembly, Assembly vccCoreLibAssembly);
}