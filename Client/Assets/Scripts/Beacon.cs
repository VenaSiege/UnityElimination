using Elimination.Command;

public class Beacon {
    public static readonly Beacon Instance = new();

    public delegate void BeaconCommandHandler(object sender, Cmd cmd);

    public event BeaconCommandHandler CommandPublisher;

    public delegate void BeaconMyScoreChangeHandler(object sender, int score);

    public event BeaconMyScoreChangeHandler MyScoreChangeEvent;

    private Beacon() { }

    public void PublishCommand(object sender, Cmd cmd) {
        CommandPublisher?.Invoke(sender, cmd);
    }

    public void PublishMyScoreChange(object sender, int value) {
        MyScoreChangeEvent?.Invoke(sender, value);
    }
}