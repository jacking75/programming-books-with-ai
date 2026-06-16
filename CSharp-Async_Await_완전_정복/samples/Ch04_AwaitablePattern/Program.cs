// 4장. Awaitable 패턴 직접 구현
//
// 사용법:
//   dotnet run --project Ch04_AwaitablePattern -- basic
//   dotnet run --project Ch04_AwaitablePattern -- typed
//   dotnet run --project Ch04_AwaitablePattern -- ext
//   dotnet run --project Ch04_AwaitablePattern -- inbox

using Ch04_AwaitablePattern;

var which = args.Length > 0 ? args[0] : "basic";

switch (which)
{
    case "basic":  await BasicAwaitable(); break;
    case "typed":  await TypedAwaiter(); break;
    case "ext":    await ExtensionAwaiter(); break;
    case "inbox":  await InboxDemo(); break;
    default:
        Console.WriteLine("usage: dotnet run -- [basic|typed|ext|inbox]");
        break;
}

static async Task BasicAwaitable()
{
    Console.WriteLine($"Before: {Environment.CurrentManagedThreadId}");
    await new MyAwaitable();
    Console.WriteLine($"After : {Environment.CurrentManagedThreadId}");
}

static async Task TypedAwaiter()
{
    int x = await new DelayedValue<int>(42, TimeSpan.FromMilliseconds(500));
    Console.WriteLine($"value = {x}");
}

static async Task ExtensionAwaiter()
{
    var sw = System.Diagnostics.Stopwatch.StartNew();
    await TimeSpan.FromSeconds(1);     // 확장 메서드로 TimeSpan을 await
    Console.WriteLine($"elapsed = {sw.ElapsedMilliseconds}ms");
}

static async Task InboxDemo()
{
    var inbox = new Inbox<string>();

    var consumer = Task.Run(async () =>
    {
        for (int i = 0; i < 3; i++)
        {
            var msg = await inbox.ReceiveAsync();
            Console.WriteLine($"received: {msg}");
        }
    });

    inbox.Send("hello");
    await Task.Delay(100);
    inbox.Send("world");
    await Task.Delay(100);
    inbox.Send("!");

    await consumer;
}
