﻿// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2023-2025 Pandora Behaviour Engine Contributors

using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Pandora.Models.Extensions;

public static class StreamExtensions
{
	public static bool TryReadLine(this StreamReader reader, [NotNullWhen(true)] out string? line)
	{
		line = reader.ReadLine();
		return line != null;
	}
	public static bool TryReadNotEmptyLine(this StreamReader reader, [NotNullWhen(true)] out string? line)
	{
		line = reader.ReadLine();
		return !string.IsNullOrEmpty(line);
	}
	public static string ReadLineOrEmpty(this StreamReader reader) => reader.ReadLine() ?? string.Empty;
}
