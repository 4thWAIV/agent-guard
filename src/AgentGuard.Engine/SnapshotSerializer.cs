// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AgentGuard.Engine;

/// <summary>
/// The versioned, length-framed format for a pre-image snapshot. A truncated or unparseable blob is reported as
/// unreadable, which the caller treats as a missing snapshot and fails closed. Read never trusts a length past
/// the end of the buffer, so a corrupt blob cannot drive an over-large allocation.
/// </summary>
internal static class SnapshotSerializer
{
    private const int Magic = 0x41_47_53_31;
    private const int Version = 1;

    /// <summary>
    /// Serializes a snapshot to its on-disk bytes.
    /// </summary>
    /// <param name="data">The snapshot to serialize.</param>
    /// <returns>The serialized bytes.</returns>
    internal static byte[] Serialize(SnapshotData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(Magic);
            writer.Write(Version);
            writer.Write(data.RulesetFingerprint);
            writer.Write(data.Entries.Count);
            foreach (SnapshotEntry entry in data.Entries)
            {
                writer.Write(entry.Path);
                writer.Write(entry.Present);
                if (entry.Present)
                {
                    writer.Write(entry.Content.Length);
                    writer.Write(entry.Content.Span);
                }
            }
        }

        return stream.ToArray();
    }

    /// <summary>
    /// Attempts to deserialize a snapshot from bytes. A truncated or malformed blob returns
    /// <see langword="false"/>, which the caller treats as a missing snapshot.
    /// </summary>
    /// <param name="bytes">The bytes to parse.</param>
    /// <param name="data">The parsed snapshot when this returns <see langword="true"/>; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the blob parsed cleanly.</returns>
    internal static bool TryDeserialize(ReadOnlyMemory<byte> bytes, out SnapshotData? data)
    {
        data = null;
        try
        {
            using var stream = new MemoryStream(bytes.ToArray(), writable: false);
            using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
            if (reader.ReadInt32() != Magic || reader.ReadInt32() != Version)
            {
                return false;
            }

            string fingerprint = reader.ReadString();
            int count = reader.ReadInt32();
            if (count < 0)
            {
                return false;
            }

            var entries = new List<SnapshotEntry>(count);
            for (int index = 0; index < count; index++)
            {
                string path = reader.ReadString();
                bool present = reader.ReadBoolean();
                ReadOnlyMemory<byte> content = ReadOnlyMemory<byte>.Empty;
                if (present)
                {
                    int length = reader.ReadInt32();
                    long remaining = stream.Length - stream.Position;
                    if (length < 0 || length > remaining)
                    {
                        return false;
                    }

                    content = reader.ReadBytes(length);
                }

                entries.Add(new SnapshotEntry(path, present, content));
            }

            data = new SnapshotData(fingerprint, entries);
            return true;
        }
        catch (EndOfStreamException)
        {
            data = null;
            return false;
        }
        catch (IOException)
        {
            data = null;
            return false;
        }
        catch (FormatException)
        {
            data = null;
            return false;
        }
    }
}
