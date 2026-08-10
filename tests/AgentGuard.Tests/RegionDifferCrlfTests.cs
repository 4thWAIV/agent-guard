// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Text;
using AgentGuard.Engine;
using FluentAssertions;
using Xunit;

namespace AgentGuard.Tests;

/// <summary>
/// Proves the config-protection line-ending normalization in <see cref="RegionDiffer.RegionMatches"/>
/// (config-protection-crlf-fix): a pure CRLF/LF difference between two files' region values is NOT read as drift,
/// while genuinely different content still is. OS-agnostic — it feeds bytes directly, never a real file, so it
/// asserts the same result on every OS. It also confirms the normalization is compare-only (scan-resolutions #6):
/// the input buffers are never mutated, so a repair would preserve the file's existing line endings.
/// </summary>
public sealed class RegionDifferCrlfTests
{
    private static readonly RegionLocator AnyLocator = new("whole-file", string.Empty);

    [Fact]
    public void RegionMatches_SameContentCrlfVersusLf_Matches()
    {
        byte[] crlf = Encoding.UTF8.GetBytes("alpha\r\nbeta\r\ngamma");
        byte[] lf = Encoding.UTF8.GetBytes("alpha\nbeta\ngamma");

        bool matches = RegionDiffer.RegionMatches(new WholeFileTextAdapter(), AnyLocator, crlf, lf);

        matches.Should().BeTrue("a pure CRLF/LF difference must not be read as drift");
    }

    [Fact]
    public void RegionMatches_DifferentContentSameLineEndings_DoesNotMatch()
    {
        byte[] left = Encoding.UTF8.GetBytes("alpha\nbeta\ngamma");
        byte[] right = Encoding.UTF8.GetBytes("alpha\nbeta\nDIFFERENT");

        bool matches = RegionDiffer.RegionMatches(new WholeFileTextAdapter(), AnyLocator, left, right);

        matches.Should().BeFalse("genuinely different content is real drift");
    }

    [Fact]
    public void RegionMatches_IsCompareOnly_DoesNotRewriteTheInputBytes()
    {
        byte[] crlf = Encoding.UTF8.GetBytes("alpha\r\nbeta");
        byte[] lf = Encoding.UTF8.GetBytes("alpha\nbeta");
        byte[] crlfBefore = (byte[])crlf.Clone();
        byte[] lfBefore = (byte[])lf.Clone();

        RegionDiffer.RegionMatches(new WholeFileTextAdapter(), AnyLocator, crlf, lf);

        // The compare normalizes only in memory; it never rewrites the bytes it was handed, so a repair over these
        // bytes would keep the file's existing CRLF/LF exactly as-is.
        crlf.Should().Equal(crlfBefore);
        lf.Should().Equal(lfBefore);
    }

    /// <summary>
    /// A minimal <see cref="IRegionAdapter"/> whose region value is the whole file decoded as UTF-8 text, so the
    /// test drives <see cref="RegionDiffer.RegionMatches"/> directly with content it controls, independent of any
    /// real format adapter.
    /// </summary>
    private sealed class WholeFileTextAdapter : IRegionAdapter
    {
        public string Format => "whole-file-text";

        public string? Read(ReadOnlyMemory<byte> file, RegionLocator locator) => Encoding.UTF8.GetString(file.Span);

        public ReadOnlyMemory<byte>? Rewrite(ReadOnlyMemory<byte> file, RegionLocator locator, string value) =>
            Encoding.UTF8.GetBytes(value);
    }
}
