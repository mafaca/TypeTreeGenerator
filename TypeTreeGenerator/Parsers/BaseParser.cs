using System;
using System.IO;
using System.Text;

namespace TypeTreeGenerator
{
	public abstract class BaseParser : IDisposable
	{
		public BaseParser(TreeReader reader)
		{
			if(reader == null)
			{
				throw new ArgumentNullException(nameof(reader));
			}
			m_reader = reader;
		}

		public void Dispose()
		{
			m_reader.Dispose();
		}
		
		protected void FindValidateSymbol(char expectedChar)
		{
			bool isFound = FindWord();
			if (!isFound)
			{
				throw CreateException($"Can't find character for reading");
			}

			char c = m_reader.ReadChar();
			if (c != expectedChar)
			{
				throw CreateException($"Can't find expected character '{expectedChar}'");
			}
		}

		protected void FindValidateWord(string expectedWord)
		{
			bool isFound = FindWord();
			if (!isFound)
			{
				throw CreateException($"Can't find word for reading");
			}

			string word = ReadWord();
			if (word != expectedWord)
			{
				throw CreateException($"Can't find expected word '{expectedWord}'");
			}
		}

		protected string FindReadWord()
		{
			bool found = FindWord();
			if (found)
			{
				return ReadWord();
			}
			else
			{
				return string.Empty;
			}
		}

		protected bool FindWord()
		{
			while (true)
			{
				if (m_reader.EndOfStream())
				{
					return false;
				}

				long position = m_reader.BaseStream.Position;
				char c = m_reader.ReadChar();
				switch (c)
				{
					case SpaceCharacter:
					case TabCharacter:
						continue;

					case CRCharacter:
					case LFCharacter:
						m_reader.BaseStream.Position = position;
						return false;

					default:
						m_reader.BaseStream.Position = position;
						return true;
				}
			}
		}

		protected bool FindNextLine(bool isIncrease = true)
		{
			while (true)
			{
				if (m_reader.EndOfStream())
				{
					return false;
				}

				char c = m_reader.ReadChar();
				switch (c)
				{
					case SpaceCharacter:
					case TabCharacter:
						continue;

					case CRCharacter:
						if(m_reader.EndOfStream())
						{
							return false;
						}

						long position = m_reader.BaseStream.Position;
						c = m_reader.ReadChar();
						if(c != LFCharacter)
						{
							m_reader.BaseStream.Position = position;
						}
						if(isIncrease)
						{
							m_reader.Line++;
						}
						return true;

					case LFCharacter:
						if(isIncrease)
						{
							m_reader.Line++;
						}
						return true;

					default:
						throw CreateException("Readable symbol was found while searching for next line");
				}
			}
		}

		protected string ReadWord()
		{
			if (m_reader.EndOfStream())
			{
				throw CreateException("Can't read word. EOF was found");
			}

			ReadWordContent();

			string word = m_sb.ToString();
			m_sb.Length = 0;
			return word;
		}

		protected virtual void ReadWordContent()
		{
			while (true)
			{
				if (m_reader.EndOfStream())
				{
					return;
				}

				long position = m_reader.BaseStream.Position;
				char c = m_reader.ReadChar();
				if(IsBreakCharacter(c))
				{
					m_reader.BaseStream.Position = position;
					return;
				}

				m_sb.Append(c);
			}
		}

		protected virtual bool IsBreakCharacter(char c)
		{
			switch (c)
			{
				case SpaceCharacter:
				case TabCharacter:
				case CRCharacter:
				case LFCharacter:
					return true;
			}
			return false;
		}

		protected Exception CreateException(string message)
		{
			return new Exception($"'{message}' at line {m_reader.Line + 1}");
		}

		protected const char CRCharacter = '\r';
		protected const char LFCharacter = '\n';
		protected const char SpaceCharacter = ' ';
		protected const char TabCharacter = '\t';
		
		protected readonly StringBuilder m_sb = new StringBuilder();

		protected readonly TreeReader m_reader;
	}
}
