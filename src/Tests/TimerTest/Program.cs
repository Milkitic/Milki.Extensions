using Milki.Extensions.MixPlayer;

var timerSource = new TimerSource();
timerSource.Updated += TimerSource_Updated;
timerSource.Start();
await Task.Delay(2000);
timerSource.SkipTo(12345);
void TimerSource_Updated(double obj)
{
    Console.WriteLine(obj);
}

Console.ReadKey(true);