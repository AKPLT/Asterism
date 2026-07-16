Console.WriteLine("DB Converter - 社内データベースのフォーマット変換ツール（デモ）");
Console.WriteLine();
Console.WriteLine($"起動フォルダ: {Environment.CurrentDirectory}");
Console.WriteLine($"実行ファイル: {Environment.ProcessPath}");
Console.WriteLine(args.Length > 0
    ? $"起動オプション: {string.Join(" ", args)}"
    : "起動オプション: なし");
Console.WriteLine();
Console.WriteLine("何かキーを押すと終了します...");
Console.ReadKey();
