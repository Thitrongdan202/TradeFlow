using System;
using System.Text;

namespace TradeFlow.Application.Common.Helpers;

public static class VietnameseNumberToWords
{
    private static readonly string[] Digits = { "không", "một", "hai", "ba", "bốn", "năm", "sáu", "bảy", "tám", "chín" };
    private static readonly string[] Tens = { "", "mười", "hai mươi", "ba mươi", "bốn mươi", "năm mươi", "sáu mươi", "bảy mươi", "tám mươi", "chín mươi" };

    public static string ConvertToWords(decimal amount, string currency = "đồng")
    {
        if (amount == 0)
            return $"Không {currency} chẵn.";

        long rounded = (long)Math.Round(amount, 0, MidpointRounding.AwayFromZero);
        if (rounded < 0)
            return "Âm " + ConvertPositiveLong(-rounded, currency);

        return ConvertPositiveLong(rounded, currency);
    }

    private static string ConvertPositiveLong(long number, string currency)
    {
        if (number == 0) return $"Không {currency} chẵn.";

        var sb = new StringBuilder();

        long billions = number / 1000000000;
        long remainder = number % 1000000000;

        long millions = remainder / 1000000;
        remainder %= 1000000;

        long thousands = remainder / 1000;
        long units = remainder % 1000;

        if (billions > 0)
        {
            sb.Append(ConvertThreeDigits((int)billions, false)).Append(" tỷ ");
        }

        if (millions > 0)
        {
            sb.Append(ConvertThreeDigits((int)millions, billions > 0)).Append(" triệu ");
        }
        else if (billions > 0 && remainder > 0)
        {
            // keep flow
        }

        if (thousands > 0)
        {
            sb.Append(ConvertThreeDigits((int)thousands, billions > 0 || millions > 0)).Append(" nghìn ");
        }

        if (units > 0)
        {
            sb.Append(ConvertThreeDigits((int)units, billions > 0 || millions > 0 || thousands > 0)).Append(" ");
        }

        string result = sb.ToString().Trim();
        if (result.Length > 0)
        {
            result = char.ToUpper(result[0]) + result[1..];
        }

        return $"{result} {currency} chẵn.";
    }

    private static string ConvertThreeDigits(int number, bool hasHigherUnits)
    {
        int hundreds = number / 100;
        int tens = (number % 100) / 10;
        int units = number % 10;

        var sb = new StringBuilder();

        if (hundreds > 0 || hasHigherUnits)
        {
            sb.Append(Digits[hundreds]).Append(" trăm ");
        }

        if (tens > 1)
        {
            sb.Append(Tens[tens]).Append(" ");
            if (units == 1) sb.Append("mốt ");
            else if (units == 4) sb.Append("tư ");
            else if (units == 5) sb.Append("lăm ");
            else if (units > 0) sb.Append(Digits[units]).Append(" ");
        }
        else if (tens == 1)
        {
            sb.Append("mười ");
            if (units == 5) sb.Append("lăm ");
            else if (units > 0) sb.Append(Digits[units]).Append(" ");
        }
        else // tens == 0
        {
            if (hundreds > 0 || hasHigherUnits)
            {
                if (units > 0) sb.Append("lẻ ").Append(Digits[units]).Append(" ");
            }
            else
            {
                if (units > 0) sb.Append(Digits[units]).Append(" ");
            }
        }

        return sb.ToString().Trim();
    }
}
