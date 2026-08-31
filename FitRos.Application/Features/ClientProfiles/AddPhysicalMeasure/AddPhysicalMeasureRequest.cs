namespace FitRos.API.Contracts.ClientProfiles;

public sealed class AddPhysicalMeasureRequest
{
    public decimal Weight { get; set; }
    public decimal BodyFatPercentage { get; set; }
    public decimal MuscleMass { get; set; }
    public decimal Waist { get; set; }
    public decimal Chest { get; set; }
    public decimal Arms { get; set; }

    /// <summary>Optional. Omit for "now". May be back-dated up to 60 days; never future.</summary>
    public DateTime? RecordedAt { get; set; }
}
