// Copyright (c) 4thWAIV. All rights reserved.

using System.IO;
using System.Text.Json;

namespace AgentGuard.Setup;

/// <summary>
/// Reads the machine state record once, classifying it as present, missing (not installed), or unreadable, so
/// every machine condition shares one reading and a missing record is never confused with an unreadable one.
/// </summary>
internal static class MachineInspection
{
    /// <summary>
    /// Reads and classifies the machine state record.
    /// </summary>
    /// <param name="context">The setup context.</param>
    /// <returns>The machine facts.</returns>
    internal static MachineFacts Read(SetupContext context)
    {
        string path = MachinePaths.StateFile(context);
        if (!context.FileReader.Exists(path))
        {
            return new MachineFacts(
                MachineStateStatus.Missing, null, "the machine is not installed; run `guard install`");
        }

        string json;
        try
        {
            json = context.FileReader.ReadAllText(path);
        }
        catch (IOException exception)
        {
            return new MachineFacts(MachineStateStatus.Unreadable, null, $"state.json is unreadable: {exception.Message}");
        }
        catch (UnauthorizedAccessException exception)
        {
            return new MachineFacts(MachineStateStatus.Unreadable, null, $"state.json is unreadable: {exception.Message}");
        }

        InstallState? state;
        try
        {
            state = SetupJson.DeserializeInstallState(json);
        }
        catch (JsonException exception)
        {
            return new MachineFacts(MachineStateStatus.Unreadable, null, $"state.json is malformed: {exception.Message}");
        }

        if (state is null || string.IsNullOrEmpty(state.Version) || string.IsNullOrEmpty(state.Sha256))
        {
            return new MachineFacts(MachineStateStatus.Unreadable, null, "state.json is incomplete");
        }

        return new MachineFacts(MachineStateStatus.Present, state, "installed");
    }
}
