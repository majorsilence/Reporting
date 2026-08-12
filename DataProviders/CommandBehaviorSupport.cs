using System;
using System.Data;

namespace Majorsilence.Reporting.Data
{
	/// <summary>
	/// Shared <see cref="CommandBehavior"/> handling for the file and web based providers in
	/// this assembly.
	///
	/// These commands used to accept only the exact values SingleResult and SchemaOnly and
	/// throw on everything else, including CommandBehavior.Default. That made them break
	/// whenever a caller passed a behaviour they would in fact have been happy to serve - the
	/// engine's data pass is one such caller - and it also rejected perfectly ordinary
	/// combinations such as SingleResult | SchemaOnly, since the checks compared the whole
	/// value rather than testing flags.
	///
	/// CommandBehavior is a [Flags] enum, so it is treated as flags here: anything these
	/// providers can honour or safely ignore is accepted in any combination.
	/// </summary>
	internal static class CommandBehaviorSupport
	{
		/// <summary>
		/// Flags these providers can serve. SchemaOnly is honoured by the readers that
		/// implement it; SingleResult, SingleRow, SequentialAccess and KeyInfo are hints that
		/// are safe to ignore for a forward-only, single-result, read-only source.
		/// CommandBehavior.Default is zero, so it always passes.
		/// </summary>
		private const CommandBehavior Supported =
			CommandBehavior.SchemaOnly |
			CommandBehavior.SingleResult |
			CommandBehavior.SingleRow |
			CommandBehavior.SequentialAccess |
			CommandBehavior.KeyInfo;

		/// <summary>
		/// Throws if <paramref name="behavior"/> asks for something these providers cannot do.
		/// Only CloseConnection is rejected: these readers do not own the connection they were
		/// handed, so quietly ignoring it would leak it.
		/// </summary>
		internal static void Validate(CommandBehavior behavior)
		{
			CommandBehavior unsupported = behavior & ~Supported;
			if (unsupported != 0)
				throw new ArgumentException(
					"ExecuteReader does not support CommandBehavior." + unsupported + ".",
					nameof(behavior));
		}

		/// <summary>
		/// True when the caller asked for schema only, i.e. column metadata and no rows.
		/// Tests the flag rather than the whole value so it still holds when SchemaOnly is
		/// combined with other flags.
		/// </summary>
		internal static bool IsSchemaOnly(CommandBehavior behavior)
		{
			return (behavior & CommandBehavior.SchemaOnly) != 0;
		}
	}
}
