// SPDX-License-Identifier: Apache-2.0

namespace Majorsilence.Reporting.RdlDoctor;

public enum FindingSeverity
{
    Info,
    Warning,
    Error,
}

/// <summary>A single compatibility issue found in an RDL/RDLC file.</summary>
public sealed record Finding(string Id, FindingSeverity Severity, string Message);
