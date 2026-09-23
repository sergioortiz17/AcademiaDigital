namespace AcademiaDigital.API.Helpers;

public static class ArgentineHolidays
{
    public static List<(DateOnly Date, string Name)> GetForYear(int year)
    {
        var holidays = new List<(DateOnly, string)>
        {
            (new DateOnly(year, 1, 1), "Año Nuevo"),
            (CarnivalMonday(year), "Carnaval"),
            (CarnivalTuesday(year), "Carnaval"),
            (new DateOnly(year, 3, 24), "Día de la Memoria"),
            (new DateOnly(year, 4, 2), "Día del Veterano y de los Caídos del Atlántico Sur"),
            (GoodFriday(year), "Viernes Santo"),
            (new DateOnly(year, 5, 1), "Día del Trabajador"),
            (new DateOnly(year, 5, 25), "Día de la Revolución de Mayo"),
            (new DateOnly(year, 6, 17), "Paso a la Inmortalidad del Gral. Güemes"),
            (new DateOnly(year, 6, 20), "Día de la Bandera"),
            (new DateOnly(year, 7, 9), "Día de la Independencia"),
            (new DateOnly(year, 8, 17), "Paso a la Inmortalidad del Gral. San Martín"),
            (new DateOnly(year, 10, 12), "Día del Respeto a la Diversidad Cultural"),
            (new DateOnly(year, 11, 20), "Día de la Soberanía Nacional"),
            (new DateOnly(year, 12, 8), "Día de la Inmaculada Concepción"),
            (new DateOnly(year, 12, 25), "Navidad"),
        };

        return holidays;
    }

    public static List<(DateOnly Date, string Name)> GetForMonth(int year, int month)
    {
        return GetForYear(year).Where(h => h.Date.Month == month).ToList();
    }

    private static DateOnly EasterSunday(int year)
    {
        int a = year % 19;
        int b = year / 100;
        int c = year % 100;
        int d = b / 4;
        int e = b % 4;
        int f = (b + 8) / 25;
        int g = (b - f + 1) / 3;
        int h = (19 * a + b - d - g + 15) % 30;
        int i = c / 4;
        int k = c % 4;
        int l = (32 + 2 * e + 2 * i - h - k) % 7;
        int m = (a + 11 * h + 22 * l) / 451;
        int month = (h + l - 7 * m + 114) / 31;
        int day = (h + l - 7 * m + 114) % 31 + 1;
        return new DateOnly(year, month, day);
    }

    private static DateOnly GoodFriday(int year)
        => EasterSunday(year).AddDays(-2);

    private static DateOnly CarnivalMonday(int year)
        => EasterSunday(year).AddDays(-48);

    private static DateOnly CarnivalTuesday(int year)
        => EasterSunday(year).AddDays(-47);
}
