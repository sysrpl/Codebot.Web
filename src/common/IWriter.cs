namespace Codebot;

using System;

public interface IWriter
{
	void Write<T>(string name, T value);
	void WriteDate(string name, DateTime value);
	void WriteInt(string name, int value);
	void WriteLong(string name, long value);
	void WriteString(string name, string value);
	void WriteBool(string name, bool value);
	void WriteBool(string name, object value);
}
