# TelegramMessagesStatistics
C# / .NET Telegram chat statistics

Сбор статистики переписки в Telegram. Строит график зависимости количества сообщений от времени.
<br/>
Использование: TelegramMessagesStatistics.exe <path_to_result.json> <output.png>
<br/>
В "path_to_result.json" указать выходной файл из следующей последовательности действий:
<br/>
Переписка с человеком -> Три точки сверху -> "Экспорт истории чата" -> Максимальный размер файла поставить, убрать все галочки, формат JSON -> Экспортировать
