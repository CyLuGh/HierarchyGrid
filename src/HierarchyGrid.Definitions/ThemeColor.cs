using System.Text;

namespace HierarchyGrid.Definitions;

public readonly struct ThemeColor
{
    public ThemeColor(byte a, byte r, byte g, byte b)
    {
        A = a;
        R = r;
        G = g;
        B = b;
    }

    public ThemeColor(string code)
    {
        var colorCode = code.Replace("#", "");

        switch (colorCode.Length)
        {
            case 6:
                A = 255;
                R = byte.Parse(colorCode[..2], System.Globalization.NumberStyles.HexNumber);
                G = byte.Parse(
                    colorCode.Substring(2, 2),
                    System.Globalization.NumberStyles.HexNumber
                );
                B = byte.Parse(
                    colorCode.Substring(4, 2),
                    System.Globalization.NumberStyles.HexNumber
                );
                break;
            case 8:
                A = byte.Parse(colorCode[..2], System.Globalization.NumberStyles.HexNumber);
                R = byte.Parse(
                    colorCode.Substring(2, 2),
                    System.Globalization.NumberStyles.HexNumber
                );
                G = byte.Parse(
                    colorCode.Substring(4, 2),
                    System.Globalization.NumberStyles.HexNumber
                );
                B = byte.Parse(
                    colorCode.Substring(6, 2),
                    System.Globalization.NumberStyles.HexNumber
                );
                break;
            default:
                throw new InvalidThemeColorCodeException($"{code} is an invalid color code");
        }
    }

    public readonly ThemeColor With(
        byte? a = null,
        byte? r = null,
        byte? g = null,
        byte? b = null
    ) => new(a ?? A, r ?? R, g ?? G, b ?? B);

    public byte A { get; }
    public byte R { get; }
    public byte G { get; }
    public byte B { get; }

    public (byte a, byte r, byte g, byte b) ToArgb() => (A, R, G, B);

    public string ToCode()
    {
        var sb = new StringBuilder("#");
        sb.Append(A.ToString("X"));
        sb.Append(R.ToString("X"));
        sb.Append(G.ToString("X"));
        sb.Append(B.ToString("X"));
        return sb.ToString();
    }

    public void Deconstruct(out byte a, out byte r, out byte g, out byte b)
    {
        a = A;
        r = R;
        g = G;
        b = B;
    }
}

public static class ThemeColors
{
    public static readonly ThemeColor Black = new("000000");
    public static readonly ThemeColor White = new("FFFFFF");

    public static readonly ThemeColor Red = new("FF0000");
    public static readonly ThemeColor Green = new("00FF00");
    public static readonly ThemeColor Blue = new("0000FF");

    public static readonly ThemeColor LightGray = new("D3D3D3");
    public static readonly ThemeColor DarkGray = new("808080");
    public static readonly ThemeColor DimGray = new("696969");
    public static readonly ThemeColor SlateGray = new("708090");
    public static readonly ThemeColor DarkSlateGray = new("2F4F4F");

    public static readonly ThemeColor IndianRed = new("CD5C5C");
    public static readonly ThemeColor YellowGreen = new("9ACD32");
    public static readonly ThemeColor GreenYellow = new("ADFF2F");

    public static readonly ThemeColor SeaGreen = new("2E8B57");
    public static readonly ThemeColor LightSeaGreen = new("20B2AA");

    public static readonly ThemeColor LightBlue = new("ADD8E6");

    public static readonly ThemeColor Gainsboro = new("#FFDCDCDC");
}
