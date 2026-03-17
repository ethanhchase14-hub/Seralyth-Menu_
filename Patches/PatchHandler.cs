/*
 * Seralyth Menu  Patches/PatchHandler.cs
 * A community driven mod menu for Gorilla Tag with over 1000+ mods
 *
 * Copyright (C) 2026  Seralyth Software
 * https://github.com/Seralyth/Seralyth-Menu
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using HarmonyLib;
using Seralyth.Managers;
using System;
using System.Linq;
using System.Reflection;

namespace Seralyth.Patches
{
    public class PatchHandler
    {
        public static bool IsPatched { get; internal set; }
        public static int PatchErrors { get; internal set; }

        public static bool CriticalPatchFailed { get; internal set; }

        [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
        public class SecurityPatch : Attribute { }

        [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
        public class PatchOnAwake : Attribute { }

        public static void PatchAll(bool awake = false)
        {
            if (IsPatched) return;
            instance ??= new HarmonyLib.Harmony(PluginInfo.GUID);

            Type[] types; // Allows for cross mod loader support
            try
            {
                types = Assembly.GetExecutingAssembly().GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                types = e.Types.Where(t => t != null).ToArray();
            }

            foreach (var type in types
                         .Where(t => t.IsClass && t.GetCustomAttribute<HarmonyPatch>() != null && t.GetCustomAttribute<PatchOnAwake>() != null == awake))
            {
                try
                {
                    instance.CreateClassProcessor(type).Patch();
                }
                catch (Exception ex)
                {
                    PatchErrors++;
                    if (type.GetCustomAttribute<SecurityPatch>() != null)
                        CriticalPatchFailed = true;
                    CriticalPatchFailed = true;
                    LogManager.LogError($"Failed to patch {type.FullName}: {ex}");
                }
            }

            LogManager.Log($"Patched with {PatchErrors} errors");

            IsPatched = !awake;
        }

        public static void UnpatchAll()
        {
            if (instance == null || !IsPatched) return;
            instance.UnpatchSelf();
            IsPatched = false;
            instance = null;
        }

        public static void ApplyPatch(Type targetClass, string methodName, MethodInfo prefix = null, MethodInfo postfix = null, Type[] parameterTypes = null)
        {
            var original =
                (parameterTypes == null ?
                targetClass.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static) :
                targetClass.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static, null, parameterTypes, null)) ?? throw new Exception($"Method '{methodName}' not found on {targetClass.FullName}");
            instance.Patch(original,
                prefix: prefix != null ? new HarmonyMethod(prefix) : null,
                postfix: postfix != null ? new HarmonyMethod(postfix) : null);
        }

        public static void RemovePatch(Type targetClass, string methodName, Type[] parameterTypes = null)
        {
            var original =
                (parameterTypes == null ?
                targetClass.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static) :
                targetClass.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static, null, parameterTypes, null)) ?? throw new Exception($"Method '{methodName}' not found on {targetClass.FullName}");
            instance.Unpatch(original, HarmonyPatchType.All, instance.Id);
        }

        private static HarmonyLib.Harmony instance;
        public const string InstanceId = PluginInfo.GUID;
    }
}