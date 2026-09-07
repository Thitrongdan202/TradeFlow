namespace TradeFlow.Application.Common.Helpers;

public static class NumberToTextHelper
{
    private static readonly string[] ChuSo = new string[] { "không", "một", "hai", "ba", "bốn", "năm", "sáu", "bảy", "tám", "chín" };
    private static readonly string[] Tien = new string[] { "", "nghìn", "triệu", "tỷ", "nghìn tỷ", "triệu tỷ" };

    private static string DocSoCo3ChuSo(int so)
    {
        string ketQua = "";
        int tram = so / 100;
        int chuc = (so % 100) / 10;
        int donVi = so % 10;

        if (tram == 0 && chuc == 0 && donVi == 0) return "";
        if (tram != 0)
        {
            ketQua += ChuSo[tram] + " trăm ";
            if ((chuc == 0) && (donVi != 0)) ketQua += "lẻ ";
        }
        if ((chuc != 0) && (chuc != 1))
        {
            ketQua += ChuSo[chuc] + " mươi ";
            if ((chuc == 4) && (donVi != 0)) ketQua += "tư ";
        }
        if (chuc == 1) ketQua += "mười ";
        switch (donVi)
        {
            case 1:
                if ((chuc != 0) && (chuc != 1))
                    ketQua += "mốt ";
                else
                    ketQua += ChuSo[donVi] + " ";
                break;
            case 5:
                if (chuc == 0)
                    ketQua += ChuSo[donVi] + " ";
                else
                    ketQua += "lăm ";
                break;
            default:
                if (donVi != 0)
                    ketQua += ChuSo[donVi] + " ";
                break;
        }
        return ketQua;
    }

    public static string ConvertToWords(long number)
    {
        if (number == 0) return "Không";
        if (number < 0) return "Âm " + ConvertToWords(-number);

        string ketQua = "";
        int i = 0;
        long soHienTai = number;

        while (number > 0)
        {
            int soCo3ChuSo = (int)(number % 1000);
            if (soCo3ChuSo > 0)
            {
                string s = DocSoCo3ChuSo(soCo3ChuSo);
                ketQua = s + Tien[i] + " " + ketQua;
            }
            i++;
            number /= 1000;
        }

        ketQua = ketQua.Trim();
        if (ketQua.Length > 0)
        {
            ketQua = ketQua.Substring(0, 1).ToUpper() + ketQua.Substring(1);
        }

        // Fix missing zeros
        if (soHienTai >= 100 && ketQua.StartsWith("Lẻ ")) ketQua = ketQua.Substring(3);

        return ketQua;
    }
}
