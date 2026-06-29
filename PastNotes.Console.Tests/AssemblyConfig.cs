// Console.SetOut/SetError はグローバル状態のため、並列実行するとテスト間で干渉する。
// このアセンブリ内のテストは直列実行する。
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]
