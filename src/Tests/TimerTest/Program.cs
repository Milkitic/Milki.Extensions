using Milki.Extensions.MixPlayer;

var timerSource = new TimerSource()
{
    Rate = 5f
};
timerSource.Updated += TimerSource_Updated;
timerSource.Start();
await Task.Delay(1000);
timerSource.SkipTo(10000);
await Task.Delay(1000);
timerSource.Stop();
await Task.Delay(500);
timerSource.Start();
await Task.Delay(500);
timerSource.Start();
await Task.Delay(500);
timerSource.Restart();
void TimerSource_Updated(double obj)
{
    Console.WriteLine(obj);
}

Console.ReadKey(true);