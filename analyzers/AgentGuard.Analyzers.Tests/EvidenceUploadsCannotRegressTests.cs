// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

/// <summary>
/// Rule 5 of the cross-OS ruleset (a test, not a Roslyn analyzer): the CI evidence uploads cannot regress. Every
/// evidence-upload step (an <c>actions/upload-artifact</c> step whose artifact name starts with <c>evidence-</c>) must
/// run on all three OS jobs, use <c>if: always()</c>, never be push-gated, carry the complete evidence set (the
/// binlog, the <c>.trx</c> test results, and the coverage/<c>TestResults</c> report), and never glob <c>dist/</c> or
/// signing material. This stops the uploads reverting to push-only — which would blind the failing PR legs — or
/// leaking release binaries or signing secrets into a PR-visible artifact. It is RED until Strand 3 lands the evidence
/// uploads; the planted-regression checks prove it has teeth.
/// </summary>
public class EvidenceUploadsCannotRegressTests
{
    private static readonly IReadOnlyList<string> OsJobs = new[] { "macos", "windows", "linux" };

    // Tokens an evidence upload must never glob: the signed binaries staging directory and any signing material.
    private static readonly IReadOnlyList<string> ForbiddenGlobs = new[]
    {
        "dist/", ".snk", ".pfx", ".p12", ".cer", "cosign", "signed-", "verify/",
    };

    [Fact]
    public void EveryOsJob_HasAConformingEvidenceUpload()
    {
        List<Step> evidenceSteps = ParseSteps(ReadWorkflowLines()).Where(IsEvidenceUpload).ToList();

        foreach (string job in OsJobs)
        {
            List<Step> jobEvidence = evidenceSteps
                .Where(step => string.Equals(step.Job, job, StringComparison.Ordinal))
                .ToList();

            string missing =
                $"The '{job}' job must have an evidence-upload step (actions/upload-artifact named evidence-*); none "
                + "found. Evidence uploads must run on all three OS jobs (ci-uploads-build-evidence).";
            Assert.True(jobEvidence.Count > 0, missing);

            foreach (Step step in jobEvidence)
            {
                List<string> problems = ConformanceProblems(step);
                string regressed = $"The '{job}' evidence-upload step must not regress, but: {string.Join("; ", problems)}.";
                Assert.True(problems.Count == 0, regressed);
            }
        }
    }

    [Fact]
    public void ConformanceProblems_FlagsAPushGatedEvidenceUpload()
    {
        // Planted regression: an evidence upload gated to push, globbing the signed-binaries dir, missing always().
        string[] lines =
        {
            "      - name: Upload evidence",
            "        if: github.event_name == 'push'",
            "        uses: actions/upload-artifact@ea165f8d # v4.6.2",
            "        with:",
            "          name: evidence-macos",
            "          path: dist/",
        };
        var bad = new Step("macos", lines);

        List<string> problems = ConformanceProblems(bad);

        Assert.Contains(problems, p => p.Contains("push-gated", StringComparison.Ordinal));
        Assert.Contains(problems, p => p.Contains("always()", StringComparison.Ordinal));
        Assert.Contains(problems, p => p.Contains("dist/", StringComparison.Ordinal));
    }

    [Fact]
    public void ConformanceProblems_AcceptsAConformingEvidenceUpload()
    {
        string[] lines =
        {
            "      - name: Upload evidence",
            "        if: always()",
            "        uses: actions/upload-artifact@ea165f8d # v4.6.2",
            "        with:",
            "          name: evidence-macos",
            "          if-no-files-found: warn",
            "          path: |",
            "            evidence/*.binlog",
            "            **/TestResults/**/*.cobertura.xml",
            "            **/*.trx",
        };
        var good = new Step("macos", lines);

        Assert.Empty(ConformanceProblems(good));
    }

    private static List<string> ReadWorkflowLines()
    {
        string workflow = Path.Combine(RepositoryFiles.FindRepositoryRoot(), ".github", "workflows", "ci.yml");
        Assert.True(File.Exists(workflow), $"The CI workflow must exist at: {workflow}");
        return File.ReadAllText(workflow).Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n').ToList();
    }

    private static bool IsEvidenceUpload(Step step)
    {
        bool uploadsArtifact = step.Lines.Any(
            line => line.Trim().StartsWith("uses: actions/upload-artifact", StringComparison.Ordinal));
        bool namedEvidence = step.Lines.Any(line => IsArtifactNamedEvidence(line.Trim()));
        return uploadsArtifact && namedEvidence;
    }

    // True for a `name: evidence-…` artifact-name line (an optional surrounding quote allowed), which is how an
    // evidence upload names its artifact — distinct from the step's own `- name:` display line.
    private static bool IsArtifactNamedEvidence(string trimmedLine)
    {
        const string namePrefix = "name:";
        if (!trimmedLine.StartsWith(namePrefix, StringComparison.Ordinal))
        {
            return false;
        }

        string value = trimmedLine.Substring(namePrefix.Length).Trim().TrimStart('"', '\'');
        return value.StartsWith("evidence-", StringComparison.Ordinal);
    }

    // The ways an evidence-upload step regresses, as a list of problems (empty when it conforms).
    private static List<string> ConformanceProblems(Step step)
    {
        var problems = new List<string>();
        string text = string.Join("\n", step.Lines);

        bool always = step.Lines.Any(line =>
        {
            string trimmed = line.Trim();
            return trimmed.StartsWith("if:", StringComparison.Ordinal)
                && trimmed.Contains("always()", StringComparison.Ordinal);
        });
        if (!always)
        {
            problems.Add("missing `if: always()`");
        }

        if (text.Contains("github.event_name == 'push'", StringComparison.Ordinal))
        {
            problems.Add("push-gated (`github.event_name == 'push'`)");
        }

        if (!text.Contains(".binlog", StringComparison.Ordinal))
        {
            problems.Add("no binlog in the evidence set");
        }

        if (!text.Contains(".trx", StringComparison.Ordinal))
        {
            problems.Add("no .trx test results in the evidence set");
        }

        if (!text.Contains("cobertura", StringComparison.Ordinal) && !text.Contains("TestResults", StringComparison.Ordinal))
        {
            problems.Add("no coverage/TestResults report in the evidence set");
        }

        foreach (string forbidden in ForbiddenGlobs)
        {
            if (text.Contains(forbidden, StringComparison.Ordinal))
            {
                problems.Add($"globs forbidden token `{forbidden}` (release binary or signing material)");
            }
        }

        return problems;
    }

    // Splits the workflow into steps, each tagged with the job it belongs to. A job header is a line at exactly two
    // spaces of indent; a step begins at a six-space dash and runs until the next step or a shallower line.
    private static List<Step> ParseSteps(IReadOnlyList<string> lines)
    {
        var steps = new List<Step>();
        string currentJob = string.Empty;
        List<string>? current = null;

        foreach (string line in lines)
        {
            if (TryParseJobHeader(line, out string job))
            {
                Flush(steps, currentJob, ref current);
                currentJob = job;
                continue;
            }

            if (line.StartsWith("      - ", StringComparison.Ordinal))
            {
                Flush(steps, currentJob, ref current);
                current = new List<string> { line };
                continue;
            }

            if (current is null)
            {
                continue;
            }

            if (line.Trim().Length == 0 || (line.Length - line.TrimStart(' ').Length) >= 6)
            {
                current.Add(line);
            }
            else
            {
                Flush(steps, currentJob, ref current);
            }
        }

        Flush(steps, currentJob, ref current);
        return steps;
    }

    // A job header is exactly two spaces of indent, an identifier, then a colon and nothing else — e.g. `  macos:`.
    private static bool TryParseJobHeader(string line, out string job)
    {
        job = string.Empty;
        if (line.Length < 4 || line[0] != ' ' || line[1] != ' ' || line[2] == ' ')
        {
            return false;
        }

        string trimmedEnd = line.TrimEnd();
        if (!trimmedEnd.EndsWith(':'))
        {
            return false;
        }

        string candidate = trimmedEnd.Substring(2, trimmedEnd.Length - 3);
        if (candidate.Length == 0 || !candidate.All(ch => char.IsLetterOrDigit(ch) || ch == '-' || ch == '_'))
        {
            return false;
        }

        job = candidate;
        return true;
    }

    private static void Flush(List<Step> steps, string job, ref List<string>? current)
    {
        if (current is not null)
        {
            steps.Add(new Step(job, current));
            current = null;
        }
    }

    // One workflow step: the job it lives in and its raw lines.
    private sealed record Step(string Job, IReadOnlyList<string> Lines);
}
