namespace FitRos.API.Contracts.ClientProfiles;

public sealed class AddPhysicalMeasureRequest
{
    public decimal Weight { get; set; }
    public decimal BodyFatPercentage { get; set; }
    public decimal MuscleMass { get; set; }
    public decimal Waist { get; set; }
    public decimal Chest { get; set; }
    public decimal Arms { get; set; }
}
