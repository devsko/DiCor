using System;
using System.Xml.Linq;

namespace DiCor.Generator
{
    internal struct TagValues
    {
        public string Group;
        public string Element;
        public string MessageField;
        public string Keyword;
        public string VR;
        public string VM;
        public bool IsRetired;
        public readonly string Symbol;

        public TagValues(string group, string element, string messageField, string keyword, string vr, string vm, bool isRetired)
        {
            Group = group;
            Element = element;
            MessageField = messageField;
            Keyword = keyword;
            VR = vr;
            VM = vm;
            IsRetired = isRetired;
            Symbol = CreateSymbol();
        }

        private string CreateSymbol(bool useValue = false)
        {
            if (!string.IsNullOrEmpty(Keyword))
            {
                return IsRetired ? Keyword + "_RETIRED" : Keyword;
            }

            string text = useValue ? $"{Group}_{Element}" : MessageField;
            Span<char> buffer = stackalloc char[text.Length + 8 + 1]; // Additional 9 chars for appending "_RETIRED" and prepending "_" if needed
            Span<char> symbol = buffer.Slice(1);
            text.AsSpan().CopyTo(symbol);

            int pos = 0;
            bool toUpper = true;
            ReadOnlySpan<char> remainder = symbol;
            while (remainder.Length > 0)
            {
                char ch = remainder[0];
                if (ch == ':')
                {
                    break;
                }

                if (char.IsLetterOrDigit(ch) || ch == '_')
                {
                    symbol[pos++] = toUpper ? char.ToUpperInvariant(ch) : ch;
                    toUpper = false;
                }
                else if (ch is ' ' or '-')
                {
                    toUpper = true;
                }
                else if (ch is '&' or '.')
                {
                    symbol[pos++] = '_';
                    toUpper = true;
                }
                remainder = remainder.Slice(1);
            }
            if (pos == 0)
            {
                return CreateSymbol(useValue: true);
            }
            if (char.IsDigit(symbol[0]))
            {
                symbol = buffer;
                symbol[0] = '_';
                pos++;
            }
            if (IsRetired)
            {
                "_RETIRED".AsSpan().CopyTo(symbol.Slice(pos));
                pos += 8;
            }

            return symbol.Slice(0, pos).ToString();
        }

        public (byte, byte, byte) GetVM()
        {
            ReadOnlySpan<char> span = VM.AsSpan();
            if (span.Length == 0)
                return default;

            int pos = span.IndexOf(' ');
            if (pos >= 0)
                span = span.Slice(0, pos);

            byte min;
            pos = span.IndexOf('-');
            if (pos == -1)
            {
                min = byte.Parse(span.ToString());
                return (min, min, 0);
            }

            min = byte.Parse(span.Slice(0, pos).ToString());
            span = span.Slice(pos + 1);

            if (span[span.Length - 1] == 'n')
            {
                if (span.Length == 1)
                    return (min, 0, 1);
                else
                    return (min, 0, byte.Parse(span.Slice(0, span.Length - 1).ToString()));
            }

            return (min, byte.Parse(span.ToString()), 1);
        }

        public string[] GetVRs()
        {
            string[] vrs = VR.Split(new[] { " or " }, StringSplitOptions.RemoveEmptyEntries);
            if (vrs.Length == 0 || vrs[0].IndexOf(' ') >= 0)
                return new[] { "UN" };

            return vrs;
        }

        public (string, string?, string?) GetValues()
        {
            (string? templateGroupValue, string group) = TemplatePartValue(Group, true);
            (string? templateElementValue, string element) = TemplatePartValue(Element, false);

            return (group + element, templateGroupValue, templateElementValue);
        }

        private static (string?, string) TemplatePartValue(string part, bool isGroup)
        {
            // Group   12xx => (12FF_FF00, 1200)
            // Element 12xx => (1200_FF00, 1200)

            if (part.IndexOf('x') < 0)
                return (null, part);

            char[] value = new char[8];
            char[] partValue = new char[4];
            char v = isGroup ? 'F' : '0';
            for (int i = 0; i < 4; i++)
            {
                char c = part[i];
                if (c == 'x')
                {
                    value[i] = v;
                    value[i + 4] = '0';
                    partValue[i] = '0';
                }
                else
                {
                    value[i] = c;
                    value[i + 4] = 'F';
                    partValue[i] = c;
                }
            }
            return (new string(value), new string(partValue));
        }


    }
}
