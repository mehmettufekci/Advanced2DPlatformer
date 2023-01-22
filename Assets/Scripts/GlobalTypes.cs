namespace GlobalTypes
{
    public enum GroundType
    {
        None,
        LevelGeometry,
        OneWayPlatform,
        MovingPlatform,
        CollapsablePlatform,
        JumpPad
    }

    public enum WallType
    {
        None,
        Normal,
        Sticky,
        Runnable
    }

    public enum AirEffectorType
    {
        None,
        Ladder,
        Updraft,
        TractorBeam
    }
}
