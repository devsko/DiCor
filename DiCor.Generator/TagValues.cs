using System;

namespace DiCor.Generator
{
    internal struct TagValues
    {
        public string Group;
        public string Element;
        public string MessageField;
        public string Keyword;
        public string VM;
        public string VR;
        public bool IsRetired;

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
