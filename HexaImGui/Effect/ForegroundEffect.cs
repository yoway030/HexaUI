namespace ELImGui.Effect;

using System;

public abstract class ForegroundEffect : IImRenderable, IImUpdatable
{
    protected long _durationTicks;

    public ForegroundEffect(DateTime startDateTime, DateTime endDateTime)
    {
        StartDateTime = startDateTime;
        EndDateTime = endDateTime;

        if (EndDateTime < StartDateTime)
        {
            throw new ArgumentException("EndDateTime must be greater than or equal to StartDateTime");
        }

        _durationTicks = (EndDateTime - StartDateTime).Ticks;
    }

    public DateTime StartDateTime { get; init; }
    public DateTime EndDateTime { get; init; }

    public bool IsStart { get; private set; } = false;
    public bool IsEnd { get; private set; } = false;

    public void RenderImObject(DateTime utcNow, double deltaSec)
    {
        OnRender(utcNow, deltaSec);
    }

    public void UpdateImObject(DateTime utcNow, double deltaSec)
    {
        if (IsStart == false && utcNow >= StartDateTime)
        {
            IsStart = true;
        }

        if (IsEnd == false && utcNow >= EndDateTime)
        {
            IsEnd = true;
        }

        OnUpdate(utcNow, deltaSec);
    }

    public abstract void OnRender(DateTime utcNow, double deltaSec);

    public abstract void OnUpdate(DateTime utcNow, double deltaSec);
}
