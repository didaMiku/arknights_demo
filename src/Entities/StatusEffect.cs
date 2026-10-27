namespace ArknightsDemo;

/// <summary>A behavioural status (as opposed to a stat buff).</summary>
public class StatusEffect
{
    public StatusType Type;
    public float Remaining;
    public float Power;

    public bool Expired => Remaining <= 0f;
}
