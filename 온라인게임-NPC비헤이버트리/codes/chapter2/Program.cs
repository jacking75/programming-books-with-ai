using chapter2;

Console.WriteLine("=== 슬라임 비헤이버트리 시뮬레이션 ===\n");

// 게임 세계 설정
var player = new Player("용사", new Vector3(0, 0, 0));
var slime = new Slime("파란 슬라임", new Vector3(10, 0, 0));

// AI 생성
var slimeAI = new SlimeAI(slime, () => player);

// 시뮬레이션 실행
for (int frame = 0; frame < 100; frame++)
{
    Console.WriteLine($"\n--- Frame {frame} ---");
    Console.WriteLine($"플레이어 위치: ({player.Position.X:F1}, {player.Position.Z:F1})");
    Console.WriteLine($"슬라임 위치: ({slime.Position.X:F1}, {slime.Position.Z:F1})");

    // AI 업데이트
    slimeAI.Update();

    // 플레이어를 슬라임 쪽으로 천천히 이동 (시뮬레이션)
    if (frame > 20 && frame < 40)
    {
        Vector3 direction = (slime.Position - player.Position).Normalized();
        player.Position += direction * 0.5f;
    }

    // 프레임 간 약간의 대기
    Thread.Sleep(100);
}

Console.WriteLine("\n=== 시뮬레이션 종료 ===");