// 파일: samples/src/Chapter18/Program.cs
// 18장 — Backpressure & load isolation

using AsyncAwaitLab.Chapter18;
using AsyncAwaitLab.Common;

ConsoleHelpers.Banner("Chapter 18 — Backpressure & load isolation");

var mb = new PriorityMailbox<string>();

var consumer = Task.Run(async () =>
{
    int n = 0;
    await foreach (var m in mb.ConsumeAsync())
    {
        ConsoleHelpers.Log($"  consume #{n++}: {m}");
        if (n >= 8) break;
    }
});

// 사용자 메시지 5개
for (int i = 0; i < 5; i++) await mb.SendLowAsync($"user-{i}");
// 시스템 메시지 1개를 중간에 — 우선 처리
mb.TrySendHigh("SYSTEM-ANNOUNCE");
for (int i = 5; i < 8; i++) await mb.SendLowAsync($"user-{i}");

await consumer;
mb.Complete();
ConsoleHelpers.Log("Chapter 18 done.");
