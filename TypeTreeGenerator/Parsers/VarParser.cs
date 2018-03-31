using System.Globalization;

namespace TypeTreeGenerator
{
	public class VarParser : BaseParser
	{
		public VarParser(TreeReader reader, bool isHex):
			base(reader)
		{
			m_isHex = isHex;
		}

		public void Read()
		{
			Parse(string.Empty);
		}

		public void Parse(string expectedName)
		{
			Name = FindReadWord();
			if (Name == string.Empty)
			{
				throw CreateException("Can't find variable's name");
			}
			if(expectedName != string.Empty)
			{
				if(Name != expectedName)
				{
					throw CreateException($"Read variable with name '{Name}' but expected '{expectedName}'");
				}
			}

			FindValidateSymbol(OpenBraceCharater);
			string value = FindReadWord();
			NumberStyles style = m_isHex ? NumberStyles.HexNumber : NumberStyles.None;
			if (!int.TryParse(value, style, CultureInfo.InvariantCulture, out int intValue))
			{
				throw CreateException($"Can't parse value '{value}'");
			}
			Value = intValue;
			FindValidateSymbol(CloseBraceCharater);
		}
		
		protected override bool IsBreakCharacter(char c)
		{
			if(base.IsBreakCharacter(c))
			{
				return true;
			}
			switch(c)
			{
				case OpenBraceCharater:
				case CloseBraceCharater:
					return true;
			}
			return false;
		}

		public string Name { get; private set; }
		public int Value { get; private set; }

		private const char OpenBraceCharater = '{';
		private const char CloseBraceCharater = '}';

		private readonly bool m_isHex;
	}
}
