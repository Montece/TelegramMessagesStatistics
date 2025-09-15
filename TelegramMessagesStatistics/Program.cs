using System.Text.Json;
using ScottPlot;

namespace TelegramMessagesStatistics;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Использование: TelegramMessagesStatistics.exe <path_to_result.json> <output.png>");
            return 1;
        }

        var jsonPath = args[0];
        var outPath = args.Length >= 2 ? args[1] : "chart.png";
        var title = "Сообщения по дням: Вы vs Собеседник";

        if (!File.Exists(jsonPath))
        {
            Console.WriteLine($"Файл не найден: {jsonPath}");
            return 1;
        }

        try
        {
            var json = File.ReadAllText(jsonPath);
            var jsonSerializerOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var export = JsonSerializer.Deserialize<TelegramExport>(json, jsonSerializerOptions) ?? new TelegramExport();

            var allMessages = export.Messages.Where(m => m.Type == "message").ToList();
            if (allMessages.Count == 0)
            {
                Console.WriteLine("В файле нет сообщений типа \"message\".");
                return 0;
            }

            var hasOutboundFlag = allMessages.Any(m => m.Outbound.HasValue);
            var partnerUserId = export.ChatType == "personal_chat" && export.ChatId is { } cid ? $"user{cid}" : null;

            var messagesMe = allMessages.Where(m =>
                (hasOutboundFlag && m.Outbound == true) ||
                (!hasOutboundFlag && export.ChatType == "personal_chat" && !string.Equals(m.FromId, partnerUserId, StringComparison.OrdinalIgnoreCase))
            ).ToList();

            var messagesThem = allMessages.Where(m =>
                (hasOutboundFlag && m.Outbound == false) ||
                (!hasOutboundFlag && export.ChatType == "personal_chat" && string.Equals(m.FromId, partnerUserId, StringComparison.OrdinalIgnoreCase))
            ).ToList();

            if (messagesMe.Count == 0 && messagesThem.Count == 0)
            {
                Console.WriteLine("Не удалось определить отправителей (нет Out и это не personal_chat).");
                return 0;
            }

            // Группировка по дням (локальное время)
            var byDayMe = GroupByLocalDate(messagesMe);
            var byDayThem = GroupByLocalDate(messagesThem);

            if (byDayMe.Count == 0 && byDayThem.Count == 0)
            {
                Console.WriteLine("Нет данных для построения графика.");
                return 0;
            }

            var haveDates = new List<DateTime>();
            haveDates.AddRange(byDayMe.Keys);
            haveDates.AddRange(byDayThem.Keys);

            var firstDay = haveDates.Min();
            var lastDay = haveDates.Max();
            var daysSpan = (lastDay - firstDay).Days + 1;

            var allDays = Enumerable.Range(0, daysSpan).Select(i => firstDay.AddDays(i)).ToArray();
            var xsBase = allDays.Select(d => d.ToOADate()).ToArray();

            var ysMe = allDays.Select(d => (double)(byDayMe.GetValueOrDefault(d, 0))).ToArray();
            var ysThem = allDays.Select(d => (double)(byDayThem.GetValueOrDefault(d, 0))).ToArray();

            var totalMe = ysMe.Sum(v => (int)v);
            var totalThem = ysThem.Sum(v => (int)v);
            var (maxDateMe, maxValMe) = FindPeak(byDayMe);
            var (maxDateThem, maxValThem) = FindPeak(byDayThem);

            Console.WriteLine("— Сводка —");
            Console.WriteLine($"Диапазон:       {firstDay:yyyy-MM-dd} .. {lastDay:yyyy-MM-dd} ({daysSpan} дней)");
            Console.WriteLine($"Вы:             всего {totalMe}, пик {maxValMe} в {maxDateMe:yyyy-MM-dd}, среднее {AverageSafe(totalMe, daysSpan):F2}/день");
            Console.WriteLine($"Собеседник:     всего {totalThem}, пик {maxValThem} в {maxDateThem:yyyy-MM-dd}, среднее {AverageSafe(totalThem, daysSpan):F2}/день");

            // Сглаживание (MA 3..14 по размеру диапазона)
            var window = Math.Clamp(daysSpan / 30, 3, 14);
            var maMe = MovingAverage(ysMe, window);
            var maThem = MovingAverage(ysThem, window);

            // Рисуем
            var plt = new Plot(1400, 700);
            plt.Title(title);
            plt.YLabel("Сообщений за день");
            plt.XLabel("Дата");
            plt.XAxis.DateTimeFormat(true);
            plt.Grid(enable: true, lineStyle: LineStyle.Solid);
            plt.Margins(x: 0.02, y: 0.08);

            // Столбцы бок-о-бок
            var offset = 0.18d; // Где-то пятая часть дня
            var barMe = plt.AddBar(ysMe, xsBase.Select(x => x - offset).ToArray());
            var barThem = plt.AddBar(ysThem, xsBase.Select(x => x + offset).ToArray());
            barMe.BarWidth = barThem.BarWidth = 0.36;
            barMe.BorderLineWidth = barThem.BorderLineWidth = 0;
            barMe.Label = "Вы";
            barThem.Label = export.ChatType == "personal_chat" ? (export.Name ?? "Собеседник") : "Собеседник";

            // Сглаживающие линии
            plt.AddScatter(xsBase, maMe, markerSize: 0, lineWidth: 2, label: "Вы (MA)");
            plt.AddScatter(xsBase, maThem, markerSize: 0, lineWidth: 2, label: "Собеседник (MA)");

            // Подсказки о пиках (если есть данные)
            if (maxValMe > 0)
            {
#pragma warning disable CS0618 // Type or member is obsolete
                plt.AddAnnotation($"Пик Вы: {maxValMe} • {maxDateMe:yyyy-MM-dd}", maxDateMe.ToOADate(), maxValMe);
#pragma warning restore CS0618 // Type or member is obsolete
            }

            if (maxValThem > 0)
            {
#pragma warning disable CS0618 // Type or member is obsolete
                plt.AddAnnotation($"Пик собес.: {maxValThem} • {maxDateThem:yyyy-MM-dd}", maxDateThem.ToOADate(), maxValThem);
#pragma warning restore CS0618 // Type or member is obsolete
            }

            plt.Legend(location: Alignment.UpperRight);
            plt.SaveFig(outPath);

            Console.WriteLine($"Готово! Сохранено: {outPath}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Ошибка: " + ex.Message);
            return 1;
        }
    }

    private static Dictionary<DateTime, int> GroupByLocalDate(IEnumerable<TelegramMessage> messages)
    {
        return messages
            .GroupBy(m => m.Date.LocalDateTime.Date)
            .OrderBy(g => g.Key)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    private static (DateTime date, int value) FindPeak(Dictionary<DateTime, int> byDay)
    {
        if (byDay.Count == 0)
        {
            return (default, 0);
        }

        var keyValuePair = byDay.Aggregate((a, b) => a.Value >= b.Value ? a : b);
        
        return (keyValuePair.Key, keyValuePair.Value);
    }

    private static double AverageSafe(int total, int days)
    {
        return days > 0 ? total / (double)days : 0d;
    }

    private static double[] MovingAverage(double[] data, int window)
    {
        if (window < 2)
        {
            return data.ToArray();
        }

        var n = data.Length;
        var result = new double[n];
        var half = window / 2;

        for (var i = 0; i < n; i++)
        {
            var a = Math.Max(0, i - half);
            var b = Math.Min(n - 1, i + half);

            var sum = 0d;
            var count = 0;

            for (var j = a; j <= b; j++)
            {
                sum += data[j];
                count++;
            }

            result[i] = count > 0 ? sum / count : data[i];
        }

        return result;
    }
}