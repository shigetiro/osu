// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using osu.Framework.Logging;

namespace osu.Desktop
{
    /// <summary>
    /// AMD GPU detection and capability checking.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static class AMDAPI
    {
        public static bool Available { get; private set; }
        public static bool IsAMDGPU { get; private set; }

        static AMDAPI()
        {
            try
            {
                DetectAMDGPU();
                Available = IsAMDGPU;

                if (Available)
                {
                    Logger.Log("AMD GPU detected - Anti-Lag 2 support available.");
                }
                else
                {
                    Logger.Log("No AMD GPU detected - Anti-Lag 2 not available.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to detect AMD GPU");
                Available = false;
                IsAMDGPU = false;
            }
        }

        private static void DetectAMDGPU()
        {
            try
            {
                // Check for AMD GPU by looking for AMD driver DLLs
                // Similar to how NVAPI detects NVIDIA GPUs

                bool hasAMDGPU = false;

                // Check for AMD driver DLLs (similar to NVAPI approach)
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // Check for atiadlxx.dll (AMD Display Library) - 64-bit
                    IntPtr amdDll = loadLibrary("atiadlxx.dll");
                    if (amdDll != IntPtr.Zero)
                    {
                        hasAMDGPU = true;
                        freeLibrary(amdDll);
                    }
                    else
                    {
                        // Check for atiadlxy.dll (AMD Display Library) - 32-bit
                        amdDll = loadLibrary("atiadlxy.dll");
                        if (amdDll != IntPtr.Zero)
                        {
                            hasAMDGPU = true;
                            freeLibrary(amdDll);
                        }
                    }
                }

                IsAMDGPU = hasAMDGPU;

                if (hasAMDGPU)
                {
                    Logger.Log("AMD GPU detected via driver DLL presence.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "AMD GPU detection failed");
                IsAMDGPU = false;
            }
        }

        /// <summary>
        /// Check if the system has AMD Anti-Lag 2 support.
        /// Requires AMD RDNA 1-based products (RX 5000 Series and newer) and appropriate drivers.
        /// </summary>
        public static bool HasAntiLag2Support
        {
            get
            {
                if (!Available || !IsAMDGPU)  // Add !IsAMDGPU check
                    return false;

                try
                {
                    // Check if the AMD Anti-Lag 2 DLL is available
                    // This is the actual requirement for Anti-Lag 2 support
                    IntPtr antiLagDll = loadLibrary("amd_antilag_dx11.dll");
                    if (antiLagDll != IntPtr.Zero)
                    {
                        freeLibrary(antiLagDll);
                        return true;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Get the name of the AMD GPU if available.
        /// </summary>
        public static string GPUName
        {
            get
            {
                if (!Available)
                    return "Not available";

                return "AMD GPU (RDNA Series)";
            }
        }

        [DllImport("kernel32.dll", EntryPoint = "LoadLibrary")]
        private static extern IntPtr loadLibrary(string dllToLoad);

        [DllImport("kernel32.dll", EntryPoint = "FreeLibrary")]
        private static extern bool freeLibrary(IntPtr hModule);
    }
}
