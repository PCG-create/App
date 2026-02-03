namespace CoachPadWpf.Models;

public sealed class LiveMetrics
{
    public double talk_listen_ratio { get; set; }
    public double questions_per_minute { get; set; }
    public double sentiment { get; set; }
    public double engagement { get; set; }
    public string methodology_stage { get; set; } = "";
    public List<string> say_next { get; set; } = [];
    public long last_update_ms { get; set; }
}
