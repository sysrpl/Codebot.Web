﻿namespace Codebot;

using System;

public interface IReader
{
	T Read<T>(string name, T value = default, bool stored = true);
	string[] ReadValues(params string[] args);
	DateTime ReadDate(string name, DateTime value = default, bool stored = true);
	int ReadInt(string name, int value = 0, bool stored = true);
	long ReadLong(string name, long value = 0, bool stored = true);
	string ReadString(string name, string value = "", bool stored = true);
	bool ReadBool(string name, bool value = false, bool stored = true);
}
