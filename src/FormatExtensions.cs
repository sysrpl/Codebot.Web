using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace Codebot.Web
{
	public static class FormatExtensions
	{
		private enum State
		{
			OutsideExpression,
			OnOpenBracket,
			InsideExpression,
			OnCloseBracket,
			End
		}

        private static string OutExpression(object source, string expression)
		{
			string format = "";
			bool colon = false;
			int colonIndex = expression.IndexOf(':');
			int commaIndex = expression.IndexOf(',');
			if (colonIndex > 0 || commaIndex > 0)
			{
				if (colonIndex > 0 && commaIndex > 0)
					colon = colonIndex < commaIndex;
				else
					colon = colonIndex > commaIndex;
				if (colon)
				{
					format = expression.Substring(colonIndex + 1);
					expression = expression.Substring(0, colonIndex);
				}
				else
				{
					format = expression.Substring(commaIndex + 1);
					expression = expression.Substring(0, commaIndex);
				}
			}
			if (String.IsNullOrEmpty(format))
				return (DataBinder.Eval(source, expression) ?? "").ToString();
			string front = colon ? "{0:" : "{0,";
			return DataBinder.Eval(source, expression, front + format + "}") ?? "";
		}

		private static void FormatObjectBuffer(this string format, object source, StringBuilder buffer)
		{
			if (source == null)
				return;
			using (var reader = new StringReader(format))
			{
				StringBuilder expression = new StringBuilder();
				int c = -1;
				State state = State.OutsideExpression;
				do
				{
					switch (state)
					{
						case State.OutsideExpression:
							c = reader.Read();
							switch (c)
							{
								case -1:
									state = State.End;
									break;
								case '{':
									state = State.OnOpenBracket;
									break;
								case '}':
									state = State.OnCloseBracket;
									break;
								default:
									buffer.Append((char)c);
									break;
							}
							break;
						case State.OnOpenBracket:
							c = reader.Read();
							if (c < 'A')
							{
								buffer.Append('{');
								state = State.OutsideExpression;
								break;
							}
							switch (c)
							{
								case -1:
									throw new FormatException();
								case '{':
									buffer.Append('{');
									state = State.OutsideExpression;
									break;
								default:
									expression.Append((char)c);
									state = State.InsideExpression;
									break;
							}
							break;
						case State.InsideExpression:
							c = reader.Read();
							switch (c)
							{
								case -1:
									throw new FormatException();
								case '}':
									buffer.Append(OutExpression(source, expression.ToString()));
									expression.Length = 0;
									state = State.OutsideExpression;
									break;
								default:
									expression.Append((char)c);
									break;
							}
							break;
						case State.OnCloseBracket:
							c = reader.Read();
							if (c < 'A')
							{
								buffer.Append('}');
								state = State.OutsideExpression;
								break;
							}
							switch (c)
							{
								case '}':
									buffer.Append('}');
									state = State.OutsideExpression;
									break;
								default:
									throw new FormatException();
							}
							break;
						default:
							throw new InvalidOperationException("Invalid state");
					}
				}
				while (state != State.End);
			}
		}

		public static StringBuilder FormatObject(this string format, object source, StringBuilder buffer = null)
		{
			if (format == null)
				throw new ArgumentNullException(nameof(format));
			if (buffer == null)
				buffer = new StringBuilder(format.Length * 2);
			if (source is IEnumerable<object>)
			{
				var list = source as IEnumerable<object>;
				foreach (var item in list)
					FormatObjectBuffer(format, item, buffer);
			}
			else
				FormatObjectBuffer(format, source, buffer);
			return buffer;
		}
	}
}

