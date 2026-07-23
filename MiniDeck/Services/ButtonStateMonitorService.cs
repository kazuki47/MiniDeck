using MiniDeck.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace MiniDeck.Services
{
    public sealed class ButtonStateMonitorService
    {
        public void Refresh(IEnumerable<ActionButton> buttons)
        {
            List<ActionButton> targets = (buttons ?? Enumerable.Empty<ActionButton>())
                .Where(button => button != null && button.StateDisplayType != ButtonStateDisplayType.None)
                .ToList();
            if (targets.Count == 0)
            {
                return;
            }

            bool microphoneAvailable = true;
            bool microphoneMuted = false;
            string microphoneError = "";
            if (targets.Any(button => button.StateDisplayType == ButtonStateDisplayType.MicrophoneMuted))
            {
                microphoneAvailable = TryGetEndpointMute(
                    AudioDataFlow.Capture,
                    out microphoneMuted,
                    out microphoneError);
            }

            bool systemAudioAvailable = true;
            bool systemAudioMuted = false;
            string systemAudioError = "";
            if (targets.Any(button => button.StateDisplayType == ButtonStateDisplayType.SystemAudioMuted))
            {
                systemAudioAvailable = TryGetEndpointMute(
                    AudioDataFlow.Render,
                    out systemAudioMuted,
                    out systemAudioError);
            }

            foreach (ActionButton button in targets)
            {
                try
                {
                    switch (button.StateDisplayType)
                    {
                        case ButtonStateDisplayType.ApplicationRunning:
                            if (TryGetApplicationRunning(button.ApplicationPath, out bool running, out string appError))
                            {
                                button.UpdateRuntimeState(
                                    running ? ButtonRuntimeState.Active : ButtonRuntimeState.Inactive);
                            }
                            else
                            {
                                button.UpdateRuntimeState(ButtonRuntimeState.Unknown, appError);
                            }
                            break;

                        case ButtonStateDisplayType.MicrophoneMuted:
                            button.UpdateRuntimeState(
                                microphoneAvailable
                                    ? (microphoneMuted ? ButtonRuntimeState.Active : ButtonRuntimeState.Inactive)
                                    : ButtonRuntimeState.Unknown,
                                microphoneAvailable ? "" : microphoneError);
                            break;

                        case ButtonStateDisplayType.SystemAudioMuted:
                            button.UpdateRuntimeState(
                                systemAudioAvailable
                                    ? (systemAudioMuted ? ButtonRuntimeState.Active : ButtonRuntimeState.Inactive)
                                    : ButtonRuntimeState.Unknown,
                                systemAudioAvailable ? "" : systemAudioError);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    button.UpdateRuntimeState(ButtonRuntimeState.Unknown, ex.Message);
                }
            }
        }

        public static bool TryGetApplicationRunning(
            string applicationPath,
            out bool isRunning,
            out string errorMessage)
        {
            isRunning = false;
            errorMessage = "";
            if (string.IsNullOrWhiteSpace(applicationPath))
            {
                errorMessage = "対象アプリのパスが設定されていません";
                return false;
            }

            string targetPath = Environment.ExpandEnvironmentVariables(applicationPath.Trim().Trim('"'));
            if (string.Equals(Path.GetExtension(targetPath), ".lnk", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryResolveShortcutTarget(targetPath, out targetPath, out errorMessage))
                {
                    return false;
                }
            }

            if (!string.Equals(Path.GetExtension(targetPath), ".exe", StringComparison.OrdinalIgnoreCase))
            {
                errorMessage = "アプリ状態の監視対象は.exeまたは.exeへのショートカットにしてください";
                return false;
            }

            string processName = Path.GetFileNameWithoutExtension(targetPath);
            if (string.IsNullOrWhiteSpace(processName))
            {
                errorMessage = "対象アプリ名を判定できません";
                return false;
            }

            bool compareFullPath = Path.IsPathRooted(targetPath);
            if (compareFullPath && !File.Exists(targetPath))
            {
                errorMessage = "監視対象の実行ファイルが見つかりません";
                return false;
            }
            string normalizedTargetPath = compareFullPath ? NormalizePath(targetPath) : "";
            Process[] processes;
            try
            {
                processes = Process.GetProcessesByName(processName);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }

            try
            {
                foreach (Process process in processes)
                {
                    if (!compareFullPath)
                    {
                        isRunning = true;
                        return true;
                    }

                    try
                    {
                        string processPath = NormalizePath(process.MainModule?.FileName);
                        if (string.Equals(processPath, normalizedTargetPath, StringComparison.OrdinalIgnoreCase))
                        {
                            isRunning = true;
                            return true;
                        }
                    }
                    catch
                    {
                        // 権限の異なるプロセスではパスを取得できないため、実行ファイル名の一致を採用する。
                        isRunning = true;
                        return true;
                    }
                }

                return true;
            }
            finally
            {
                foreach (Process process in processes)
                {
                    process.Dispose();
                }
            }
        }

        private static bool TryResolveShortcutTarget(
            string shortcutPath,
            out string targetPath,
            out string errorMessage)
        {
            targetPath = "";
            errorMessage = "";
            object shell = null;
            object shortcut = null;
            try
            {
                if (!File.Exists(shortcutPath))
                {
                    errorMessage = "ショートカットが見つかりません";
                    return false;
                }

                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null)
                {
                    errorMessage = "ショートカットを読み取れません";
                    return false;
                }

                shell = Activator.CreateInstance(shellType);
                dynamic dynamicShell = shell;
                shortcut = dynamicShell.CreateShortcut(shortcutPath);
                dynamic dynamicShortcut = shortcut;
                targetPath = dynamicShortcut.TargetPath as string ?? "";
                if (string.IsNullOrWhiteSpace(targetPath))
                {
                    errorMessage = "ショートカットのリンク先が空です";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
            finally
            {
                ReleaseComObject(shortcut);
                ReleaseComObject(shell);
            }
        }

        private static bool TryGetEndpointMute(
            AudioDataFlow dataFlow,
            out bool isMuted,
            out string errorMessage)
        {
            isMuted = false;
            errorMessage = "";
            object enumeratorObject = null;
            IMMDevice device = null;
            object endpointObject = null;
            try
            {
                enumeratorObject = new MMDeviceEnumeratorComObject();
                var enumerator = (IMMDeviceEnumerator)enumeratorObject;
                int result = enumerator.GetDefaultAudioEndpoint(dataFlow, AudioRole.Multimedia, out device);
                Marshal.ThrowExceptionForHR(result);

                Guid endpointVolumeId = typeof(IAudioEndpointVolume).GUID;
                result = device.Activate(
                    ref endpointVolumeId,
                    ClassContext.All,
                    IntPtr.Zero,
                    out endpointObject);
                Marshal.ThrowExceptionForHR(result);

                var endpoint = (IAudioEndpointVolume)endpointObject;
                result = endpoint.GetMute(out isMuted);
                Marshal.ThrowExceptionForHR(result);
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
            finally
            {
                ReleaseComObject(endpointObject);
                ReleaseComObject(device);
                ReleaseComObject(enumeratorObject);
            }
        }

        private static string NormalizePath(string path)
        {
            return string.IsNullOrWhiteSpace(path) ? "" : Path.GetFullPath(path).TrimEnd('\\', '/');
        }

        private static void ReleaseComObject(object value)
        {
            if (value != null && Marshal.IsComObject(value))
            {
                try
                {
                    Marshal.FinalReleaseComObject(value);
                }
                catch
                {
                    // 終了処理中のCOM解放失敗はアプリの動作へ影響させない。
                }
            }
        }

        private enum AudioDataFlow
        {
            Render,
            Capture,
            All
        }

        private enum AudioRole
        {
            Console,
            Multimedia,
            Communications
        }

        [Flags]
        private enum ClassContext : uint
        {
            InProcessServer = 0x1,
            InProcessHandler = 0x2,
            LocalServer = 0x4,
            RemoteServer = 0x10,
            All = InProcessServer | InProcessHandler | LocalServer | RemoteServer
        }

        [ComImport]
        [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
        private class MMDeviceEnumeratorComObject
        {
        }

        [ComImport]
        [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDeviceEnumerator
        {
            [PreserveSig]
            int EnumAudioEndpoints(AudioDataFlow dataFlow, uint stateMask, out IntPtr devices);

            [PreserveSig]
            int GetDefaultAudioEndpoint(AudioDataFlow dataFlow, AudioRole role, out IMMDevice device);

            [PreserveSig]
            int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string id, out IMMDevice device);

            [PreserveSig]
            int RegisterEndpointNotificationCallback(IntPtr client);

            [PreserveSig]
            int UnregisterEndpointNotificationCallback(IntPtr client);
        }

        [ComImport]
        [Guid("D666063F-1587-4E43-81F1-B948E807363F")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDevice
        {
            [PreserveSig]
            int Activate(
                ref Guid interfaceId,
                ClassContext classContext,
                IntPtr activationParameters,
                [MarshalAs(UnmanagedType.IUnknown)] out object interfacePointer);
        }

        [ComImport]
        [Guid("5CDF2C82-841E-4546-9722-0CF74078229A")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IAudioEndpointVolume
        {
            [PreserveSig] int RegisterControlChangeNotify(IntPtr notify);
            [PreserveSig] int UnregisterControlChangeNotify(IntPtr notify);
            [PreserveSig] int GetChannelCount(out uint channelCount);
            [PreserveSig] int SetMasterVolumeLevel(float level, ref Guid eventContext);
            [PreserveSig] int SetMasterVolumeLevelScalar(float level, ref Guid eventContext);
            [PreserveSig] int GetMasterVolumeLevel(out float level);
            [PreserveSig] int GetMasterVolumeLevelScalar(out float level);
            [PreserveSig] int SetChannelVolumeLevel(uint channel, float level, ref Guid eventContext);
            [PreserveSig] int SetChannelVolumeLevelScalar(uint channel, float level, ref Guid eventContext);
            [PreserveSig] int GetChannelVolumeLevel(uint channel, out float level);
            [PreserveSig] int GetChannelVolumeLevelScalar(uint channel, out float level);
            [PreserveSig] int SetMute([MarshalAs(UnmanagedType.Bool)] bool muted, ref Guid eventContext);
            [PreserveSig] int GetMute([MarshalAs(UnmanagedType.Bool)] out bool muted);
        }
    }
}
